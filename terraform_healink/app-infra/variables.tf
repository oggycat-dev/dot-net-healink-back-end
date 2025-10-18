# ==============================================
# APPLICATION INFRASTRUCTURE VARIABLES
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
# ECS CONFIGURATION
# ==============================================

variable "ecs_task_cpu" {
  description = "ECS task CPU units"
  type        = string
  default     = "256"
}

variable "ecs_task_memory" {
  description = "ECS task memory in MiB"
  type        = string
  default     = "512"
}

variable "ecs_desired_count" {
  description = "Desired number of ECS tasks"
  type        = number
  default     = 1
}

# ==============================================
# IMAGE CONFIGURATION
# ==============================================

variable "image_tag" {
  description = "Docker image tag to deploy"
  type        = string
  default     = "latest"
}

# ==============================================
# APPLICATION CONFIGURATION
# ==============================================

variable "aspnetcore_environment" {
  description = "ASP.NET Core environment"
  type        = string
  default     = "Development"
}

variable "allowed_origins" {
  description = "CORS allowed origins"
  type        = string
  default     = "http://localhost:3000,http://localhost:8080,http://localhost:5010"
}

# ==============================================
# SECRETS (should be overridden by GitHub Secrets)
# ==============================================

variable "db_password" {
  description = "Database password"
  type        = string
  sensitive   = true
  default     = "admin@123"
}

variable "jwt_secret" {
  description = "JWT secret key"
  type        = string
  sensitive   = true
  default     = "HealinkMicroserviceSecretKeyIsLongEnoughToBeUsedWithJWT"
}

variable "rabbitmq_password" {
  description = "RabbitMQ password"
  type        = string
  sensitive   = true
  default     = "admin@123"
}

variable "redis_password" {
  description = "Redis password"
  type        = string
  sensitive   = true
  default     = "admin@123"
}

variable "aws_s3_access_key" {
  description = "AWS S3 Access Key"
  type        = string
  sensitive   = true
  default     = ""
}

variable "aws_s3_secret_key" {
  description = "AWS S3 Secret Key"
  type        = string
  sensitive   = true
  default     = ""
}

variable "aws_s3_bucket_name" {
  description = "AWS S3 Bucket Name"
  type        = string
  default     = "healink-upload-file"
}

variable "smtp_username" {
  description = "SMTP username for email service"
  type        = string
  sensitive   = true
  default     = ""
}

variable "smtp_password" {
  description = "SMTP password for email service"
  type        = string
  sensitive   = true
  default     = ""
}

variable "momo_access_key" {
  description = "MoMo payment gateway access key"
  type        = string
  sensitive   = true
  default     = ""
}

variable "momo_secret_key" {
  description = "MoMo payment gateway secret key"
  type        = string
  sensitive   = true
  default     = ""
}

# ==============================================
# STATEFUL INFRASTRUCTURE (from remote state)
# ==============================================

variable "stateful_state_bucket" {
  description = "S3 bucket name for stateful infrastructure state"
  type        = string
  default     = "healink-tf-state-2025-oggycatdev"
}

variable "stateful_state_key" {
  description = "S3 key for stateful infrastructure state"
  type        = string
  default     = "stateful/terraform.tfstate"
}

variable "stateful_state_region" {
  description = "S3 region for stateful infrastructure state"
  type        = string
  default     = "ap-southeast-2"
}

variable "stateful_dynamodb_table" {
  description = "DynamoDB table for state locking"
  type        = string
  default     = "healink-tf-lock"
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

