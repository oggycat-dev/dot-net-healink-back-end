terraform {
  backend "s3" {
    bucket = "healink-tf-state-2025-oggycatdev" # Tên bucket S3 của bạn
    key    = "global/terraform.tfstate"
    region = "ap-southeast-2"
  }
}

provider "aws" {
  region = "ap-southeast-2"
}

# --- HẠ TẦNG NỀN ---

resource "aws_ecr_repository" "auth_service_repo" {
  name = "healink/auth-service"
}

resource "aws_ecs_cluster" "main" {
  name = "healink-cluster"
}

# --- HẠ TẦNG ỨNG DỤNG ---

variable "app_image_tag" {
  type        = string
  description = "The Docker image tag to deploy."
}

resource "aws_iam_role" "ecs_task_execution_role" {
  name = "healink-ecs-task-execution-role"
  assume_role_policy = jsonencode({
    Version   = "2012-10-17"
    Statement = [ { Action = "sts:AssumeRole", Effect = "Allow", Principal = { Service = "ecs-tasks.amazonaws.com" } } ]
  })
}

resource "aws_iam_role_policy_attachment" "ecs_task_execution_role_policy" {
  role       = aws_iam_role.ecs_task_execution_role.name
  policy_arn = "arn:aws:iam::aws:policy/service-role/AmazonECSTaskExecutionRolePolicy"
}

resource "aws_ecs_task_definition" "auth_service" {
  family                   = "auth-service-task"
  network_mode             = "awsvpc"
  requires_compatibilities = ["FARGATE"]
  cpu                      = "256"
  memory                   = "512"
  execution_role_arn       = aws_iam_role.ecs_task_execution_role.arn

  container_definitions = jsonencode([
    {
      name      = "auth-service-container"
      image     = "${aws_ecr_repository.auth_service_repo.repository_url}:${var.app_image_tag}"
      essential = true
      portMappings = [
        { containerPort = 80, hostPort = 80 }
      ]
      # --- PHẦN MỚI ---
      secrets = [
        # Biến môi trường DB_PASSWORD sẽ được điền từ secret
        {
          name      = "DB_PASSWORD",
          valueFrom = aws_secretsmanager_secret.db_password.arn
        }
      ]
      environment = [
        # Tự động xây dựng Connection String hoàn chỉnh
        {
          name  = "ConnectionStrings__DefaultConnection",
          value = "Host=${aws_db_instance.healink_db.address};Database=${aws_db_instance.healink_db.db_name};Username=${aws_db_instance.healink_db.username};Password=${aws_secretsmanager_secret.db_password.arn}"
        }
      ]
    }
  ])
}

resource "aws_ecs_service" "auth_service" {
  name            = "auth-service"
  cluster         = aws_ecs_cluster.main.id
  task_definition = aws_ecs_task_definition.auth_service.arn
  desired_count   = 1
  launch_type     = "FARGATE"
  network_configuration {
    subnets         = ["subnet-00d0aabb44d3b86f4"] # ID Subnet của bạn
    security_groups = ["sg-0b078db7777c9f84a"]   # ID Security Group của bạn
    assign_public_ip = true
  }
}

# --- SECRETS MANAGER: Lưu trữ mật khẩu an toàn ---

# Yêu cầu Terraform tạo một mật khẩu ngẫu nhiên, mạnh
resource "random_password" "db_master_password" {
  length           = 16
  special          = true
  override_special = "!#$%&*()-_=+[]{}<>:?"
}

# Tạo một "két sắt" (secret) để chứa mật khẩu
resource "aws_secretsmanager_secret" "db_password" {
  name = "healink/db_password"
}

# Đặt phiên bản đầu tiên của secret bằng mật khẩu ngẫu nhiên đã tạo
resource "aws_secretsmanager_secret_version" "db_password_version" {
  secret_id     = aws_secretsmanager_secret.db_password.id
  secret_string = random_password.db_master_password.result
}

# --- AMAZON RDS: Database PostgreSQL ---

resource "aws_db_instance" "healink_db" {
  identifier           = "healink-db-instance"
  allocated_storage    = 20             # 20 GB, đủ cho Free Tier
  instance_class       = "db.t3.micro"  # Loại instance nằm trong Free Tier
  engine               = "postgres"
  engine_version       = "15"           # Chọn phiên bản PostgreSQL

  db_name              = "AuthServiceDB" # Tên database ban đầu
  username             = "postgres_admin"
  # Lấy mật khẩu an toàn đã được tạo ở bước 1
  password             = random_password.db_master_password.result

  # Kết nối database với cùng mạng và security group của ứng dụng
  # !!! QUAN TRỌNG: Thay bằng Security Group ID chính xác của anh
  vpc_security_group_ids = ["sg-0b078db7777c9f84a"]

  publicly_accessible  = true # Tạm thời cho phép truy cập public để dễ debug
  skip_final_snapshot  = true # Không tạo snapshot cuối cùng khi xóa (tiết kiệm chi phí)
}

resource "aws_iam_role_policy" "ecs_secrets_policy" {
  name = "ecs-secrets-manager-policy"
  role = aws_iam_role.ecs_task_execution_role.id

  policy = jsonencode({
    Version = "2012-10-17",
    Statement = [
      {
        Effect   = "Allow",
        Action   = ["secretsmanager:GetSecretValue"],
        Resource = [aws_secretsmanager_secret.db_password.arn]
      }
    ]
  })
}