# ==============================================
# STATEFUL INFRASTRUCTURE VARIABLES
# ==============================================

variable "aws_region" {
  description = "AWS region to deploy resources"
  type        = string
  default     = "ap-southeast-2"
}

variable "project_name" {
  description = "Project name for resource naming"
  type        = string
  default     = "healink-free"
}

variable "environment" {
  description = "Environment name (dev, prod, free)"
  type        = string
  default     = "dev"
}

# ==============================================
# VPC CONFIGURATION
# ==============================================

variable "vpc_cidr" {
  description = "CIDR block for VPC"
  type        = string
  default     = "10.0.0.0/16"
}

variable "public_subnet_cidrs" {
  description = "CIDR blocks for public subnets"
  type        = list(string)
  default     = ["10.0.1.0/24", "10.0.2.0/24"]
}

variable "private_subnet_cidrs" {
  description = "CIDR blocks for private subnets"
  type        = list(string)
  default     = ["10.0.10.0/24", "10.0.11.0/24"]
}

variable "availability_zones" {
  description = "Availability zones"
  type        = list(string)
  default     = ["ap-southeast-2a", "ap-southeast-2b"]
}

# ==============================================
# RDS CONFIGURATION
# ==============================================

variable "db_instance_class" {
  description = "RDS instance class"
  type        = string
  default     = "db.t3.micro"
}

variable "db_allocated_storage" {
  description = "RDS allocated storage in GB"
  type        = number
  default     = 20
}

variable "db_name" {
  description = "Database name"
  type        = string
  default     = "healinkdb"
}

variable "db_username" {
  description = "Database master username"
  type        = string
  default     = "admin"
}

variable "db_password" {
  description = "Database master password"
  type        = string
  sensitive   = true
  default     = "admin@123" # Should be overridden by GitHub Secrets
}

variable "db_backup_retention_period" {
  description = "Database backup retention period in days"
  type        = number
  default     = 1
}

variable "db_max_allocated_storage" {
  description = "Database max allocated storage in GB"
  type        = number
  default     = 100
}

variable "db_skip_final_snapshot" {
  description = "Skip final snapshot when destroying DB"
  type        = bool
  default     = true
}

# ==============================================
# ELASTICACHE REDIS CONFIGURATION
# ==============================================

variable "redis_node_type" {
  description = "ElastiCache Redis node type"
  type        = string
  default     = "cache.t3.micro"
}

variable "redis_num_cache_nodes" {
  description = "Number of cache nodes"
  type        = number
  default     = 1
}

variable "redis_parameter_group_family" {
  description = "Redis parameter group family"
  type        = string
  default     = "redis7"
}

variable "redis_engine_version" {
  description = "Redis engine version"
  type        = string
  default     = "7.0"
}

variable "redis_port" {
  description = "Redis port"
  type        = number
  default     = 6379
}

variable "redis_password" {
  description = "Redis password"
  type        = string
  sensitive   = true
  default     = "admin@123" # Should be overridden by GitHub Secrets
}

# ==============================================
# RABBITMQ (Amazon MQ) CONFIGURATION
# ==============================================

variable "rabbitmq_instance_type" {
  description = "RabbitMQ broker instance type"
  type        = string
  default     = "mq.t3.micro"
}

variable "rabbitmq_engine_version" {
  description = "RabbitMQ engine version"
  type        = string
  default     = "3.13"
}

variable "rabbitmq_deployment_mode" {
  description = "RabbitMQ deployment mode"
  type        = string
  default     = "SINGLE_INSTANCE"
}

variable "rabbitmq_username" {
  description = "RabbitMQ admin username"
  type        = string
  default     = "admin"
}

variable "rabbitmq_password" {
  description = "RabbitMQ admin password"
  type        = string
  sensitive   = true
  default     = "admin@123" # Should be overridden by GitHub Secrets
}

variable "rabbitmq_publicly_accessible" {
  description = "Make RabbitMQ publicly accessible"
  type        = bool
  default     = true
}

# ==============================================
# TAGS
# ==============================================

variable "common_tags" {
  description = "Common tags for all resources"
  type        = map(string)
  default = {
    Project     = "Healink"
    ManagedBy   = "Terraform"
    Environment = "free"
  }
}

