terraform {
  backend "s3" {
    bucket = "healink-tf-state-2025-oggycatdev"
    key    = "global/terraform.tfstate"
    region = "ap-southeast-2"
  }
}

provider "aws" {
  region = "ap-southeast-2"
}

# --- BIẾN ĐẦU VÀO ---
# Các biến này được định nghĩa trong file variables.tf
# và được truyền giá trị từ terraform.tfvars hoặc CI/CD

# --- VPC & SUBNETS (!!! THAY THẾ BẰNG GIÁ TRỊ CỦA BẠN !!!) ---
variable "vpc_id" {
  description = "ID of your VPC"
  default     = "vpc-08fe88c24397c79a9"
}

variable "public_subnets" {
  description = "A list of at least 2 public subnet IDs"
  type        = list(string)
  default     = ["subnet-00d0aabb44d3b86f4", "subnet-0cf7a8a098483c77e"]
}

# --- DATA SOURCES ---
data "aws_region" "current" {}

data "aws_route_table" "main" {
  vpc_id = var.vpc_id
  filter {
    name   = "association.main"
    values = ["true"]
  }
}

# --- LOGGING ---
resource "aws_cloudwatch_log_group" "auth_service" {
  name              = "/ecs/auth-service"
  retention_in_days = 7 # Tự động xóa log sau 7 ngày để tiết kiệm chi phí
}

# --- SECURITY GROUPS ---
resource "aws_security_group" "alb_sg" {
  name        = "healink-alb-sg"
  description = "Allow traffic from ALB to ECS and from ECS to RDS"
  vpc_id      = var.vpc_id
  ingress {
    from_port   = 80
    to_port     = 80
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"]
  }
  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }
}

resource "aws_security_group" "app_sg" {
  name        = "healink-app-sg"
  description = "Allow traffic from ALB to ECS and from ECS to RDS"
  vpc_id      = var.vpc_id
  ingress {
    from_port       = 80
    to_port         = 80
    protocol        = "tcp"
    security_groups = [aws_security_group.alb_sg.id]
  }
  ingress {
    from_port = 5432
    to_port   = 5432
    protocol  = "tcp"
    self      = true
  }
  ingress {
    from_port = 443
    to_port   = 443
    protocol  = "tcp"
    self      = true
  }
  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }
}

# --- APPLICATION LOAD BALANCER ---
resource "aws_lb" "main" {
  name               = "healink-alb"
  internal           = false
  load_balancer_type = "application"
  security_groups    = [aws_security_group.alb_sg.id]
  subnets            = var.public_subnets
}
resource "aws_lb_target_group" "auth_service" {
  name        = "tg-auth-service"
  port        = 80
  protocol    = "HTTP"
  vpc_id      = var.vpc_id
  target_type = "ip"
  health_check {
    path    = "/health"
    matcher = "200"
  }
}
resource "aws_lb_listener" "http" {
  load_balancer_arn = aws_lb.main.arn
  port              = "80"
  protocol          = "HTTP"
  default_action {
    type             = "forward"
    target_group_arn = aws_lb_target_group.auth_service.arn
  }
}

# --- ECR & ECS CLUSTER ---
resource "aws_ecr_repository" "auth_service_repo" {
  name = "healink/auth-service"
}
resource "aws_ecs_cluster" "main" {
  name = "healink-cluster"
}

# --- IAM & ECS TASK ---
resource "aws_iam_role" "ecs_task_execution_role" {
  name = "healink-ecs-task-execution-role"
  assume_role_policy = jsonencode({
    Version   = "2012-10-17"
    Statement = [{ Action = "sts:AssumeRole", Effect = "Allow", Principal = { Service = "ecs-tasks.amazonaws.com" } }]
  })
}
resource "aws_iam_role_policy_attachment" "ecs_task_execution_role_policy" {
  role       = aws_iam_role.ecs_task_execution_role.name
  policy_arn = "arn:aws:iam::aws:policy/service-role/AmazonECSTaskExecutionRolePolicy"
}

resource "aws_ecs_task_definition" "auth_service" {
  family                   = "auth-service-task"
  network_mode             = "awsvpc"
  requires_compatibilities = ["FARGATE"]
  cpu                      = "256"
  memory                   = "512"
  execution_role_arn       = aws_iam_role.ecs_task_execution_role.arn
  container_definitions    = jsonencode([
    {
      name      = "auth-service-container"
      image     = "${aws_ecr_repository.auth_service_repo.repository_url}:${var.app_image_tag}"
      essential = true
      portMappings = [ { containerPort = 80, hostPort = 80 } ]
      secrets = [
        { name = "DB_PASSWORD", valueFrom = aws_secretsmanager_secret.db_password.arn },
        { name = "JWT_SECRET_KEY", valueFrom = aws_secretsmanager_secret.jwt_secret_key.arn },
        { name = "REDIS_CONNECTION_STRING", valueFrom = aws_secretsmanager_secret.redis_connection_string.arn },
        { name = "ADMIN_PASSWORD", valueFrom = aws_secretsmanager_secret.admin_password.arn },
        { name = "RABBITMQ_CONNECTION_STRING", valueFrom = aws_secretsmanager_secret.rabbitmq_connection_string.arn }
      ]
      environment = [
        { name = "DB_HOST", value = aws_db_instance.healink_db.address },
        { name = "DB_PORT", value = tostring(aws_db_instance.healink_db.port) },
        { name = "DB_NAME", value = aws_db_instance.healink_db.db_name },
        { name = "DB_USER", value = aws_db_instance.healink_db.username },
        { name = "ASPNETCORE_ENVIRONMENT", value = "Development" },
        { name = "JWT_ISSUER", value = var.jwt_issuer },
        { name = "JWT_AUDIENCE", value = var.jwt_audience },
        { name = "JWT_EXPIRE_MINUTES", value = tostring(var.jwt_expire_minutes) },
        { name = "ADMIN_EMAIL", value = var.admin_email },
        { name = "ALLOWED_ORIGINS", value = var.allowed_origins },
        # Redis Configuration (use ElastiCache endpoint)
        { name = "Redis__ConnectionString", value = "${aws_elasticache_cluster.healink_redis.cache_nodes[0].address}:${aws_elasticache_cluster.healink_redis.cache_nodes[0].port}" },
        { name = "Redis__InstanceName", value = "ProductAuthCache" },
        { name = "Redis__Database", value = "0" },
        { name = "Redis__ConnectTimeout", value = "5000" },
        { name = "Redis__SyncTimeout", value = "5000" },
        { name = "Redis__AbortOnConnectFail", value = "false" },
        { name = "Redis__ConnectRetry", value = "3" },
        { name = "Redis__Enabled", value = "true" },
        # RabbitMQ Configuration (use Amazon MQ endpoint)
        { name = "RabbitMQ__HostName", value = replace(replace(aws_mq_broker.healink_rabbitmq.instances[0].endpoints[0], "amqps://", ""), ":5671", "") },
        { name = "RabbitMQ__Port", value = "5672" },
        { name = "RabbitMQ__UserName", value = "admin" },
        { name = "RabbitMQ__Password", value = "HealinkRabbitMQ2025!" },
        { name = "RabbitMQ__VirtualHost", value = "/" },
        { name = "RabbitMQ__QueueName", value = "healink_events" },
        { name = "RabbitMQ__ExchangeName", value = "healink_exchange" },
        { name = "RabbitMQ__RoutingKey", value = "healink_routing" },
        { name = "RabbitMQ__Enabled", value = "true" }
      ]
      logConfiguration = {
        logDriver = "awslogs"
        options = {
          awslogs-group         = "/ecs/auth-service"
          awslogs-region        = "ap-southeast-2"
          awslogs-stream-prefix = "ecs"
        }
      }
    }
  ])
}

resource "aws_ecs_service" "auth_service" {
  name            = "auth-service"
  cluster         = aws_ecs_cluster.main.id
  task_definition = aws_ecs_task_definition.auth_service.arn
  desired_count   = 1
  launch_type     = "FARGATE"
  network_configuration {
    subnets          = var.public_subnets
    security_groups  = [aws_security_group.app_sg.id]
    assign_public_ip = false # Sửa lại thành false cho đúng kiến trúc
  }
  load_balancer {
    target_group_arn = aws_lb_target_group.auth_service.arn
    container_name   = "auth-service-container"
    container_port   = 80
  }
  depends_on = [aws_lb_listener.http]
}

# --- SECRETS & DATABASE ---
resource "random_password" "db_master_password" {
  length           = 16
  special          = true
  override_special = "_%[]{}<>()-!#$&=?"
}
resource "aws_secretsmanager_secret" "db_password" {
  name = "healink/db_password"
}
resource "aws_secretsmanager_secret_version" "db_password_version" {
  secret_id     = aws_secretsmanager_secret.db_password.id
  secret_string = random_password.db_master_password.result
}

resource "aws_secretsmanager_secret" "jwt_secret_key" {
  name = "healink/jwt_secret_key"
}
resource "aws_secretsmanager_secret_version" "jwt_secret_key_version" {
  secret_id     = aws_secretsmanager_secret.jwt_secret_key.id
  secret_string = var.jwt_secret_key
}

resource "aws_secretsmanager_secret" "redis_connection_string" {
  name = "healink/redis_connection_string"
}
resource "aws_secretsmanager_secret_version" "redis_connection_string_version" {
  secret_id     = aws_secretsmanager_secret.redis_connection_string.id
  secret_string = "${aws_elasticache_cluster.healink_redis.cache_nodes[0].address}:${aws_elasticache_cluster.healink_redis.cache_nodes[0].port}"
}

resource "aws_secretsmanager_secret" "admin_password" {
  name = "healink/admin_password"
}
resource "aws_secretsmanager_secret_version" "admin_password_version" {
  secret_id     = aws_secretsmanager_secret.admin_password.id
  secret_string = var.admin_password
}

# RabbitMQ connection string
resource "aws_secretsmanager_secret" "rabbitmq_connection_string" {
  name = "healink/rabbitmq_connection_string"
}
resource "aws_secretsmanager_secret_version" "rabbitmq_connection_string_version" {
  secret_id     = aws_secretsmanager_secret.rabbitmq_connection_string.id
  secret_string = "amqp://admin:HealinkRabbitMQ2025!@${aws_mq_broker.healink_rabbitmq.instances[0].endpoints[0]}:5672/"
}

resource "aws_iam_role_policy" "ecs_secrets_policy" {
  name   = "ecs-secrets-manager-policy"
  role   = aws_iam_role.ecs_task_execution_role.id
  policy = jsonencode({
    Version   = "2012-10-17",
    Statement = [ {
      Effect   = "Allow",
      Action   = ["secretsmanager:GetSecretValue"],
      Resource = [
        aws_secretsmanager_secret.db_password.arn,
        aws_secretsmanager_secret.jwt_secret_key.arn,
        aws_secretsmanager_secret.redis_connection_string.arn,
        aws_secretsmanager_secret.admin_password.arn,
        aws_secretsmanager_secret.rabbitmq_connection_string.arn,
      ]
    } ]
  })
}

resource "aws_db_instance" "healink_db" {
  identifier           = "healink-db-instance"
  allocated_storage    = 20
  instance_class       = "db.t3.micro"
  engine               = "postgres"
  engine_version       = "15"
  db_name              = "AuthServiceDB"
  username             = "postgres_admin"
  password             = random_password.db_master_password.result
  vpc_security_group_ids = [aws_security_group.app_sg.id]
  publicly_accessible  = false
  skip_final_snapshot  = true
}

# --- VPC ENDPOINTS ---
resource "aws_vpc_endpoint" "secrets_manager" {
  vpc_id              = var.vpc_id
  service_name        = "com.amazonaws.${data.aws_region.current.id}.secretsmanager"
  vpc_endpoint_type   = "Interface"
  subnet_ids          = var.public_subnets
  security_group_ids  = [aws_security_group.app_sg.id]
  private_dns_enabled = true
}
resource "aws_vpc_endpoint" "ecr_api" {
  vpc_id              = var.vpc_id
  service_name        = "com.amazonaws.${data.aws_region.current.id}.ecr.api"
  vpc_endpoint_type   = "Interface"
  subnet_ids          = var.public_subnets
  security_group_ids  = [aws_security_group.app_sg.id]
  private_dns_enabled = true
}
resource "aws_vpc_endpoint" "ecr_dkr" {
  vpc_id              = var.vpc_id
  service_name        = "com.amazonaws.${data.aws_region.current.id}.ecr.dkr"
  vpc_endpoint_type   = "Interface"
  subnet_ids          = var.public_subnets
  security_group_ids  = [aws_security_group.app_sg.id]
  private_dns_enabled = true
}
resource "aws_vpc_endpoint" "s3_gateway" {
  vpc_id            = var.vpc_id
  service_name      = "com.amazonaws.${data.aws_region.current.id}.s3"
  vpc_endpoint_type = "Gateway"
  route_table_ids   = [data.aws_route_table.main.id]
}
resource "aws_vpc_endpoint" "cloudwatch_logs" {
  vpc_id              = var.vpc_id
  service_name        = "com.amazonaws.${data.aws_region.current.id}.logs"
  vpc_endpoint_type   = "Interface"
  subnet_ids          = var.public_subnets
  security_group_ids  = [aws_security_group.app_sg.id]
  private_dns_enabled = true
}

# --- RABBITMQ (Amazon MQ) ---
resource "aws_mq_broker" "healink_rabbitmq" {
  broker_name                = "healink-rabbitmq"
  engine_type               = "RabbitMQ"
  engine_version           = "3.13"
  auto_minor_version_upgrade = true
  host_instance_type       = "mq.t3.micro"  # Cheapest option for dev/test
  deployment_mode          = "SINGLE_INSTANCE"  # For development, use ACTIVE_STANDBY_MULTI_AZ for production
  
  user {
    username = "admin"
    password = "HealinkRabbitMQ2025!"  # In production, use AWS Secrets Manager
  }
  
  subnet_ids         = [var.public_subnets[0]]  # Single AZ for development
  security_groups    = [aws_security_group.rabbitmq_sg.id]
  publicly_accessible = false
  
  tags = {
    Name = "healink-rabbitmq"
    Environment = "development"
  }
}

resource "aws_security_group" "rabbitmq_sg" {
  name        = "healink-rabbitmq-sg"
  description = "Security group for RabbitMQ (Amazon MQ)"
  vpc_id      = var.vpc_id

  ingress {
    description = "RabbitMQ AMQP from ECS"
    from_port   = 5672
    to_port     = 5672
    protocol    = "tcp"
    security_groups = [aws_security_group.app_sg.id]
  }

  ingress {
    description = "RabbitMQ Management Console from ECS"
    from_port   = 15672
    to_port     = 15672
    protocol    = "tcp"
    security_groups = [aws_security_group.app_sg.id]
  }

  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }

  tags = {
    Name = "healink-rabbitmq-sg"
  }
}

# --- REDIS (ElastiCache) ---
resource "aws_elasticache_subnet_group" "healink_redis" {
  name       = "healink-redis-subnet-group"
  subnet_ids = var.public_subnets
}

resource "aws_elasticache_cluster" "healink_redis" {
  cluster_id           = "healink-redis"
  engine               = "redis"
  node_type            = "cache.t3.micro"  # Cheapest option for dev/test
  num_cache_nodes      = 1
  parameter_group_name = "default.redis7"
  port                 = 6379
  subnet_group_name    = aws_elasticache_subnet_group.healink_redis.name
  security_group_ids   = [aws_security_group.redis_sg.id]

  tags = {
    Name = "healink-redis"
    Environment = "development"
  }
}

resource "aws_security_group" "redis_sg" {
  name        = "healink-redis-sg"
  description = "Security group for Redis (ElastiCache)"
  vpc_id      = var.vpc_id

  ingress {
    description = "Redis from ECS"
    from_port   = 6379
    to_port     = 6379
    protocol    = "tcp"
    security_groups = [aws_security_group.app_sg.id]
  }

  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }

  tags = {
    Name = "healink-redis-sg"
  }
}

# --- OUTPUTS ---
output "db_endpoint" {
  description = "RDS instance endpoint"
  value       = aws_db_instance.healink_db.endpoint
}

output "db_name" {
  description = "Database name"
  value       = aws_db_instance.healink_db.db_name
}

output "alb_dns_name" {
  description = "ALB DNS name"
  value       = aws_lb.main.dns_name
}

output "rabbitmq_endpoint" {
  description = "RabbitMQ broker endpoint"
  value       = aws_mq_broker.healink_rabbitmq.instances[0].endpoints[0]
}

output "rabbitmq_console_url" {
  description = "RabbitMQ management console URL"
  value       = aws_mq_broker.healink_rabbitmq.instances[0].console_url
}

output "redis_endpoint" {
  description = "Redis cluster endpoint"
  value       = aws_elasticache_cluster.healink_redis.cache_nodes[0].address
}

output "redis_port" {
  description = "Redis port"
  value       = aws_elasticache_cluster.healink_redis.cache_nodes[0].port
}