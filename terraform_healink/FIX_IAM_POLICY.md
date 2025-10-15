# 🔐 Fix IAM Policy cho GitHub Actions

## ❌ Lỗi hiện tại

```
User: arn:aws:sts::855160720656:assumed-role/GitHubActionRole-Healink/GitHubActions 
is not authorized to perform: s3:PutObject on resource: 
"arn:aws:s3:::healink-tf-state-2025-oggycatdev/env:/free/stateful/terraform.tfstate" 
because no identity-based policy allows the s3:PutObject action
```

IAM Role `GitHubActionRole-Healink` thiếu quyền truy cập S3 bucket để lưu Terraform state.

---

## ✅ Giải pháp: Update IAM Policy

### Bước 1: Mở AWS Console

1. Đăng nhập vào [AWS Console](https://console.aws.amazon.com/)
2. Vào **IAM** service
3. Click **Roles** → Tìm role `GitHubActionRole-Healink`

### Bước 2: Update Policy

Click vào role → Tab **Permissions** → Click **Add permissions** → **Create inline policy**

### Bước 3: Thêm JSON Policy này

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Sid": "TerraformStateS3Access",
      "Effect": "Allow",
      "Action": [
        "s3:PutObject",
        "s3:GetObject",
        "s3:DeleteObject",
        "s3:ListBucket"
      ],
      "Resource": [
        "arn:aws:s3:::healink-tf-state-2025-oggycatdev",
        "arn:aws:s3:::healink-tf-state-2025-oggycatdev/*"
      ]
    },
    {
      "Sid": "TerraformStateLocking",
      "Effect": "Allow",
      "Action": [
        "dynamodb:PutItem",
        "dynamodb:GetItem",
        "dynamodb:DeleteItem",
        "dynamodb:DescribeTable"
      ],
      "Resource": "arn:aws:dynamodb:ap-southeast-2:855160720656:table/healink-tf-lock"
    }
  ]
}
```

### Bước 4: Đặt tên cho Policy

- **Policy name**: `TerraformStateAccess`
- Click **Create policy**

---

## 🚀 Alternative: Sử dụng AWS CLI

Nếu bạn có AWS CLI đã cấu hình:

```bash
# 1. Tạo file policy
cat > /tmp/terraform-state-policy.json << 'EOF'
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Sid": "TerraformStateS3Access",
      "Effect": "Allow",
      "Action": [
        "s3:PutObject",
        "s3:GetObject",
        "s3:DeleteObject",
        "s3:ListBucket"
      ],
      "Resource": [
        "arn:aws:s3:::healink-tf-state-2025-oggycatdev",
        "arn:aws:s3:::healink-tf-state-2025-oggycatdev/*"
      ]
    },
    {
      "Sid": "TerraformStateLocking",
      "Effect": "Allow",
      "Action": [
        "dynamodb:PutItem",
        "dynamodb:GetItem",
        "dynamodb:DeleteItem",
        "dynamodb:DescribeTable"
      ],
      "Resource": "arn:aws:dynamodb:ap-southeast-2:855160720656:table/healink-tf-lock"
    }
  ]
}
EOF

# 2. Attach policy vào role
aws iam put-role-policy \
  --role-name GitHubActionRole-Healink \
  --policy-name TerraformStateAccess \
  --policy-document file:///tmp/terraform-state-policy.json

# 3. Verify
aws iam get-role-policy \
  --role-name GitHubActionRole-Healink \
  --policy-name TerraformStateAccess
```

---

## ✔️ Kiểm tra sau khi update

1. Vào GitHub Repository → **Actions**
2. Click **Re-run failed jobs**
3. Workflow sẽ chạy thành công ✅

---

## 📝 Giải thích

### Tại sao cần các quyền này?

| Permission | Mục đích |
|-----------|----------|
| `s3:PutObject` | Lưu terraform state file lên S3 |
| `s3:GetObject` | Đọc terraform state hiện tại |
| `s3:DeleteObject` | Xoá state cũ khi cần |
| `s3:ListBucket` | List các object trong bucket |
| `dynamodb:*` | State locking để tránh conflict khi nhiều người deploy |

### Terraform Workspace và S3 Key

Khi dùng Terraform workspace, state file path sẽ là:

```
Default workspace:     s3://bucket/stateful/terraform.tfstate
Workspace "free":      s3://bucket/env:/free/stateful/terraform.tfstate
Workspace "dev":       s3://bucket/env:/dev/stateful/terraform.tfstate
Workspace "prod":      s3://bucket/env:/prod/stateful/terraform.tfstate
```

Policy trên cho phép truy cập **TẤT CẢ** các path trong bucket bằng wildcard `/*`.

---

## 🆘 Nếu vẫn lỗi

1. **Check region**: Đảm bảo S3 bucket và DynamoDB table ở đúng region `ap-southeast-2`
2. **Check trust relationship**: Role phải trust GitHub OIDC provider
3. **Check bucket exists**: Bucket `healink-tf-state-2025-oggycatdev` phải tồn tại
4. **Check DynamoDB table**: Table `healink-tf-lock` phải tồn tại (nếu dùng state locking)

### Kiểm tra trust relationship:

```bash
aws iam get-role \
  --role-name GitHubActionRole-Healink \
  --query 'Role.AssumeRolePolicyDocument'
```

Phải có trust cho GitHub OIDC:

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Principal": {
        "Federated": "arn:aws:iam::855160720656:oidc-provider/token.actions.githubusercontent.com"
      },
      "Action": "sts:AssumeRoleWithWebIdentity",
      "Condition": {
        "StringEquals": {
          "token.actions.githubusercontent.com:aud": "sts.amazonaws.com"
        },
        "StringLike": {
          "token.actions.githubusercontent.com:sub": "repo:oggycat-dev/dot-net-healink-back-end:*"
        }
      }
    }
  ]
}
```

---

## 📞 Liên hệ

Nếu còn vướng mắc, hãy check:
- GitHub Actions logs: Chi tiết lỗi
- AWS CloudTrail: Xem exact permission nào bị deny
- IAM Policy Simulator: Test policy trước khi apply

