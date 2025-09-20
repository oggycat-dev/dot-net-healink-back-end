# Healink Infrastructure with Terraform Modules
# This demonstrates the new modular approach for microservices

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

# --- NETWORK VARIABLES ---
variable "vpc_id" {
  description = "ID of your VPC"
  default     = "vpc-08fe88c24397c79a9"
}

variable "public_subnets" {
  description = "A list of at least 2 public subnet IDs"
  type        = list(string)
  default     = ["subnet-00d0aabb44d3b86f4", "subnet-0cf7a8a098483c77e"]
}

# --- PROJECT VARIABLES ---
variable "project_name" {
  description = "Project name prefix"
  type        = string
  default     = "healink"
}

# --- DATABASE VARIABLES ---
variable "db_instance_class" {
  description = "RDS instance class"
  type        = string
  default     = "db.t4g.micro"
}

variable "db_allocated_storage" {
  description = "RDS allocated storage in GB"
  type        = number
  default     = 20
}

variable "db_max_allocated_storage" {
  description = "RDS max allocated storage in GB"
  type        = number
  default     = 100
}

variable "db_name" {
  description = "Database name"
  type        = string
  default     = "healink_db"
}

variable "db_username" {
  description = "Database username"
  type        = string
  default     = "healink_user"
}

variable "db_password" {
  description = "Database password"
  type        = string
  sensitive   = true
  default     = "HealinkSecure2024!"
}

variable "db_backup_retention" {
  description = "RDS backup retention period in days"
  type        = number
  default     = 1
}

# --- REDIS VARIABLES ---
variable "redis_node_type" {
  description = "ElastiCache node type"
  type        = string
  default     = "cache.t4g.micro"
}

# --- RABBITMQ VARIABLES ---
variable "rabbitmq_instance_type" {
  description = "Amazon MQ instance type"
  type        = string
  default     = "mq.t3.micro"
}

variable "rabbitmq_username" {
  description = "RabbitMQ username"
  type        = string
  default     = "healink_mq"
}

variable "rabbitmq_password" {
  description = "RabbitMQ password"
  type        = string
  sensitive   = true
  default     = "HealinkMQ2024!"
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

# --- IAM ROLE FOR ECS TASKS ---
resource "aws_iam_role" "ecs_task_execution_role" {
  name = "${var.project_name}-ecs-task-execution-role-${terraform.workspace}"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Action = "sts:AssumeRole"
        Effect = "Allow"
        Principal = {
          Service = "ecs-tasks.amazonaws.com"
        }
      }
    ]
  })
}

resource "aws_iam_role_policy_attachment" "ecs_task_execution_role_policy" {
  role       = aws_iam_role.ecs_task_execution_role.name
  policy_arn = "arn:aws:iam::aws:policy/service-role/AmazonECSTaskExecutionRolePolicy"
}

resource "aws_iam_role_policy" "ecs_task_execution_role_cloudwatch" {
  name = "${var.project_name}-ecs-task-execution-cloudwatch-${terraform.workspace}"
  role = aws_iam_role.ecs_task_execution_role.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Action = [
          "logs:CreateLogGroup",
          "logs:CreateLogStream",
          "logs:PutLogEvents"
        ]
        Resource = "*"
      }
    ]
  })
}

# --- ECS CLUSTER ---
resource "aws_ecs_cluster" "healink_cluster" {
  name = "${var.project_name}-cluster-${terraform.workspace}"

  tags = {
    Name        = "${var.project_name}-cluster-${terraform.workspace}"
    Environment = terraform.workspace
  }
}

# --- DATABASE (RDS PostgreSQL) ---
resource "aws_db_subnet_group" "healink_db_subnet_group" {
  name       = "${var.project_name}-db-subnet-group-${terraform.workspace}"
  subnet_ids = var.public_subnets

  tags = {
    Name        = "${var.project_name}-db-subnet-group-${terraform.workspace}"
    Environment = terraform.workspace
  }
}

resource "aws_security_group" "rds_sg" {
  name_prefix = "${var.project_name}-rds-sg-${terraform.workspace}"
  vpc_id      = var.vpc_id

  ingress {
    from_port   = 5432
    to_port     = 5432
    protocol    = "tcp"
    cidr_blocks = ["10.0.0.0/16"]
  }

  tags = {
    Name        = "${var.project_name}-rds-sg-${terraform.workspace}"
    Environment = terraform.workspace
  }
}

resource "aws_db_instance" "healink_db" {
  identifier     = "${var.project_name}-db-${terraform.workspace}"
  engine         = "postgres"
  engine_version = "15.4"
  instance_class = var.db_instance_class

  allocated_storage     = var.db_allocated_storage
  max_allocated_storage = var.db_max_allocated_storage
  storage_encrypted     = true

  db_name  = var.db_name
  username = var.db_username
  password = var.db_password

  vpc_security_group_ids = [aws_security_group.rds_sg.id]
  db_subnet_group_name   = aws_db_subnet_group.healink_db_subnet_group.name

  backup_retention_period = var.db_backup_retention
  backup_window          = "03:00-04:00"
  maintenance_window     = "sun:04:00-sun:05:00"

  skip_final_snapshot = true
  deletion_protection = false

  tags = {
    Name        = "${var.project_name}-db-${terraform.workspace}"
    Environment = terraform.workspace
  }
}

# --- REDIS (ElastiCache) ---
resource "aws_elasticache_subnet_group" "healink_redis_subnet_group" {
  name       = "${var.project_name}-redis-subnet-group-${terraform.workspace}"
  subnet_ids = var.public_subnets

  tags = {
    Name        = "${var.project_name}-redis-subnet-group-${terraform.workspace}"
    Environment = terraform.workspace
  }
}

resource "aws_security_group" "redis_sg" {
  name_prefix = "${var.project_name}-redis-sg-${terraform.workspace}"
  vpc_id      = var.vpc_id

  ingress {
    from_port   = 6379
    to_port     = 6379
    protocol    = "tcp"
    cidr_blocks = ["10.0.0.0/16"]
  }

  tags = {
    Name        = "${var.project_name}-redis-sg-${terraform.workspace}"
    Environment = terraform.workspace
  }
}

resource "aws_elasticache_replication_group" "healink_redis" {
  replication_group_id       = "${var.project_name}-redis-${terraform.workspace}"
  description                = "Redis cluster for ${var.project_name}"
  
  node_type                  = var.redis_node_type
  port                      = 6379
  parameter_group_name      = "default.redis7"
  
  num_cache_clusters         = 1
  
  subnet_group_name         = aws_elasticache_subnet_group.healink_redis_subnet_group.name
  security_group_ids        = [aws_security_group.redis_sg.id]
  
  at_rest_encryption_enabled = false
  transit_encryption_enabled = false
  
  tags = {
    Name        = "${var.project_name}-redis-${terraform.workspace}"
    Environment = terraform.workspace
  }
}

# --- RABBITMQ (Amazon MQ) ---
resource "aws_security_group" "rabbitmq_sg" {
  name_prefix = "${var.project_name}-rabbitmq-sg-${terraform.workspace}"
  vpc_id      = var.vpc_id

  ingress {
    from_port   = 5671
    to_port     = 5671
    protocol    = "tcp"
    cidr_blocks = ["10.0.0.0/16"]
  }

  ingress {
    from_port   = 15671
    to_port     = 15671
    protocol    = "tcp"
    cidr_blocks = ["10.0.0.0/16"]
  }

  tags = {
    Name        = "${var.project_name}-rabbitmq-sg-${terraform.workspace}"
    Environment = terraform.workspace
  }
}

resource "aws_mq_broker" "healink_rabbitmq" {
  broker_name        = "${var.project_name}-rabbitmq-${terraform.workspace}"
  engine_type        = "RabbitMQ"
  engine_version     = "3.11.20"
  host_instance_type = var.rabbitmq_instance_type
  security_groups    = [aws_security_group.rabbitmq_sg.id]
  subnet_ids         = [var.public_subnets[0]]

  user {
    username = var.rabbitmq_username
    password = var.rabbitmq_password
  }

  logs {
    general = true
  }

  tags = {
    Name        = "${var.project_name}-rabbitmq-${terraform.workspace}"
    Environment = terraform.workspace
  }
}

# --- ECR REPOSITORIES ---
resource "aws_ecr_repository" "auth_service" {
  name                 = "${var.project_name}/auth-service"
  image_tag_mutability = "MUTABLE"
  force_delete         = true

  image_scanning_configuration {
    scan_on_push = false
  }

  tags = {
    Name        = "${var.project_name}-auth-service-ecr"
    Environment = terraform.workspace
  }
}

resource "aws_ecr_repository" "product_service" {
  name                 = "${var.project_name}/product-service"
  image_tag_mutability = "MUTABLE"
  force_delete         = true

  image_scanning_configuration {
    scan_on_push = false
  }

  tags = {
    Name        = "${var.project_name}-product-service-ecr"
    Environment = terraform.workspace
  }
}

# --- GATEWAY MODULE ---
module "gateway" {
  source = "./modules/microservice"

  service_name     = "gateway"
  environment      = terraform.workspace
  project_name     = var.project_name
  
  ecs_cluster_name = aws_ecs_cluster.healink_cluster.name
  task_cpu         = "256"
  task_memory      = "512"
  desired_count    = 1
  
  docker_image     = "${aws_ecr_repository.gateway.repository_url}:latest"
  container_port   = 80
  
  environment_variables = [
    {
      name  = "ASPNETCORE_ENVIRONMENT"
      value = terraform.workspace == "prod" ? "Production" : "Development"
    }
  ]
  
  vpc_id                    = var.vpc_id
  subnet_ids               = var.public_subnets
  alb_subnet_ids           = var.public_subnets
  task_execution_role_arn  = aws_iam_role.ecs_task_execution_role.arn
  
  health_check_path     = "/health"
  health_check_matcher  = "200"
}

# --- AUTH SERVICE MODULE ---
module "auth_service" {
  source = "./modules/microservice"

  service_name     = "auth-service"
  environment      = terraform.workspace
  project_name     = var.project_name
  
  ecs_cluster_name = aws_ecs_cluster.healink_cluster.name
  task_cpu         = "512"
  task_memory      = "1024"
  desired_count    = terraform.workspace == "prod" ? 2 : 1
  
  docker_image     = "${aws_ecr_repository.auth_service.repository_url}:latest"
  container_port   = 80
  
  environment_variables = [
    {
      name  = "ASPNETCORE_ENVIRONMENT"
      value = terraform.workspace == "prod" ? "Production" : "Development"
    },
    {
      name  = "ConnectionStrings__DefaultConnection"
      value = "Host=${aws_db_instance.healink_db.endpoint};Port=5432;Database=${var.db_name};Username=${var.db_username};Password=${var.db_password};"
    },
    {
      name  = "ConnectionStrings__Redis"
      value = "${aws_elasticache_replication_group.healink_redis.configuration_endpoint_address}:6379"
    },
    {
      name  = "RabbitMQ__Host"
      value = "${replace(aws_mq_broker.healink_rabbitmq.instances[0].endpoints[1], "amqps://", "")}"
    },
    {
      name  = "RabbitMQ__Port"
      value = "5671"
    },
    {
      name  = "RabbitMQ__Username"
      value = var.rabbitmq_username
    },
    {
      name  = "RabbitMQ__Password"
      value = var.rabbitmq_password
    },
    {
      name  = "RabbitMQ__UseSsl"
      value = "true"
    }
  ]
  
  vpc_id                    = var.vpc_id
  subnet_ids               = var.public_subnets
  alb_subnet_ids           = var.public_subnets
  task_execution_role_arn  = aws_iam_role.ecs_task_execution_role.arn
  
  health_check_path     = "/health"
  health_check_matcher  = "200,405"
}

# --- PRODUCT SERVICE MODULE ---
module "product_service" {
  source = "./modules/microservice"

  service_name     = "product-service"
  environment      = terraform.workspace
  project_name     = var.project_name
  
  ecs_cluster_name = aws_ecs_cluster.healink_cluster.name
  task_cpu         = "512"
  task_memory      = "1024"
  desired_count    = terraform.workspace == "prod" ? 2 : 1
  
  docker_image     = "${aws_ecr_repository.product_service.repository_url}:latest"
  container_port   = 80
  
  environment_variables = [
    {
      name  = "ASPNETCORE_ENVIRONMENT"
      value = terraform.workspace == "prod" ? "Production" : "Development"
    },
    {
      name  = "ConnectionStrings__DefaultConnection"
      value = "Host=${aws_db_instance.healink_db.endpoint};Port=5432;Database=${var.db_name};Username=${var.db_username};Password=${var.db_password};"
    },
    {
      name  = "ConnectionStrings__Redis"
      value = "${aws_elasticache_replication_group.healink_redis.configuration_endpoint_address}:6379"
    },
    {
      name  = "RabbitMQ__Host"
      value = "${replace(aws_mq_broker.healink_rabbitmq.instances[0].endpoints[1], "amqps://", "")}"
    },
    {
      name  = "RabbitMQ__Port"
      value = "5671"
    },
    {
      name  = "RabbitMQ__Username"
      value = var.rabbitmq_username
    },
    {
      name  = "RabbitMQ__Password"
      value = var.rabbitmq_password
    },
    {
      name  = "RabbitMQ__UseSsl"
      value = "true"
    }
  ]
  
  vpc_id                    = var.vpc_id
  subnet_ids               = var.public_subnets
  alb_subnet_ids           = var.public_subnets
  task_execution_role_arn  = aws_iam_role.ecs_task_execution_role.arn
  
  health_check_path     = "/health"
  health_check_matcher  = "200,405"
}

# --- ECR FOR GATEWAY ---
resource "aws_ecr_repository" "gateway" {
  name                 = "${var.project_name}/gateway"
  image_tag_mutability = "MUTABLE"
  force_delete         = true

  image_scanning_configuration {
    scan_on_push = false
  }

  tags = {
    Name        = "${var.project_name}-gateway-ecr"
    Environment = terraform.workspace
  }
}