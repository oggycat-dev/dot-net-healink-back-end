# Application Infrastructure Outputs
# Optimized: Only Gateway has public ALB

# === ECS CLUSTER ===
output "cluster_name" {
  description = "ECS cluster name"
  value       = aws_ecs_cluster.healink_cluster.name
}

output "cluster_arn" {
  description = "ECS cluster ARN"
  value       = aws_ecs_cluster.healink_cluster.arn
}

# === GATEWAY (PUBLIC) ===
output "gateway_url" {
  description = "Gateway ALB URL (public endpoint)"
  value       = module.gateway.alb_dns_name
}

output "gateway_alb_arn" {
  description = "Gateway ALB ARN"
  value       = module.gateway.alb_arn
}

output "gateway_service_name" {
  description = "Gateway ECS service name"
  value       = module.gateway.service_name
}

# === INTERNAL SERVICES ===
# These services don't have ALBs, only accessible via Gateway or internally

output "auth_service_name" {
  description = "Auth Service ECS service name (internal)"
  value       = module.auth_service.service_name
}

output "user_service_name" {
  description = "User Service ECS service name (internal)"
  value       = module.user_service.service_name
}

output "content_service_name" {
  description = "Content Service ECS service name (internal)"
  value       = module.content_service.service_name
}

output "notification_service_name" {
  description = "Notification Service ECS service name (internal)"
  value       = module.notification_service.service_name
}

output "subscription_service_name" {
  description = "Subscription Service ECS service name (internal)"
  value       = module.subscription_service.service_name
}

output "payment_service_name" {
  description = "Payment Service ECS service name (internal)"
  value       = module.payment_service.service_name
}

output "podcast_recommendation_service_name" {
  description = "Podcast Recommendation Service ECS service name (internal)"
  value       = module.podcast_recommendation_service.service_name
}

# === DEPLOYMENT INFO ===
output "deployment_summary" {
  description = "Deployment summary"
  value = {
    environment          = terraform.workspace
    cluster             = aws_ecs_cluster.healink_cluster.name
    gateway_endpoint    = module.gateway.alb_dns_name
    total_services      = 8
    public_services     = 1
    internal_services   = 7
    cost_optimization   = "Eliminated 7 ALBs, saves ~$96/month"
  }
}

# === CLOUDWATCH LOGS ===
output "gateway_logs" {
  description = "Gateway CloudWatch log group"
  value       = module.gateway.cloudwatch_log_group
}

output "auth_service_logs" {
  description = "Auth Service CloudWatch log group"
  value       = module.auth_service.cloudwatch_log_group
}

output "user_service_logs" {
  description = "User Service CloudWatch log group"
  value       = module.user_service.cloudwatch_log_group
}

output "content_service_logs" {
  description = "Content Service CloudWatch log group"
  value       = module.content_service.cloudwatch_log_group
}

output "notification_service_logs" {
  description = "Notification Service CloudWatch log group"
  value       = module.notification_service.cloudwatch_log_group
}

output "subscription_service_logs" {
  description = "Subscription Service CloudWatch log group"
  value       = module.subscription_service.cloudwatch_log_group
}

output "payment_service_logs" {
  description = "Payment Service CloudWatch log group"
  value       = module.payment_service.cloudwatch_log_group
}

output "podcast_recommendation_service_logs" {
  description = "Podcast Recommendation Service CloudWatch log group"
  value       = module.podcast_recommendation_service.cloudwatch_log_group
}
