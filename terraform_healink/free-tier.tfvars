# =========================================
# FREE TIER CONFIGURATION
# Optimized for AWS Free Tier to minimize costs
# =========================================

# Environment & Project
environment  = "dev"  # Keep as "dev" for free tier settings
project_name = "healink-free"

# Database (RDS PostgreSQL)
db_instance_class       = "db.t3.micro"        # Free tier eligible
db_allocated_storage    = 20                   # Free tier: 20GB
db_backup_retention_period = 1                 # Minimum retention

# Cache (ElastiCache Redis)  
redis_node_type = "cache.t3.micro"             # Smallest instance

# ECS Fargate
ecs_task_cpu    = "256"                        # 0.25 vCPU
ecs_task_memory = "512"                        # 0.5 GB
ecs_desired_count = 1                          # Single instance per service

# Message Queue (Amazon MQ RabbitMQ)
mq_instance_type   = "mq.t3.micro"             # Free tier eligible
mq_deployment_mode = "SINGLE_INSTANCE"         # No HA for cost saving

# Application Settings
aspnetcore_environment = "Development"
allowed_origins = "http://localhost:3000,http://localhost:8080,http://localhost:5010"

