# Healink Stateful Infrastructure
# Contains long-lived resources: RDS, ECR, Security Groups
# These resources persist and are NOT destroyed regularly

terraform {
  backend "s3" {
    bucket         = "healink-tf-state-2025-oggycatdev"
    key            = "stateful/terraform.tfstate"
    region         = "ap-southeast-2"
    workspace_key_prefix = "env:"
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

# --- SECURITY GROUPS ---
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

# --- DATABASE (RDS PostgreSQL) ---
resource "aws_db_subnet_group" "healink_db_subnet_group" {
  name       = "${var.project_name}-db-subnet-group-${terraform.workspace}"
  subnet_ids = var.public_subnets

  tags = {
    Name        = "${var.project_name}-db-subnet-group-${terraform.workspace}"
    Environment = terraform.workspace
  }
}

resource "aws_db_instance" "healink_db" {
  identifier     = "${var.project_name}-db-${terraform.workspace}"
  engine         = "postgres"
  engine_version = "16.3"  # Updated to latest stable version
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
resource "aws_mq_broker" "healink_rabbitmq" {
  broker_name               = "${var.project_name}-rabbitmq-${terraform.workspace}"
  engine_type               = "RabbitMQ"
  engine_version            = "3.13"  # Updated to valid version for RabbitMQ
  host_instance_type        = var.rabbitmq_instance_type
  security_groups           = [aws_security_group.rabbitmq_sg.id]
  subnet_ids                = [var.public_subnets[0]]
  auto_minor_version_upgrade = true  # Required for RabbitMQ 3.13

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
# All Healink microservices

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

resource "aws_ecr_repository" "user_service" {
  name                 = "${var.project_name}/user-service"
  image_tag_mutability = "MUTABLE"
  force_delete         = true

  image_scanning_configuration {
    scan_on_push = false
  }

  tags = {
    Name        = "${var.project_name}-user-service-ecr"
    Environment = terraform.workspace
  }
}

resource "aws_ecr_repository" "content_service" {
  name                 = "${var.project_name}/content-service"
  image_tag_mutability = "MUTABLE"
  force_delete         = true

  image_scanning_configuration {
    scan_on_push = false
  }

  tags = {
    Name        = "${var.project_name}-content-service-ecr"
    Environment = terraform.workspace
  }
}

resource "aws_ecr_repository" "notification_service" {
  name                 = "${var.project_name}/notification-service"
  image_tag_mutability = "MUTABLE"
  force_delete         = true

  image_scanning_configuration {
    scan_on_push = false
  }

  tags = {
    Name        = "${var.project_name}-notification-service-ecr"
    Environment = terraform.workspace
  }
}

resource "aws_ecr_repository" "subscription_service" {
  name                 = "${var.project_name}/subscription-service"
  image_tag_mutability = "MUTABLE"
  force_delete         = true

  image_scanning_configuration {
    scan_on_push = false
  }

  tags = {
    Name        = "${var.project_name}-subscription-service-ecr"
    Environment = terraform.workspace
  }
}

resource "aws_ecr_repository" "payment_service" {
  name                 = "${var.project_name}/payment-service"
  image_tag_mutability = "MUTABLE"
  force_delete         = true

  image_scanning_configuration {
    scan_on_push = false
  }

  tags = {
    Name        = "${var.project_name}-payment-service-ecr"
    Environment = terraform.workspace
  }
}

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

resource "aws_ecr_repository" "podcast_recommendation_service" {
  name                 = "${var.project_name}/podcast-recommendation-service"
  image_tag_mutability = "MUTABLE"
  force_delete         = true

  image_scanning_configuration {
    scan_on_push = false
  }

  tags = {
    Name        = "${var.project_name}-podcast-recommendation-service-ecr"
    Environment = terraform.workspace
  }
}