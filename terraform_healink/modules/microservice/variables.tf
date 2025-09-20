# Microservice Module Variables
# This module creates a complete microservice with ECS, ALB, CloudWatch

variable "service_name" {
  description = "Name of the microservice (e.g., auth-service, product-service)"
  type        = string
}

variable "environment" {
  description = "Environment (dev, prod)"
  type        = string
  default     = "dev"
}

variable "project_name" {
  description = "Project name prefix"
  type        = string
  default     = "healink"
}

# ECS Configuration
variable "ecs_cluster_name" {
  description = "ECS cluster name"
  type        = string
}

variable "task_cpu" {
  description = "ECS task CPU units"
  type        = string
  default     = "256"
}

variable "task_memory" {
  description = "ECS task memory in MB"
  type        = string
  default     = "512"
}

variable "desired_count" {
  description = "Desired number of ECS tasks"
  type        = number
  default     = 1
}

# Docker Configuration
variable "docker_image" {
  description = "Docker image URI"
  type        = string
}

variable "container_port" {
  description = "Port the container listens on"
  type        = number
  default     = 80
}

# Application Configuration
variable "environment_variables" {
  description = "Environment variables for the container"
  type = list(object({
    name  = string
    value = string
  }))
  default = []
}

# Networking
variable "vpc_id" {
  description = "VPC ID"
  type        = string
}

variable "subnet_ids" {
  description = "Subnet IDs for ECS tasks"
  type        = list(string)
}

variable "alb_subnet_ids" {
  description = "Subnet IDs for ALB"
  type        = list(string)
}

# IAM
variable "task_execution_role_arn" {
  description = "ECS task execution role ARN"
  type        = string
}

# Health Check
variable "health_check_path" {
  description = "Health check path"
  type        = string
  default     = "/"
}

variable "health_check_matcher" {
  description = "Health check success codes"
  type        = string
  default     = "200,405"
}