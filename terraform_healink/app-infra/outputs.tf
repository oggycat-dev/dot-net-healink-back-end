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

output "product_service_alb_dns" {
  description = "Product Service ALB DNS name"
  value       = module.product_service.alb_dns_name
}

output "product_service_url" {
  description = "Product Service public URL"
  value       = "http://${module.product_service.alb_dns_name}"
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
    product_service = {
      build = "docker build -t ${data.terraform_remote_state.stateful.outputs.product_service_ecr_url}:latest -f src/ProductService/ProductService.API/Dockerfile ."
      push  = "docker push ${data.terraform_remote_state.stateful.outputs.product_service_ecr_url}:latest"
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
      auth_service    = data.terraform_remote_state.stateful.outputs.auth_service_ecr_url
      product_service = data.terraform_remote_state.stateful.outputs.product_service_ecr_url
      gateway         = data.terraform_remote_state.stateful.outputs.gateway_ecr_url
    }
  }
}