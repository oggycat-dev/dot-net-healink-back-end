# 🏗️ Healink CI/CD Architecture

## Overview Diagram

```
┌─────────────────────────────────────────────────────────────────────────┐
│                           GitHub Actions                                 │
│                                                                          │
│  ┌───────────────────┐    ┌──────────────────┐   ┌──────────────────┐ │
│  │  Code Push        │    │  Manual Trigger  │   │  Scheduled       │ │
│  │  (main branch)    │    │  (workflow_disp) │   │  (future)        │ │
│  └────────┬──────────┘    └────────┬─────────┘   └────────┬─────────┘ │
│           │                        │                       │           │
│           └────────────────────────┴───────────────────────┘           │
│                                    │                                    │
└────────────────────────────────────┼────────────────────────────────────┘
                                     │
                    ┌────────────────┴────────────────┐
                    │                                 │
         ┌──────────▼──────────┐         ┌──────────▼──────────┐
         │  🚀 Full Deploy     │         │  ☢️  Nuke AWS       │
         │  Workflow           │         │  Workflow           │
         └──────────┬──────────┘         └──────────┬──────────┘
                    │                               │
                    │                               │
      ┌─────────────┴─────────────┐    ┌───────────┴───────────┐
      │                           │    │                       │
┌─────▼──────┐           ┌────────▼────▼────┐      ┌─────────▼─────────┐
│ Build All  │           │  Terraform       │      │  Terraform        │
│ Services   │           │  Deploy          │      │  Destroy          │
└─────┬──────┘           └────────┬─────────┘      └─────────┬─────────┘
      │                           │                           │
      │                  ┌────────┴────────┐                 │
      │                  │                 │                 │
      │         ┌────────▼───────┐  ┌──────▼──────┐         │
      │         │ Stateful Infra │  │ App Infra   │         │
      │         │ (RDS, ECR)     │  │ (ECS, ALB)  │         │
      │         └────────┬───────┘  └──────┬──────┘         │
      └──────────────────┴──────────────────┘                │
                         │                                   │
                ┌────────▼────────┐                         │
                │                 │                         │
                │   AWS Cloud     │◄────────────────────────┘
                │                 │
                └─────────────────┘
```

## Full Deploy Workflow

```
┌─────────────────────────────────────────────────────────────────────┐
│                        FULL DEPLOY WORKFLOW                          │
└─────────────────────────────────────────────────────────────────────┘

Step 1: Build All Services (Parallel)
┌──────────────┐  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐
│ AuthService  │  │ UserService  │  │ ContentSvc   │  │ Gateway      │
│              │  │              │  │              │  │              │
│ Build Docker │  │ Build Docker │  │ Build Docker │  │ Build Docker │
│ Push to ECR  │  │ Push to ECR  │  │ Push to ECR  │  │ Push to ECR  │
└──────┬───────┘  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘
       │                 │                 │                 │
       └─────────────────┴─────────────────┴─────────────────┘
                                 │
                                 ▼
Step 2: Deploy Stateful Infrastructure
┌─────────────────────────────────────────────────────────────────────┐
│  Terraform Init → Workspace Select → Plan → Apply                   │
│                                                                      │
│  Resources Created:                                                  │
│  ✓ ECR Repositories (healink/*)                                     │
│  ✓ RDS PostgreSQL Instance (multi-database)                         │
│  ✓ Redis ElastiCache                                                │
│  ✓ VPC, Subnets, Security Groups                                    │
│  ✓ IAM Roles                                                        │
└─────────────────────────────────────────────────────────────────────┘
                                 │
                                 ▼
Step 3: Deploy Application Infrastructure
┌─────────────────────────────────────────────────────────────────────┐
│  Terraform Init → Workspace Select → Plan → Apply                   │
│                                                                      │
│  Resources Created:                                                  │
│  ✓ ECS Fargate Cluster                                              │
│  ✓ ECS Services (5x)                                                │
│  ✓ Application Load Balancers (5x)                                  │
│  ✓ Target Groups (5x)                                               │
│  ✓ CloudWatch Log Groups                                            │
│  ✓ Auto Scaling Policies                                            │
└─────────────────────────────────────────────────────────────────────┘
                                 │
                                 ▼
Step 4: Health Check
┌─────────────────────────────────────────────────────────────────────┐
│  Wait 60s → Check ALB URLs → Verify Services Running                │
└─────────────────────────────────────────────────────────────────────┘
                                 │
                                 ▼
                            ✅ DEPLOYED!
```

## Nuke AWS Workflow

```
┌─────────────────────────────────────────────────────────────────────┐
│                         NUKE AWS WORKFLOW                            │
└─────────────────────────────────────────────────────────────────────┘

Step 1: Safety Check
┌─────────────────────────────────────────────────────────────────────┐
│  Verify Confirmation = "NUKE"                                        │
│  Show what will be destroyed vs preserved                            │
└─────────────────────────────────────────────────────────────────────┘
                                 │
                                 ▼
Step 2: Destroy Application Infrastructure
┌─────────────────────────────────────────────────────────────────────┐
│  Terraform Destroy (app-infra)                                      │
│                                                                      │
│  Resources Destroyed:                                                │
│  ✗ ECS Services                                                     │
│  ✗ Application Load Balancers                                       │
│  ✗ Target Groups                                                    │
│  ✗ Security Groups (app)                                            │
│  ✗ CloudWatch Log Groups                                            │
│  ✗ IAM Roles (ECS Task Execution)                                   │
└─────────────────────────────────────────────────────────────────────┘
                                 │
                                 ▼
Step 3: Manual Cleanup (Safety Net)
┌─────────────────────────────────────────────────────────────────────┐
│  Force Delete ECS Services                                           │
│  Delete Remaining ALBs                                               │
│  Delete Target Groups                                                │
│  Delete CloudWatch Logs                                              │
└─────────────────────────────────────────────────────────────────────┘
                                 │
                                 ▼
Step 4: Verify Safety
┌─────────────────────────────────────────────────────────────────────┐
│  ✓ ECR Repositories Still Exist                                     │
│  ✓ RDS Database Still Running                                       │
│  ✓ Redis Cache Still Running                                        │
│  ✓ Docker Images Intact                                             │
│  ✓ Database Data Preserved                                          │
└─────────────────────────────────────────────────────────────────────┘
                                 │
                                 ▼
                      ✅ NUKED SAFELY!
```

## Service Architecture on AWS

```
┌─────────────────────────────────────────────────────────────────────┐
│                           AWS CLOUD                                  │
│                                                                      │
│  ┌────────────────────────────────────────────────────────────────┐│
│  │                      Internet Gateway                           ││
│  └───────────────────────┬────────────────────────────────────────┘│
│                          │                                          │
│  ┌───────────────────────▼────────────────────────────────────────┐│
│  │              Application Load Balancer (Public)                 ││
│  │                                                                  ││
│  │  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐       ││
│  │  │ Auth ALB │  │ User ALB │  │ Content  │  │ Gateway  │       ││
│  │  └────┬─────┘  └────┬─────┘  │   ALB    │  │   ALB    │       ││
│  │       │             │         └────┬─────┘  └────┬─────┘       ││
│  └───────┼─────────────┼──────────────┼─────────────┼─────────────┘│
│          │             │              │             │               │
│  ┌───────▼─────────────▼──────────────▼─────────────▼─────────────┐│
│  │                    ECS Fargate Cluster                          ││
│  │                                                                  ││
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐            ││
│  │  │ Auth Tasks  │  │ User Tasks  │  │Content Tasks│            ││
│  │  │ (1-3 tasks) │  │ (1-3 tasks) │  │ (1-3 tasks) │            ││
│  │  └──────┬──────┘  └──────┬──────┘  └──────┬──────┘            ││
│  │         │                │                │                     ││
│  │         └────────────────┴────────────────┘                     ││
│  │                          │                                       ││
│  └──────────────────────────┼───────────────────────────────────┘ │
│                             │                                       │
│  ┌──────────────────────────┴──────────────────────────────────┐  │
│  │                    Data Layer                                 │  │
│  │                                                               │  │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐         │  │
│  │  │ RDS         │  │ Redis       │  │ ECR         │         │  │
│  │  │ PostgreSQL  │  │ ElastiCache │  │ Registries  │         │  │
│  │  │             │  │             │  │             │         │  │
│  │  │ Multi-DB:   │  │ Cache       │  │ Docker      │         │  │
│  │  │ - Auth      │  │ Distributed │  │ Images      │         │  │
│  │  │ - User      │  │ State       │  │             │         │  │
│  │  │ - Content   │  │             │  │             │         │  │
│  │  └─────────────┘  └─────────────┘  └─────────────┘         │  │
│  │                                                               │  │
│  └───────────────────────────────────────────────────────────────┘  │
│                                                                      │
└──────────────────────────────────────────────────────────────────────┘
```

## Cost Breakdown

### Full Running System (Dev)

```
┌──────────────────────────────────────────────────────────────┐
│                    FULL SYSTEM COSTS                          │
├──────────────────────────────────────────────────────────────┤
│                                                               │
│  ECS Fargate (5 services @ 256 CPU, 512 MB)                 │
│  - Auth Service:        $6-10/month                          │
│  - User Service:        $6-10/month                          │
│  - Content Service:     $6-10/month                          │
│  - Notification Svc:    $6-10/month                          │
│  - Gateway:             $6-10/month                          │
│                         ──────────────                        │
│  Subtotal:              $30-50/month                         │
│                                                               │
│  Application Load Balancers (5x)                             │
│  - $16/month each       $80/month (but can share 1 ALB)     │
│  - Optimized:           $16/month                            │
│                                                               │
│  RDS PostgreSQL (t4g.micro)                                  │
│  - Instance:            $12/month                            │
│  - Storage (20GB):      $3/month                             │
│                         ──────────                            │
│  Subtotal:              $15/month                            │
│                                                               │
│  Redis ElastiCache (t4g.micro)                               │
│  - Instance:            $12/month                            │
│                                                               │
│  ECR Storage (~5GB)                                          │
│  - $0.10/GB/month:      $0.50/month                          │
│                                                               │
│  CloudWatch Logs (~1GB)                                      │
│  - $0.50/GB:            $0.50/month                          │
│                                                               │
├──────────────────────────────────────────────────────────────┤
│  TOTAL (Dev):           ~$74-94/month                        │
└──────────────────────────────────────────────────────────────┘
```

### After Nuke (Keep ECR + RDS)

```
┌──────────────────────────────────────────────────────────────┐
│                     AFTER NUKE COSTS                          │
├──────────────────────────────────────────────────────────────┤
│                                                               │
│  ECS Fargate:           $0/month   ✗ Deleted                │
│  ALB:                   $0/month   ✗ Deleted                │
│  CloudWatch Logs:       $0/month   ✗ Deleted                │
│                                                               │
│  RDS PostgreSQL:        $15/month  ✓ Preserved              │
│  Redis Cache:           $12/month  ✓ Preserved              │
│  ECR Storage:           $0.50/month ✓ Preserved             │
│                                                               │
├──────────────────────────────────────────────────────────────┤
│  TOTAL (After Nuke):    ~$27.50/month                       │
│  SAVINGS:               ~$46.50-66.50/month (63-70% saved)  │
└──────────────────────────────────────────────────────────────┘
```

## Deployment Timeline

```
Full Deploy Timeline (~15-20 minutes)
├─ Build Services (5 parallel builds)    [5-8 minutes]
│  ├─ AuthService                        [1-2 min]
│  ├─ UserService                        [1-2 min]
│  ├─ ContentService                     [1-2 min]
│  ├─ NotificationService                [1-2 min]
│  └─ Gateway                            [1-2 min]
│
├─ Deploy Stateful Infrastructure        [3-5 minutes]
│  ├─ Terraform Init                     [30s]
│  ├─ Terraform Plan                     [1 min]
│  └─ Terraform Apply                    [2-3 min]
│
├─ Deploy Application Infrastructure     [5-8 minutes]
│  ├─ Terraform Init                     [30s]
│  ├─ Terraform Plan                     [1 min]
│  ├─ Terraform Apply                    [3-5 min]
│  └─ ECS Tasks Starting                 [1-2 min]
│
└─ Health Check                          [1-2 minutes]
   ├─ Wait for services                  [1 min]
   └─ Verify endpoints                   [30s]

─────────────────────────────────────────────────────────────
Total: 15-20 minutes


Nuke AWS Timeline (~5-10 minutes)
├─ Safety Check                          [10s]
│
├─ Destroy Application Infrastructure    [3-5 minutes]
│  ├─ Terraform Destroy                  [2-3 min]
│  └─ Wait for resources                 [1-2 min]
│
├─ Manual Cleanup                        [2-3 minutes]
│  ├─ Force delete ECS services          [1 min]
│  ├─ Delete ALBs                        [1 min]
│  └─ Delete Target Groups               [30s]
│
├─ CloudWatch Logs Cleanup               [1 minute]
│
└─ Verify Safety                         [30s]
   ├─ Check ECR exists                   [10s]
   └─ Check RDS exists                   [10s]

─────────────────────────────────────────────────────────────
Total: 5-10 minutes
```

## Security Model

```
┌─────────────────────────────────────────────────────────────────┐
│                      SECURITY LAYERS                             │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  Layer 1: GitHub Actions → AWS (OIDC)                          │
│  ┌────────────────────────────────────────────────────────────┐│
│  │  GitHub Actions uses OIDC to assume IAM Role               ││
│  │  No static credentials stored in GitHub                    ││
│  │  Role: GitHubActionRole-Healink                            ││
│  └────────────────────────────────────────────────────────────┘│
│                                                                  │
│  Layer 2: Terraform State (S3 Backend)                         │
│  ┌────────────────────────────────────────────────────────────┐│
│  │  State stored in S3: healink-tf-state-2025-oggycatdev     ││
│  │  Separate state files:                                     ││
│  │  - stateful/terraform.tfstate                              ││
│  │  - app-infra/terraform.tfstate                             ││
│  └────────────────────────────────────────────────────────────┘│
│                                                                  │
│  Layer 3: Service Isolation                                    │
│  ┌────────────────────────────────────────────────────────────┐│
│  │  Each service runs in isolated ECS tasks                   ││
│  │  Security groups restrict inter-service communication      ││
│  │  Secrets managed via GitHub Secrets                        ││
│  └────────────────────────────────────────────────────────────┘│
│                                                                  │
│  Layer 4: Data Protection                                      │
│  ┌────────────────────────────────────────────────────────────┐│
│  │  RDS: Encrypted at rest, automated backups                 ││
│  │  ECR: Image scanning enabled                               ││
│  │  Nuke workflow: Cannot delete RDS/ECR by default           ││
│  └────────────────────────────────────────────────────────────┘│
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

---

**Last Updated**: September 30, 2025  
**Version**: 2.0  
**Status**: Production Ready
