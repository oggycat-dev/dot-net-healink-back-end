terraform {
  backend "s3" {
    bucket = "healink-tf-state-2025-oggycatdev" # Tên bucket S3 của bạn
    key    = "global/terraform.tfstate"
    region = "ap-southeast-2"
  }
}

provider "aws" {
  region = "ap-southeast-2"
}

variable "app_image_tag" {
  type        = string
  description = "The Docker image tag to deploy."
}

# --- VPC & SUBNETS (Điền thông tin của bạn ở đây) ---
# Bạn cần cung cấp ID của VPC và ít nhất 2 Public Subnet
# để Load Balancer có thể hoạt động ổn định.
variable "vpc_id" {
  description = "ID of your VPC"
  default     = "vpc-08fe88c24397c79a9" # !!! THAY BẰNG VPC ID CỦA BẠN
}

variable "public_subnets" {
  description = "A list of public subnet IDs"
  type        = list(string)
  default     = ["subnet-00d0aabb44d3b86f4", "subnet-0cf7a8a098483c77e"] # !!! THAY BẰNG 2 PUBLIC SUBNET ID CỦA BẠN
}

# --- SECURITY GROUPS ---

# Security Group cho Application Load Balancer
# Chỉ cho phép traffic từ internet vào port 80 (HTTP)
resource "aws_security_group" "alb_sg" {
  name        = "healink-alb-sg"
  description = "Allow HTTP inbound traffic for ALB"
  vpc_id      = var.vpc_id

  ingress {
    from_port   = 80
    to_port     = 80
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"]
  }

  # !!! THÊM MỚI RULE NÀY !!!
  # Cho phép traffic HTTPS bên trong security group
  # để task có thể nói chuyện với các VPC Endpoints (Secrets Manager, ECR)
  ingress {
    from_port = 443
    to_port   = 443
    protocol  = "tcp"
    self      = true # Chỉ cho phép traffic từ chính security group này
  }

  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }
}

# Security Group cho ECS Service và RDS Database
# Chỉ cho phép traffic từ ALB vào ứng dụng và từ ứng dụng vào database
resource "aws_security_group" "app_sg" {
  name        = "healink-app-sg"
  description = "Allow traffic from ALB to ECS and from ECS to RDS"
  vpc_id      = var.vpc_id

  # Cho phép traffic từ ALB vào ECS Service trên port 80
  ingress {
    from_port       = 80
    to_port         = 80
    protocol        = "tcp"
    security_groups = [aws_security_group.alb_sg.id]
  }

  # Cho phép traffic từ ECS Service vào RDS trên port 5432
  ingress {
    from_port = 5432
    to_port   = 5432
    protocol  = "tcp"
    self      = true # Chỉ cho phép các tài nguyên trong cùng SG này giao tiếp với nhau
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
    path                = "/health" # Giả định app của bạn có endpoint /health
    protocol            = "HTTP"
    matcher             = "200"
    interval            = 30
    timeout             = 5
    healthy_threshold   = 2
    unhealthy_threshold = 2
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

# --- HẠ TẦNG NỀN (ECR, ECS CLUSTER) ---

resource "aws_ecr_repository" "auth_service_repo" {
  name = "healink/auth-service"
}

resource "aws_ecs_cluster" "main" {
  name = "healink-cluster"
}

# --- HẠ TẦNG ỨNG DỤNG (IAM, ECS TASK DEF & SERVICE) ---

resource "aws_iam_role" "ecs_task_execution_role" {
  name               = "healink-ecs-task-execution-role"
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
      portMappings = [
        { containerPort = 80, hostPort = 80 }
      ]
      secrets = [
        { name = "DB_PASSWORD", valueFrom = aws_secretsmanager_secret.db_password.arn }
      ]
      environment = [
        { name = "DB_HOST", value = aws_db_instance.healink_db.address },
        { name = "DB_PORT", value = tostring(aws_db_instance.healink_db.port) },
        { name = "DB_NAME", value = aws_db_instance.healink_db.db_name },
        { name = "DB_USER", value = aws_db_instance.healink_db.username },
        { name = "ASPNETCORE_ENVIRONMENT", value = "Docker" }
      ]
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
    subnets         = var.public_subnets
    security_groups = [aws_security_group.app_sg.id]
    assign_public_ip = false # Không cần IP công cộng khi đã có ALB
  }

  load_balancer {
    target_group_arn = aws_lb_target_group.auth_service.arn
    container_name   = "auth-service-container"
    container_port   = 80
  }

  # Đảm bảo listener được tạo trước khi service cố gắng đăng ký với nó
  depends_on = [aws_lb_listener.http]
}

# --- SECRETS MANAGER: Lưu trữ mật khẩu an toàn ---

resource "random_password" "db_master_password" {
  length           = 16
  special          = true
  override_special = "!#$%&*()-_=+[]{}<>:?"
}

resource "aws_secretsmanager_secret" "db_password" {
  name = "healink/db_password"
}

resource "aws_secretsmanager_secret_version" "db_password_version" {
  secret_id     = aws_secretsmanager_secret.db_password.id
  secret_string = random_password.db_master_password.result
}

resource "aws_iam_role_policy" "ecs_secrets_policy" {
  name = "ecs-secrets-manager-policy"
  role = aws_iam_role.ecs_task_execution_role.id

  policy = jsonencode({
    Version = "2012-10-17",
    Statement = [
      {
        Effect   = "Allow",
        Action   = ["secretsmanager:GetSecretValue"],
        Resource = [aws_secretsmanager_secret.db_password.arn]
      }
    ]
  })
}

# --- AMAZON RDS: Database PostgreSQL ---

resource "aws_db_instance" "healink_db" {
  identifier           = "healink-db-instance"
  allocated_storage    = 20
  instance_class       = "db.t3.micro"
  engine               = "postgres"
  engine_version       = "15"
  db_name              = "AuthServiceDB"
  username             = "postgres_admin"
  password             = random_password.db_master_password.result
  vpc_security_group_ids = [aws_security_group.app_sg.id] # Dùng chung SG với app để dễ giao tiếp
  
  # Không cần public access, an toàn hơn
  publicly_accessible  = false 
  skip_final_snapshot  = true
}

# --- VPC ENDPOINT FOR SECRETS MANAGER ---
# Lấy thông tin region hiện tại để dùng trong service_name
data "aws_region" "current" {}

# Tạo một lối đi riêng cho phép ECS tasks trong VPC
# nói chuyện với Secrets Manager mà không cần ra internet.
resource "aws_vpc_endpoint" "secrets_manager" {
  vpc_id            = var.vpc_id
  service_name      = "com.amazonaws.${data.aws_region.current.name}.secretsmanager"
  vpc_endpoint_type = "Interface"

  # Gắn endpoint vào các subnet của ứng dụng
  subnet_ids = var.public_subnets

  # Gắn vào security group của ứng dụng để cho phép traffic
  security_group_ids = [aws_security_group.app_sg.id]

  private_dns_enabled = true
}