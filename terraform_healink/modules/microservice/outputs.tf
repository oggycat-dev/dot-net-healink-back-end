# Microservice Module Outputs
# These outputs can be used by the parent module

output "alb_dns_name" {
  description = "DNS name of the load balancer"
  value       = aws_lb.microservice_alb.dns_name
}

output "alb_arn" {
  description = "ARN of the load balancer"
  value       = aws_lb.microservice_alb.arn
}

output "target_group_arn" {
  description = "ARN of the target group"
  value       = aws_lb_target_group.microservice_tg.arn
}

output "ecs_service_name" {
  description = "Name of the ECS service"
  value       = aws_ecs_service.microservice_service.name
}

output "ecs_task_definition_arn" {
  description = "ARN of the ECS task definition"
  value       = aws_ecs_task_definition.microservice_task.arn
}

output "security_group_id" {
  description = "ID of the microservice security group"
  value       = aws_security_group.microservice_sg.id
}

output "alb_security_group_id" {
  description = "ID of the ALB security group"
  value       = aws_security_group.alb_sg.id
}

output "cloudwatch_log_group_name" {
  description = "Name of the CloudWatch log group"
  value       = aws_cloudwatch_log_group.microservice_logs.name
}