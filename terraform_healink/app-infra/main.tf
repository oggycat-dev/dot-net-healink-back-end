# Healink Application Infrastructure - Optimized for Free Tier
# Only Gateway has public ALB, other services are internal
# Saves ~$96/month (7 ALBs eliminated)

terraform {
  backend "s3" {
    bucket         = "healink-tf-state-2025-oggycatdev"
    key            = "app-infra/terraform.tfstate"
    region         = "ap-southeast-2"
    workspace_key_prefix = "env:"
  }
}

provider "aws" {
  region = "ap-southeast-2"
}

# --- DATA SOURCES ---
data "terraform_remote_state" "stateful" {
  backend = "s3"
  config = {
    bucket         = "healink-tf-state-2025-oggycatdev"
    key            = "env:/${terraform.workspace}/stateful/terraform.tfstate"
    region         = "ap-southeast-2"
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

# ============================================
# GATEWAY (PUBLIC - WITH ALB)
# ============================================
# Only service exposed to internet

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
      value = "Development"
    },
    {
      name  = "AUTH_SERVICE_URL"
      value = "http://${module.auth_service.service_name}.${var.project_name}-${terraform.workspace}.local"
    },
    {
      name  = "USER_SERVICE_URL"
      value = "http://${module.user_service.service_name}.${var.project_name}-${terraform.workspace}.local"
    },
    {
      name  = "CONTENT_SERVICE_URL"
      value = "http://${module.content_service.service_name}.${var.project_name}-${terraform.workspace}.local"
    },
    {
      name  = "NOTIFICATION_SERVICE_URL"
      value = "http://${module.notification_service.service_name}.${var.project_name}-${terraform.workspace}.local"
    },
    {
      name  = "SUBSCRIPTION_SERVICE_URL"
      value = "http://${module.subscription_service.service_name}.${var.project_name}-${terraform.workspace}.local"
    },
    {
      name  = "PAYMENT_SERVICE_URL"
      value = "http://${module.payment_service.service_name}.${var.project_name}-${terraform.workspace}.local"
    },
    {
      name  = "PODCAST_RECOMMENDATION_SERVICE_URL"
      value = "http://${module.podcast_recommendation_service.service_name}.${var.project_name}-${terraform.workspace}.local"
    },
    # JWT Configuration
    {
      name  = "JwtConfig__Key"
      value = var.jwt_secret
    },
    {
      name  = "JwtConfig__Issuer"
      value = "Healink"
    },
    {
      name  = "JwtConfig__Audience"
      value = "Healink.Users"
    },
    # Redis Configuration
    {
      name  = "ConnectionStrings__Redis"
      value = "${data.terraform_remote_state.stateful.outputs.redis_endpoint}:${data.terraform_remote_state.stateful.outputs.redis_port}"
    },
    {
      name  = "Redis__Password"
      value = var.redis_password
    },
    # SharedLibrary RedisConfig (used by RedisConfiguration)
    {
      name  = "RedisConfig__Enabled"
      value = "true"
    },
    {
      name  = "RedisConfig__ConnectionString"
      value = "${data.terraform_remote_state.stateful.outputs.redis_endpoint}:${data.terraform_remote_state.stateful.outputs.redis_port},password=${var.redis_password}"
    }
  ]
  
  vpc_id                    = data.terraform_remote_state.stateful.outputs.vpc_id
  subnet_ids               = data.terraform_remote_state.stateful.outputs.public_subnets
  alb_subnet_ids           = data.terraform_remote_state.stateful.outputs.public_subnets
  task_execution_role_arn  = aws_iam_role.ecs_task_execution_role.arn
  
  health_check_path     = "/health"
  health_check_matcher  = "200"
}

# ============================================
# INTERNAL SERVICES (NO ALB)
# ============================================
# These services communicate via Gateway or internal service discovery

# --- AUTH SERVICE ---
module "auth_service" {
  source = "../modules/internal-microservice"

  service_name     = "auth-service"
  environment      = terraform.workspace
  project_name     = var.project_name
  
  ecs_cluster_name = aws_ecs_cluster.healink_cluster.name
  task_cpu         = "256"
  task_memory      = "512"
  desired_count    = 1
  
  docker_image     = "${data.terraform_remote_state.stateful.outputs.auth_service_ecr_url}:latest"
  container_port   = 80
  
  environment_variables = [
    {
      name  = "ASPNETCORE_ENVIRONMENT"
      value = "Development"
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
      value = replace(data.terraform_remote_state.stateful.outputs.rabbitmq_endpoint, "amqps://", "")
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
    },
    # JWT Configuration
    {
      name  = "JwtConfig__Key"
      value = var.jwt_secret
    },
    {
      name  = "JwtConfig__Issuer"
      value = "Healink"
    },
    {
      name  = "JwtConfig__Audience"
      value = "Healink.Users"
    }
  ]
  
  vpc_id                    = data.terraform_remote_state.stateful.outputs.vpc_id
  subnet_ids               = data.terraform_remote_state.stateful.outputs.public_subnets
  task_execution_role_arn  = aws_iam_role.ecs_task_execution_role.arn
}

# --- USER SERVICE ---
module "user_service" {
  source = "../modules/internal-microservice"

  service_name     = "user-service"
  environment      = terraform.workspace
  project_name     = var.project_name
  
  ecs_cluster_name = aws_ecs_cluster.healink_cluster.name
  task_cpu         = "256"
  task_memory      = "512"
  desired_count    = 1
  
  docker_image     = "${data.terraform_remote_state.stateful.outputs.user_service_ecr_url}:latest"
  container_port   = 80
  
  environment_variables = [
    {
      name  = "ASPNETCORE_ENVIRONMENT"
      value = "Development"
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
      value = replace(data.terraform_remote_state.stateful.outputs.rabbitmq_endpoint, "amqps://", "")
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
    },
    # JWT Configuration
    {
      name  = "JwtConfig__Key"
      value = var.jwt_secret
    },
    {
      name  = "JwtConfig__Issuer"
      value = "Healink"
    },
    {
      name  = "JwtConfig__Audience"
      value = "Healink.Users"
    }
  ]
  
  vpc_id                    = data.terraform_remote_state.stateful.outputs.vpc_id
  subnet_ids               = data.terraform_remote_state.stateful.outputs.public_subnets
  task_execution_role_arn  = aws_iam_role.ecs_task_execution_role.arn
}

# --- CONTENT SERVICE ---
module "content_service" {
  source = "../modules/internal-microservice"

  service_name     = "content-service"
  environment      = terraform.workspace
  project_name     = var.project_name
  
  ecs_cluster_name = aws_ecs_cluster.healink_cluster.name
  task_cpu         = "256"
  task_memory      = "512"
  desired_count    = 1
  
  docker_image     = "${data.terraform_remote_state.stateful.outputs.content_service_ecr_url}:latest"
  container_port   = 80
  
  environment_variables = [
    {
      name  = "ASPNETCORE_ENVIRONMENT"
      value = "Development"
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
      value = replace(data.terraform_remote_state.stateful.outputs.rabbitmq_endpoint, "amqps://", "")
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
    },
    # JWT Configuration
    {
      name  = "JWT__Secret"
      value = var.jwt_secret
    },
    {
      name  = "JWT__Issuer"
      value = "Healink"
    },
    {
      name  = "JWT__Audience"
      value = "Healink.Users"
    }
  ]
  
  vpc_id                    = data.terraform_remote_state.stateful.outputs.vpc_id
  subnet_ids               = data.terraform_remote_state.stateful.outputs.public_subnets
  task_execution_role_arn  = aws_iam_role.ecs_task_execution_role.arn
}

# --- NOTIFICATION SERVICE ---
module "notification_service" {
  source = "../modules/internal-microservice"

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
      value = "Development"
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
      value = replace(data.terraform_remote_state.stateful.outputs.rabbitmq_endpoint, "amqps://", "")
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
    },
    # JWT Configuration
    {
      name  = "JWT__Secret"
      value = var.jwt_secret
    },
    {
      name  = "JWT__Issuer"
      value = "Healink"
    },
    {
      name  = "JWT__Audience"
      value = "Healink.Users"
    }
  ]
  
  vpc_id                    = data.terraform_remote_state.stateful.outputs.vpc_id
  subnet_ids               = data.terraform_remote_state.stateful.outputs.public_subnets
  task_execution_role_arn  = aws_iam_role.ecs_task_execution_role.arn
}

# --- SUBSCRIPTION SERVICE ---
module "subscription_service" {
  source = "../modules/internal-microservice"

  service_name     = "subscription-service"
  environment      = terraform.workspace
  project_name     = var.project_name
  
  ecs_cluster_name = aws_ecs_cluster.healink_cluster.name
  task_cpu         = "256"
  task_memory      = "512"
  desired_count    = 1
  
  docker_image     = "${data.terraform_remote_state.stateful.outputs.subscription_service_ecr_url}:latest"
  container_port   = 80
  
  environment_variables = [
    {
      name  = "ASPNETCORE_ENVIRONMENT"
      value = "Development"
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
      value = replace(data.terraform_remote_state.stateful.outputs.rabbitmq_endpoint, "amqps://", "")
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
    },
    # JWT Configuration
    {
      name  = "JWT__Secret"
      value = var.jwt_secret
    },
    {
      name  = "JWT__Issuer"
      value = "Healink"
    },
    {
      name  = "JWT__Audience"
      value = "Healink.Users"
    }
  ]
  
  vpc_id                    = data.terraform_remote_state.stateful.outputs.vpc_id
  subnet_ids               = data.terraform_remote_state.stateful.outputs.public_subnets
  task_execution_role_arn  = aws_iam_role.ecs_task_execution_role.arn
}

# --- PAYMENT SERVICE ---
module "payment_service" {
  source = "../modules/internal-microservice"

  service_name     = "payment-service"
  environment      = terraform.workspace
  project_name     = var.project_name
  
  ecs_cluster_name = aws_ecs_cluster.healink_cluster.name
  task_cpu         = "256"
  task_memory      = "512"
  desired_count    = 1
  
  docker_image     = "${data.terraform_remote_state.stateful.outputs.payment_service_ecr_url}:latest"
  container_port   = 80
  
  environment_variables = [
    {
      name  = "ASPNETCORE_ENVIRONMENT"
      value = "Development"
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
      value = replace(data.terraform_remote_state.stateful.outputs.rabbitmq_endpoint, "amqps://", "")
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
    },
    # JWT Configuration
    {
      name  = "JWT__Secret"
      value = var.jwt_secret
    },
    {
      name  = "JWT__Issuer"
      value = "Healink"
    },
    {
      name  = "JWT__Audience"
      value = "Healink.Users"
    }
  ]
  
  vpc_id                    = data.terraform_remote_state.stateful.outputs.vpc_id
  subnet_ids               = data.terraform_remote_state.stateful.outputs.public_subnets
  task_execution_role_arn  = aws_iam_role.ecs_task_execution_role.arn
}

# --- PODCAST RECOMMENDATION SERVICE (.NET API) ---
module "podcast_recommendation_service" {
  source = "../modules/internal-microservice"

  service_name     = "podcast-recommendation-service"
  environment      = terraform.workspace
  project_name     = var.project_name
  
  ecs_cluster_name = aws_ecs_cluster.healink_cluster.name
  task_cpu         = "512"
  task_memory      = "1024"
  desired_count    = 1
  
  docker_image     = "${data.terraform_remote_state.stateful.outputs.podcast_recommendation_service_ecr_url}:latest"
  container_port   = 80  # .NET service port (ASPNETCORE_URLS: http://+:80)
  
  environment_variables = [
    {
      name  = "ENVIRONMENT"
      value = "production"
    },
    {
      name  = "DB_HOST"
      value = data.terraform_remote_state.stateful.outputs.database_endpoint
    },
    {
      name  = "DB_PORT"
      value = tostring(data.terraform_remote_state.stateful.outputs.database_port)
    },
    {
      name  = "DB_NAME"
      value = data.terraform_remote_state.stateful.outputs.database_name
    },
    {
      name  = "DB_USER"
      value = data.terraform_remote_state.stateful.outputs.database_username
    },
    {
      name  = "DB_PASSWORD"
      value = data.terraform_remote_state.stateful.outputs.database_password
    },
    {
      name  = "PODCAST_AI_SERVICE_URL"
      value = "http://podcast-ai-service.${var.project_name}.local:8000"
    }
  ]
  
  vpc_id                    = data.terraform_remote_state.stateful.outputs.vpc_id
  subnet_ids               = data.terraform_remote_state.stateful.outputs.public_subnets
  task_execution_role_arn  = aws_iam_role.ecs_task_execution_role.arn
}

# --- PODCAST AI SERVICE (FastAPI Python ML Service) ---
module "podcast_ai_service" {
  source = "../modules/internal-microservice"

  service_name     = "podcast-ai-service"
  environment      = terraform.workspace
  project_name     = var.project_name
  
  ecs_cluster_name = aws_ecs_cluster.healink_cluster.name
  task_cpu         = "1024"  # More CPU for ML models
  task_memory      = "2048"  # More memory for ML models
  desired_count    = 1
  
  docker_image     = "${data.terraform_remote_state.stateful.outputs.podcast_ai_service_ecr_url}:latest"
  container_port   = 8000  # FastAPI port
  
  environment_variables = [
    {
      name  = "ENVIRONMENT"
      value = "production"
    },
    {
      name  = "PYTHONUNBUFFERED"
      value = "1"
    },
    {
      name  = "USER_SERVICE_URL"
      value = "http://user-service.${var.project_name}.local:5002"
    },
    {
      name  = "CONTENT_SERVICE_URL"
      value = "http://content-service.${var.project_name}.local:5003"
    },
    {
      name  = "GATEWAY_URL"
      value = module.gateway.alb_dns_name
    },
    {
      name  = "MODEL_PATH"
      value = "/app/models"
    }
  ]
  
  vpc_id                    = data.terraform_remote_state.stateful.outputs.vpc_id
  subnet_ids               = data.terraform_remote_state.stateful.outputs.public_subnets
  task_execution_role_arn  = aws_iam_role.ecs_task_execution_role.arn
}
