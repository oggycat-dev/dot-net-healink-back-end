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

# --- HẠ TẦNG NỀN ---

resource "aws_ecr_repository" "auth_service_repo" {
  name = "healink/auth-service"
}

resource "aws_ecs_cluster" "main" {
  name = "healink-cluster"
}

# --- HẠ TẦNG ỨNG DỤNG ---

variable "app_image_tag" {
  type        = string
  description = "The Docker image tag to deploy."
}

resource "aws_iam_role" "ecs_task_execution_role" {
  name = "healink-ecs-task-execution-role"
  assume_role_policy = jsonencode({
    Version   = "2012-10-17"
    Statement = [ { Action = "sts:AssumeRole", Effect = "Allow", Principal = { Service = "ecs-tasks.amazonaws.com" } } ]
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
  container_definitions = jsonencode([
    {
      name      = "auth-service-container"
      image     = "${aws_ecr_repository.auth_service_repo.repository_url}:${var.app_image_tag}"
      essential = true
      portMappings = [ { containerPort = 80, hostPort = 80 } ]
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
    subnets         = ["subnet-00d0aabb44d3b86f4"] # ID Subnet của bạn
    security_groups = ["sg-0b078db7777c9f84a"]   # ID Security Group của bạn
    assign_public_ip = true
  }
}