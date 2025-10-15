# ==============================================
# DEVELOPMENT ENVIRONMENT CONFIGURATION
# ==============================================

# Basic Configuration
environment = "dev"
project_name = "healink-dev"

# Database Configuration
db_instance_class = "db.t3.micro"
db_allocated_storage = 20
db_backup_retention_period = 1

# Redis Configuration
redis_node_type = "cache.t4g.micro"

# ECS Configuration
ecs_task_cpu = "256"
ecs_task_memory = "512"
ecs_desired_count = 1

# RabbitMQ Configuration
rabbitmq_instance_type = "mq.t3.micro"
rabbitmq_deployment_mode = "SINGLE_INSTANCE"

# Application Settings
aspnetcore_environment = "Development"
allowed_origins = "http://localhost:3000,http://localhost:8080"

