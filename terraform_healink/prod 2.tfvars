# ==============================================
# PRODUCTION ENVIRONMENT CONFIGURATION
# ==============================================

# Basic Configuration
environment = "prod"
project_name = "healink-prod"

# Database Configuration
db_instance_class = "db.t4g.small"
db_allocated_storage = 50
db_backup_retention_period = 7

# Redis Configuration
redis_node_type = "cache.t4g.small"

# ECS Configuration
ecs_task_cpu = "512"
ecs_task_memory = "1024"
ecs_desired_count = 2

# RabbitMQ Configuration
rabbitmq_instance_type = "mq.t3.small"
rabbitmq_deployment_mode = "ACTIVE_STANDBY_MULTI_AZ"

# Application Settings
aspnetcore_environment = "Production"
allowed_origins = "https://yourdomain.com"

