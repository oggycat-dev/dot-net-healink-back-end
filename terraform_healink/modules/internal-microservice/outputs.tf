# Internal Microservice Module Outputs

output "service_name" {
  description = "ECS service name"
  value       = aws_ecs_service.microservice_service.name
}

output "ecs_service_name" {
  description = "ECS service name (alias for compatibility)"
  value       = aws_ecs_service.microservice_service.name
}

output "service_id" {
  description = "ECS service ID"
  value       = aws_ecs_service.microservice_service.id
}

output "task_definition_arn" {
  description = "Task definition ARN"
  value       = aws_ecs_task_definition.microservice_task.arn
}

output "ecs_task_definition_arn" {
  description = "Task definition ARN (alias for compatibility)"
  value       = aws_ecs_task_definition.microservice_task.arn
}

output "security_group_id" {
  description = "Security group ID for the service"
  value       = aws_security_group.microservice_sg.id
}

output "cloudwatch_log_group" {
  description = "CloudWatch log group name"
  value       = aws_cloudwatch_log_group.microservice_logs.name
}

output "cloudwatch_log_group_name" {
  description = "CloudWatch log group name (alias for compatibility)"
  value       = aws_cloudwatch_log_group.microservice_logs.name
}
