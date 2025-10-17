# =========================================
# DEVELOPMENT ENVIRONMENT CONFIGURATION
# =========================================

# Environment & Project
environment  = "dev"
project_name = "healink-dev"

# Database (RDS PostgreSQL)
db_instance_class       = "db.t3.micro"
db_allocated_storage    = 20
db_backup_retention_period = 1

# Cache (ElastiCache Redis)
redis_node_type = "cache.t4g.micro"

# ECS Fargate
ecs_task_cpu    = "256"
ecs_task_memory = "512"
ecs_desired_count = 1

# Message Queue (Amazon MQ RabbitMQ)
mq_instance_type   = "mq.t3.micro"
mq_deployment_mode = "SINGLE_INSTANCE"

# Application Settings
aspnetcore_environment = "Development"
allowed_origins = "http://localhost:3000,http://localhost:8080"

