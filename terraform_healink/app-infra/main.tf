# Healink Application Infrastructure
# Contains ephemeral resources: ECS, ALB, Auto Scaling
# These resources can be destroyed and recreated frequently

terraform {
  backend "s3" {
    bucket = "healink-tf-state-2025-oggycatdev"
    key    = "app-infra/terraform.tfstate"
    region = "ap-southeast-2"
  }
}

provider "aws" {
  region = "ap-southeast-2"
}

# --- VARIABLES ---
variable "project_name" {
  description = "Project name prefix"
  type        = string
  default     = "healink"
}

# --- DATA SOURCES ---
# Get stateful infrastructure outputs
data "terraform_remote_state" "stateful" {
  backend = "s3"
  config = {
    bucket = "healink-tf-state-2025-oggycatdev"
    key    = "stateful/terraform.tfstate"
    region = "ap-southeast-2"
  }
}

data "aws_region" "current" {}

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

# --- GATEWAY MODULE ---
module "gateway" {
  source = "../modules/microservice"

  service_name     = "gateway"
  environment      = terraform.workspace
  project_name     = var.project_name
  
  ecs_cluster_name = aws_ecs_cluster.healink_cluster.name
  task_cpu         = "256"
  task_memory      = "512"
  desired_count    = 1
  
  docker_image     = "${data.terraform_remote_state.stateful.outputs.gateway_ecr_url}:latest"
  container_port   = 80
  
  environment_variables = [
    {
      name  = "ASPNETCORE_ENVIRONMENT"
      value = terraform.workspace == "prod" ? "Production" : "Development"
    }
  ]
  
  vpc_id                    = data.terraform_remote_state.stateful.outputs.vpc_id
  subnet_ids               = data.terraform_remote_state.stateful.outputs.public_subnets
  alb_subnet_ids           = data.terraform_remote_state.stateful.outputs.public_subnets
  task_execution_role_arn  = aws_iam_role.ecs_task_execution_role.arn
  
  health_check_path     = "/health"
  health_check_matcher  = "200"
}

# --- AUTH SERVICE MODULE ---
module "auth_service" {
  source = "../modules/microservice"

  service_name     = "auth-service"
  environment      = terraform.workspace
  project_name     = var.project_name
  
  ecs_cluster_name = aws_ecs_cluster.healink_cluster.name
  task_cpu         = "512"
  task_memory      = "1024"
  desired_count    = terraform.workspace == "prod" ? 2 : 1
  
  docker_image     = "${data.terraform_remote_state.stateful.outputs.auth_service_ecr_url}:latest"
  container_port   = 80
  
  environment_variables = [
    {
      name  = "ASPNETCORE_ENVIRONMENT"
      value = terraform.workspace == "prod" ? "Production" : "Development"
    },
    {
      name  = "ConnectionStrings__DefaultConnection"
      value = "Host=${data.terraform_remote_state.stateful.outputs.database_endpoint};Port=${data.terraform_remote_state.stateful.outputs.database_port};Database=${data.terraform_remote_state.stateful.outputs.database_name};Username=${data.terraform_remote_state.stateful.outputs.database_username};Password=${data.terraform_remote_state.stateful.outputs.database_password};"
    },
    {
      name  = "ConnectionStrings__Redis"
      value = "${data.terraform_remote_state.stateful.outputs.redis_endpoint}:${data.terraform_remote_state.stateful.outputs.redis_port}"
    },
    {
      name  = "RabbitMQ__Host"
      value = "${replace(data.terraform_remote_state.stateful.outputs.rabbitmq_endpoint, "amqps://", "")}"
    },
    {
      name  = "RabbitMQ__Port"
      value = "5671"
    },
    {
      name  = "RabbitMQ__Username"
      value = data.terraform_remote_state.stateful.outputs.rabbitmq_username
    },
    {
      name  = "RabbitMQ__Password"
      value = data.terraform_remote_state.stateful.outputs.rabbitmq_password
    },
    {
      name  = "RabbitMQ__UseSsl"
      value = "true"
    }
  ]
  
  vpc_id                    = data.terraform_remote_state.stateful.outputs.vpc_id
  subnet_ids               = data.terraform_remote_state.stateful.outputs.public_subnets
  alb_subnet_ids           = data.terraform_remote_state.stateful.outputs.public_subnets
  task_execution_role_arn  = aws_iam_role.ecs_task_execution_role.arn
  
  health_check_path     = "/health"
  health_check_matcher  = "200,405"
}

# --- USER SERVICE MODULE ---
module "user_service" {
  source = "../modules/microservice"

  service_name     = "user-service"
  environment      = terraform.workspace
  project_name     = var.project_name
  
  ecs_cluster_name = aws_ecs_cluster.healink_cluster.name
  task_cpu         = "512"
  task_memory      = "1024"
  desired_count    = terraform.workspace == "prod" ? 2 : 1
  
  docker_image     = "${data.terraform_remote_state.stateful.outputs.user_service_ecr_url}:latest"
  container_port   = 80
  
  environment_variables = [
    {
      name  = "ASPNETCORE_ENVIRONMENT"
      value = terraform.workspace == "prod" ? "Production" : "Development"
    },
    {
      name  = "ConnectionStrings__DefaultConnection"
      value = "Host=${data.terraform_remote_state.stateful.outputs.database_endpoint};Port=${data.terraform_remote_state.stateful.outputs.database_port};Database=${data.terraform_remote_state.stateful.outputs.database_name};Username=${data.terraform_remote_state.stateful.outputs.database_username};Password=${data.terraform_remote_state.stateful.outputs.database_password};"
    },
    {
      name  = "ConnectionStrings__Redis"
      value = "${data.terraform_remote_state.stateful.outputs.redis_endpoint}:${data.terraform_remote_state.stateful.outputs.redis_port}"
    },
    {
      name  = "RabbitMQ__Host"
      value = "${replace(data.terraform_remote_state.stateful.outputs.rabbitmq_endpoint, "amqps://", "")}"
    },
    {
      name  = "RabbitMQ__Port"
      value = "5671"
    },
    {
      name  = "RabbitMQ__Username"
      value = data.terraform_remote_state.stateful.outputs.rabbitmq_username
    },
    {
      name  = "RabbitMQ__Password"
      value = data.terraform_remote_state.stateful.outputs.rabbitmq_password
    },
    {
      name  = "RabbitMQ__UseSsl"
      value = "true"
    }
  ]
  
  vpc_id                    = data.terraform_remote_state.stateful.outputs.vpc_id
  subnet_ids               = data.terraform_remote_state.stateful.outputs.public_subnets
  alb_subnet_ids           = data.terraform_remote_state.stateful.outputs.public_subnets
  task_execution_role_arn  = aws_iam_role.ecs_task_execution_role.arn
  
  health_check_path     = "/health"
  health_check_matcher  = "200,405"
}

# --- CONTENT SERVICE MODULE ---
module "content_service" {
  source = "../modules/microservice"

  service_name     = "content-service"
  environment      = terraform.workspace
  project_name     = var.project_name
  
  ecs_cluster_name = aws_ecs_cluster.healink_cluster.name
  task_cpu         = "512"
  task_memory      = "1024"
  desired_count    = terraform.workspace == "prod" ? 2 : 1
  
  docker_image     = "${data.terraform_remote_state.stateful.outputs.content_service_ecr_url}:latest"
  container_port   = 80
  
  environment_variables = [
    {
      name  = "ASPNETCORE_ENVIRONMENT"
      value = terraform.workspace == "prod" ? "Production" : "Development"
    },
    {
      name  = "ConnectionStrings__DefaultConnection"
      value = "Host=${data.terraform_remote_state.stateful.outputs.database_endpoint};Port=${data.terraform_remote_state.stateful.outputs.database_port};Database=${data.terraform_remote_state.stateful.outputs.database_name};Username=${data.terraform_remote_state.stateful.outputs.database_username};Password=${data.terraform_remote_state.stateful.outputs.database_password};"
    },
    {
      name  = "ConnectionStrings__Redis"
      value = "${data.terraform_remote_state.stateful.outputs.redis_endpoint}:${data.terraform_remote_state.stateful.outputs.redis_port}"
    },
    {
      name  = "RabbitMQ__Host"
      value = "${replace(data.terraform_remote_state.stateful.outputs.rabbitmq_endpoint, "amqps://", "")}"
    },
    {
      name  = "RabbitMQ__Port"
      value = "5671"
    },
    {
      name  = "RabbitMQ__Username"
      value = data.terraform_remote_state.stateful.outputs.rabbitmq_username
    },
    {
      name  = "RabbitMQ__Password"
      value = data.terraform_remote_state.stateful.outputs.rabbitmq_password
    },
    {
      name  = "RabbitMQ__UseSsl"
      value = "true"
    }
  ]
  
  vpc_id                    = data.terraform_remote_state.stateful.outputs.vpc_id
  subnet_ids               = data.terraform_remote_state.stateful.outputs.public_subnets
  alb_subnet_ids           = data.terraform_remote_state.stateful.outputs.public_subnets
  task_execution_role_arn  = aws_iam_role.ecs_task_execution_role.arn
  
  health_check_path     = "/health"
  health_check_matcher  = "200,405"
}

# --- NOTIFICATION SERVICE MODULE ---
module "notification_service" {
  source = "../modules/microservice"

  service_name     = "notification-service"
  environment      = terraform.workspace
  project_name     = var.project_name
  
  ecs_cluster_name = aws_ecs_cluster.healink_cluster.name
  task_cpu         = "256"
  task_memory      = "512"
  desired_count    = 1
  
  docker_image     = "${data.terraform_remote_state.stateful.outputs.notification_service_ecr_url}:latest"
  container_port   = 80
  
  environment_variables = [
    {
      name  = "ASPNETCORE_ENVIRONMENT"
      value = terraform.workspace == "prod" ? "Production" : "Development"
    },
    {
      name  = "ConnectionStrings__DefaultConnection"
      value = "Host=${data.terraform_remote_state.stateful.outputs.database_endpoint};Port=${data.terraform_remote_state.stateful.outputs.database_port};Database=${data.terraform_remote_state.stateful.outputs.database_name};Username=${data.terraform_remote_state.stateful.outputs.database_username};Password=${data.terraform_remote_state.stateful.outputs.database_password};"
    },
    {
      name  = "ConnectionStrings__Redis"
      value = "${data.terraform_remote_state.stateful.outputs.redis_endpoint}:${data.terraform_remote_state.stateful.outputs.redis_port}"
    },
    {
      name  = "RabbitMQ__Host"
      value = "${replace(data.terraform_remote_state.stateful.outputs.rabbitmq_endpoint, "amqps://", "")}"
    },
    {
      name  = "RabbitMQ__Port"
      value = "5671"
    },
    {
      name  = "RabbitMQ__Username"
      value = data.terraform_remote_state.stateful.outputs.rabbitmq_username
    },
    {
      name  = "RabbitMQ__Password"
      value = data.terraform_remote_state.stateful.outputs.rabbitmq_password
    },
    {
      name  = "RabbitMQ__UseSsl"
      value = "true"
    }
  ]
  
  vpc_id                    = data.terraform_remote_state.stateful.outputs.vpc_id
  subnet_ids               = data.terraform_remote_state.stateful.outputs.public_subnets
  alb_subnet_ids           = data.terraform_remote_state.stateful.outputs.public_subnets
  task_execution_role_arn  = aws_iam_role.ecs_task_execution_role.arn
  
  health_check_path     = "/health"
  health_check_matcher  = "200,405"
}

# --- SUBSCRIPTION SERVICE MODULE ---
module "subscription_service" {
  source = "../modules/microservice"

  service_name     = "subscription-service"
  environment      = terraform.workspace
  project_name     = var.project_name
  
  ecs_cluster_name = aws_ecs_cluster.healink_cluster.name
  task_cpu         = "512"
  task_memory      = "1024"
  desired_count    = terraform.workspace == "prod" ? 2 : 1
  
  docker_image     = "${data.terraform_remote_state.stateful.outputs.subscription_service_ecr_url}:latest"
  container_port   = 80
  
  environment_variables = [
    {
      name  = "ASPNETCORE_ENVIRONMENT"
      value = terraform.workspace == "prod" ? "Production" : "Development"
    },
    {
      name  = "ConnectionStrings__DefaultConnection"
      value = "Host=${data.terraform_remote_state.stateful.outputs.database_endpoint};Port=${data.terraform_remote_state.stateful.outputs.database_port};Database=${data.terraform_remote_state.stateful.outputs.database_name};Username=${data.terraform_remote_state.stateful.outputs.database_username};Password=${data.terraform_remote_state.stateful.outputs.database_password};"
    },
    {
      name  = "ConnectionStrings__Redis"
      value = "${data.terraform_remote_state.stateful.outputs.redis_endpoint}:${data.terraform_remote_state.stateful.outputs.redis_port}"
    },
    {
      name  = "RabbitMQ__Host"
      value = "${replace(data.terraform_remote_state.stateful.outputs.rabbitmq_endpoint, "amqps://", "")}"
    },
    {
      name  = "RabbitMQ__Port"
      value = "5671"
    },
    {
      name  = "RabbitMQ__Username"
      value = data.terraform_remote_state.stateful.outputs.rabbitmq_username
    },
    {
      name  = "RabbitMQ__Password"
      value = data.terraform_remote_state.stateful.outputs.rabbitmq_password
    },
    {
      name  = "RabbitMQ__UseSsl"
      value = "true"
    }
  ]
  
  vpc_id                    = data.terraform_remote_state.stateful.outputs.vpc_id
  subnet_ids               = data.terraform_remote_state.stateful.outputs.public_subnets
  alb_subnet_ids           = data.terraform_remote_state.stateful.outputs.public_subnets
  task_execution_role_arn  = aws_iam_role.ecs_task_execution_role.arn
  
  health_check_path     = "/health"
  health_check_matcher  = "200,405"
}

# --- PAYMENT SERVICE MODULE ---
module "payment_service" {
  source = "../modules/microservice"

  service_name     = "payment-service"
  environment      = terraform.workspace
  project_name     = var.project_name
  
  ecs_cluster_name = aws_ecs_cluster.healink_cluster.name
  task_cpu         = "512"
  task_memory      = "1024"
  desired_count    = terraform.workspace == "prod" ? 2 : 1
  
  docker_image     = "${data.terraform_remote_state.stateful.outputs.payment_service_ecr_url}:latest"
  container_port   = 80
  
  environment_variables = [
    {
      name  = "ASPNETCORE_ENVIRONMENT"
      value = terraform.workspace == "prod" ? "Production" : "Development"
    },
    {
      name  = "ConnectionStrings__DefaultConnection"
      value = "Host=${data.terraform_remote_state.stateful.outputs.database_endpoint};Port=${data.terraform_remote_state.stateful.outputs.database_port};Database=${data.terraform_remote_state.stateful.outputs.database_name};Username=${data.terraform_remote_state.stateful.outputs.database_username};Password=${data.terraform_remote_state.stateful.outputs.database_password};"
    },
    {
      name  = "ConnectionStrings__Redis"
      value = "${data.terraform_remote_state.stateful.outputs.redis_endpoint}:${data.terraform_remote_state.stateful.outputs.redis_port}"
    },
    {
      name  = "RabbitMQ__Host"
      value = "${replace(data.terraform_remote_state.stateful.outputs.rabbitmq_endpoint, "amqps://", "")}"
    },
    {
      name  = "RabbitMQ__Port"
      value = "5671"
    },
    {
      name  = "RabbitMQ__Username"
      value = data.terraform_remote_state.stateful.outputs.rabbitmq_username
    },
    {
      name  = "RabbitMQ__Password"
      value = data.terraform_remote_state.stateful.outputs.rabbitmq_password
    },
    {
      name  = "RabbitMQ__UseSsl"
      value = "true"
    }
  ]
  
  vpc_id                    = data.terraform_remote_state.stateful.outputs.vpc_id
  subnet_ids               = data.terraform_remote_state.stateful.outputs.public_subnets
  alb_subnet_ids           = data.terraform_remote_state.stateful.outputs.public_subnets
  task_execution_role_arn  = aws_iam_role.ecs_task_execution_role.arn
  
  health_check_path     = "/health"
  health_check_matcher  = "200,405"
}