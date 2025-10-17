# =========================================
# PRODUCTION ENVIRONMENT CONFIGURATION
# Higher resources for production workloads
# =========================================

# Environment & Project
environment  = "prod"
project_name = "healink-prod"

# Database (RDS PostgreSQL)
db_instance_class       = "db.t4g.small"       # Better performance
db_allocated_storage    = 50                   # More storage
db_backup_retention_period = 7                 # 7 days retention

# Cache (ElastiCache Redis)
redis_node_type = "cache.t4g.small"            # Better performance

# ECS Fargate
ecs_task_cpu    = "512"                        # 0.5 vCPU
ecs_task_memory = "1024"                       # 1 GB
ecs_desired_count = 2                          # HA with 2 instances

# Message Queue (Amazon MQ RabbitMQ)
mq_instance_type   = "mq.t3.small"
mq_deployment_mode = "ACTIVE_STANDBY_MULTI_AZ" # High Availability

# Application Settings
aspnetcore_environment = "Production"
allowed_origins = "https://yourdomain.com"     # Replace with your domain

