# Internal Microservice Module (No ALB)
# Services communicate internally via service discovery or direct connection through Gateway

terraform {
  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.0"
    }
  }
}

# Security Group for internal microservice
resource "aws_security_group" "microservice_sg" {
  name_prefix = "${var.project_name}-${var.service_name}-${var.environment}"
  vpc_id      = var.vpc_id

  # Allow traffic from within VPC
  ingress {
    from_port   = var.container_port
    to_port     = var.container_port
    protocol    = "tcp"
    cidr_blocks = ["10.0.0.0/16"]
  }

  # Allow all outbound
  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }

  tags = {
    Name        = "${var.project_name}-${var.service_name}-${var.environment}-sg"
    Environment = var.environment
    Service     = var.service_name
  }
}

# CloudWatch Log Group
resource "aws_cloudwatch_log_group" "microservice_logs" {
  name              = "/ecs/${var.project_name}-${var.service_name}-${var.environment}"
  retention_in_days = 7

  tags = {
    Name        = "${var.project_name}-${var.service_name}-${var.environment}-logs"
    Environment = var.environment
    Service     = var.service_name
  }
}

# ECS Task Definition
resource "aws_ecs_task_definition" "microservice_task" {
  family                   = "${var.project_name}-${var.service_name}-${var.environment}"
  network_mode             = "awsvpc"
  requires_compatibilities = ["FARGATE"]
  cpu                      = var.task_cpu
  memory                   = var.task_memory
  execution_role_arn       = var.task_execution_role_arn
  task_role_arn            = var.task_execution_role_arn

  container_definitions = jsonencode([
    {
      name  = "${var.service_name}-container"
      image = var.docker_image
      
      portMappings = [
        {
          containerPort = var.container_port
          protocol      = "tcp"
        }
      ]

      environment = var.environment_variables

      logConfiguration = {
        logDriver = "awslogs"
        options = {
          "awslogs-group"         = aws_cloudwatch_log_group.microservice_logs.name
          "awslogs-region"        = data.aws_region.current.name
          "awslogs-stream-prefix" = "ecs"
        }
      }

      essential = true
    }
  ])

  tags = {
    Name        = "${var.project_name}-${var.service_name}-${var.environment}-task"
    Environment = var.environment
    Service     = var.service_name
  }
}

# ECS Service (No Load Balancer)
resource "aws_ecs_service" "microservice_service" {
  name            = "${var.project_name}-${var.service_name}-${var.environment}"
  cluster         = var.ecs_cluster_name
  task_definition = aws_ecs_task_definition.microservice_task.arn
  desired_count   = var.desired_count
  launch_type     = "FARGATE"

  network_configuration {
    subnets          = var.subnet_ids
    assign_public_ip = true
    security_groups  = [aws_security_group.microservice_sg.id]
  }

  # Enable service discovery
  enable_ecs_managed_tags = true
  propagate_tags          = "SERVICE"

  tags = {
    Name        = "${var.project_name}-${var.service_name}-${var.environment}-service"
    Environment = var.environment
    Service     = var.service_name
  }
}

# Data sources
data "aws_region" "current" {}

