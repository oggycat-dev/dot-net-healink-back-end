# Outputs for Application Infrastructure

# === MICROSERVICE OUTPUTS ===
output "gateway_alb_dns" {
  description = "Gateway ALB DNS name"
  value       = module.gateway.alb_dns_name
}

output "gateway_url" {
  description = "Gateway public URL"
  value       = "http://${module.gateway.alb_dns_name}"
}

output "auth_service_alb_dns" {
  description = "Auth Service ALB DNS name"
  value       = module.auth_service.alb_dns_name
}

output "auth_service_url" {
  description = "Auth Service public URL"
  value       = "http://${module.auth_service.alb_dns_name}"
}

output "user_service_alb_dns" {
  description = "User Service ALB DNS name"
  value       = module.user_service.alb_dns_name
}

output "user_service_url" {
  description = "User Service public URL"
  value       = "http://${module.user_service.alb_dns_name}"
}

output "content_service_alb_dns" {
  description = "Content Service ALB DNS name"
  value       = module.content_service.alb_dns_name
}

output "content_service_url" {
  description = "Content Service public URL"
  value       = "http://${module.content_service.alb_dns_name}"
}

output "notification_service_alb_dns" {
  description = "Notification Service ALB DNS name"
  value       = module.notification_service.alb_dns_name
}

output "notification_service_url" {
  description = "Notification Service public URL"
  value       = "http://${module.notification_service.alb_dns_name}"
}

output "subscription_service_alb_dns" {
  description = "Subscription Service ALB DNS name"
  value       = module.subscription_service.alb_dns_name
}

output "subscription_service_url" {
  description = "Subscription Service public URL"
  value       = "http://${module.subscription_service.alb_dns_name}"
}

output "payment_service_alb_dns" {
  description = "Payment Service ALB DNS name"
  value       = module.payment_service.alb_dns_name
}

output "payment_service_url" {
  description = "Payment Service public URL"
  value       = "http://${module.payment_service.alb_dns_name}"
}

# === ENVIRONMENT INFO ===
output "environment" {
  description = "Current Terraform workspace/environment"
  value       = terraform.workspace
}

output "cluster_name" {
  description = "ECS cluster name"
  value       = aws_ecs_cluster.healink_cluster.name
}

# === QUICK ACCESS COMMANDS ===
output "docker_commands" {
  description = "Docker commands for building and pushing images"
  value = {
    auth_service = {
      build = "docker build -t ${data.terraform_remote_state.stateful.outputs.auth_service_ecr_url}:latest -f src/AuthService/AuthService.API/Dockerfile ."
      push  = "docker push ${data.terraform_remote_state.stateful.outputs.auth_service_ecr_url}:latest"
    }
    user_service = {
      build = "docker build -t ${data.terraform_remote_state.stateful.outputs.user_service_ecr_url}:latest -f src/UserService/UserService.API/Dockerfile ."
      push  = "docker push ${data.terraform_remote_state.stateful.outputs.user_service_ecr_url}:latest"
    }
    content_service = {
      build = "docker build -t ${data.terraform_remote_state.stateful.outputs.content_service_ecr_url}:latest -f src/ContentService/ContentService.API/Dockerfile ."
      push  = "docker push ${data.terraform_remote_state.stateful.outputs.content_service_ecr_url}:latest"
    }
    notification_service = {
      build = "docker build -t ${data.terraform_remote_state.stateful.outputs.notification_service_ecr_url}:latest -f src/NotificaitonService/NotificaitonService.API/Dockerfile ."
      push  = "docker push ${data.terraform_remote_state.stateful.outputs.notification_service_ecr_url}:latest"
    }
    subscription_service = {
      build = "docker build -t ${data.terraform_remote_state.stateful.outputs.subscription_service_ecr_url}:latest -f src/SubscriptionService/SubscriptionService.API/Dockerfile ."
      push  = "docker push ${data.terraform_remote_state.stateful.outputs.subscription_service_ecr_url}:latest"
    }
    payment_service = {
      build = "docker build -t ${data.terraform_remote_state.stateful.outputs.payment_service_ecr_url}:latest -f src/PaymentService/PaymentService.API/Dockerfile ."
      push  = "docker push ${data.terraform_remote_state.stateful.outputs.payment_service_ecr_url}:latest"
    }
    gateway = {
      build = "docker build -t ${data.terraform_remote_state.stateful.outputs.gateway_ecr_url}:latest -f src/Gateway/Gateway.API/Dockerfile ."
      push  = "docker push ${data.terraform_remote_state.stateful.outputs.gateway_ecr_url}:latest"
    }
  }
}

# === STATEFUL REFERENCES ===
output "stateful_info" {
  description = "Reference to stateful infrastructure"
  value = {
    database_endpoint  = data.terraform_remote_state.stateful.outputs.database_endpoint
    redis_endpoint     = data.terraform_remote_state.stateful.outputs.redis_endpoint
    rabbitmq_endpoint  = data.terraform_remote_state.stateful.outputs.rabbitmq_endpoint
    ecr_repositories = {
      auth_service         = data.terraform_remote_state.stateful.outputs.auth_service_ecr_url
      user_service         = data.terraform_remote_state.stateful.outputs.user_service_ecr_url
      content_service      = data.terraform_remote_state.stateful.outputs.content_service_ecr_url
      notification_service = data.terraform_remote_state.stateful.outputs.notification_service_ecr_url
      subscription_service = data.terraform_remote_state.stateful.outputs.subscription_service_ecr_url
      payment_service      = data.terraform_remote_state.stateful.outputs.payment_service_ecr_url
      gateway              = data.terraform_remote_state.stateful.outputs.gateway_ecr_url
    }
  }
}