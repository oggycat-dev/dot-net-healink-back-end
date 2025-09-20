# Microservice Module Main Configuration
# This module creates a complete microservice infrastructure

terraform {
  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.0"
    }
  }
}

# Security Group for the microservice
resource "aws_security_group" "microservice_sg" {
  name_prefix = "${var.project_name}-${var.service_name}-${var.environment}"
  vpc_id      = var.vpc_id

  ingress {
    from_port   = var.container_port
    to_port     = var.container_port
    protocol    = "tcp"
    cidr_blocks = ["10.0.0.0/16"]
  }

  ingress {
    from_port       = var.container_port
    to_port         = var.container_port
    protocol        = "tcp"
    security_groups = [aws_security_group.alb_sg.id]
  }

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

# Security Group for ALB
resource "aws_security_group" "alb_sg" {
  name_prefix = "${var.project_name}-${var.service_name}-alb-${var.environment}"
  vpc_id      = var.vpc_id

  ingress {
    from_port   = 80
    to_port     = 80
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"]
  }

  ingress {
    from_port   = 443
    to_port     = 443
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"]
  }

  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }

  tags = {
    Name        = "${var.project_name}-${var.service_name}-alb-${var.environment}-sg"
    Environment = var.environment
    Service     = var.service_name
  }
}

# Application Load Balancer
resource "aws_lb" "microservice_alb" {
  name               = "${var.project_name}-${var.service_name}-${substr(var.environment, 0, 3)}"
  internal           = false
  load_balancer_type = "application"
  security_groups    = [aws_security_group.alb_sg.id]
  subnets            = var.alb_subnet_ids

  enable_deletion_protection = false

  tags = {
    Name        = "${var.project_name}-${var.service_name}-${var.environment}-alb"
    Environment = var.environment
    Service     = var.service_name
  }
}

# Target Group
resource "aws_lb_target_group" "microservice_tg" {
  name        = "${var.project_name}-${var.service_name}-${substr(var.environment, 0, 3)}"
  port        = var.container_port
  protocol    = "HTTP"
  vpc_id      = var.vpc_id
  target_type = "ip"

  health_check {
    enabled             = true
    healthy_threshold   = 2
    interval            = 30
    matcher             = var.health_check_matcher
    path                = var.health_check_path
    port                = "traffic-port"
    protocol            = "HTTP"
    timeout             = 5
    unhealthy_threshold = 2
  }

  tags = {
    Name        = "${var.project_name}-${var.service_name}-${var.environment}-tg"
    Environment = var.environment
    Service     = var.service_name
  }
}

# ALB Listener
resource "aws_lb_listener" "microservice_listener" {
  load_balancer_arn = aws_lb.microservice_alb.arn
  port              = "80"
  protocol          = "HTTP"

  default_action {
    type             = "forward"
    target_group_arn = aws_lb_target_group.microservice_tg.arn
  }

  tags = {
    Name        = "${var.project_name}-${var.service_name}-${var.environment}-listener"
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

# ECS Service
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

  load_balancer {
    target_group_arn = aws_lb_target_group.microservice_tg.arn
    container_name   = "${var.service_name}-container"
    container_port   = var.container_port
  }

  depends_on = [aws_lb_listener.microservice_listener]

  tags = {
    Name        = "${var.project_name}-${var.service_name}-${var.environment}-service"
    Environment = var.environment
    Service     = var.service_name
  }
}

# Data sources
data "aws_region" "current" {}