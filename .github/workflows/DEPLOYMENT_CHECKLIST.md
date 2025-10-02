# ✅ Healink CI/CD Deployment Checklist

## 📋 Pre-Deployment Checklist

### AWS Configuration
- [ ] AWS Account created and accessible
- [ ] IAM Role `GitHubActionRole-Healink` exists
  - ARN: `arn:aws:iam::855160720656:role/GitHubActionRole-Healink`
  - Trust policy includes GitHub OIDC
  - Permissions: ECR, ECS, RDS, ALB, CloudWatch, IAM PassRole
- [ ] S3 Bucket for Terraform state exists
  - Bucket: `healink-tf-state-2025-oggycatdev`
  - Region: `ap-southeast-2`
  - Versioning enabled
- [ ] VPC and Subnets configured
  - VPC ID: `vpc-08fe88c24397c79a9`
  - Subnet 1: `subnet-00d0aabb44d3b86f4`
  - Subnet 2: `subnet-0cf7a8a098483c77e`

### GitHub Configuration
- [ ] Repository forked/cloned
- [ ] GitHub Secrets configured:
  - [ ] `JWT_ISSUER`
  - [ ] `JWT_AUDIENCE`
  - [ ] `JWT_SECRET_KEY`
  - [ ] `REDIS_CONNECTION_STRING`
- [ ] GitHub Actions enabled
- [ ] Workflow files present in `.github/workflows/`

### Local Development
- [ ] Docker installed and running
- [ ] Docker Compose works locally
- [ ] All services build successfully
- [ ] All tests pass locally
- [ ] `.env` file configured properly

---

## 🚀 First Deployment Checklist

### Step 1: Verify Prerequisites
```bash
# Check AWS CLI configured
aws sts get-caller-identity

# Check Terraform installed
terraform --version

# Check Docker installed
docker --version

# Check Docker Compose
docker-compose --version
```

- [ ] All commands return valid output
- [ ] AWS CLI shows correct account ID (855160720656)

### Step 2: Review Configuration
- [ ] Review `terraform_healink/dev.tfvars`
- [ ] Review `terraform_healink/stateful-infra/main.tf`
- [ ] Review `terraform_healink/app-infra/main.tf`
- [ ] Confirm VPC and subnet IDs are correct
- [ ] Confirm ECR repository names match

### Step 3: Run Full Deploy
- [ ] Go to GitHub Actions tab
- [ ] Select "🚀 Full Deploy - All Services"
- [ ] Click "Run workflow"
- [ ] Select environment: `dev`
- [ ] Leave skip_build: `false`
- [ ] Click "Run workflow"
- [ ] Monitor workflow progress

### Step 4: Wait for Completion (~15-20 minutes)
- [ ] Build phase completed (5-8 min)
- [ ] Stateful infrastructure deployed (3-5 min)
- [ ] Application infrastructure deployed (5-8 min)
- [ ] Health checks passed (1-2 min)

### Step 5: Verify Deployment
```bash
# Get ALB URLs from Terraform output
cd terraform_healink/app-infra
terraform output

# Test each service
curl http://{auth-alb-url}/health
curl http://{user-alb-url}/health
curl http://{content-alb-url}/health
curl http://{notification-alb-url}/health
curl http://{gateway-alb-url}/health
```

- [ ] All health endpoints return 200 OK
- [ ] Services show "Healthy" in ECS console
- [ ] RDS database is accessible
- [ ] Redis cache is connected
- [ ] CloudWatch logs are flowing

---

## 💰 Cost Optimization Checklist

### Daily (Dev Environment)
- [ ] End of day: Run "Nuke AWS" workflow
- [ ] Morning: Run "Full Deploy" workflow
- [ ] Track actual costs in AWS Cost Explorer

### Weekly
- [ ] Review AWS bill
- [ ] Check for unused resources
- [ ] Verify dev environment nuked over weekend
- [ ] Clean old ECR images (> 30 days)

### Monthly
- [ ] Review Cost Explorer
- [ ] Compare actual vs estimated costs
- [ ] Optimize instance types if needed
- [ ] Review and adjust budget alerts

---

## 🔒 Security Checklist

### Initial Setup
- [ ] GitHub Secrets are properly configured
- [ ] No secrets in code or commit history
- [ ] IAM Role has minimum required permissions
- [ ] VPC security groups configured
- [ ] RDS encryption enabled
- [ ] ECR image scanning enabled

### Ongoing
- [ ] Rotate secrets every 90 days
- [ ] Review IAM policies quarterly
- [ ] Update security groups as needed
- [ ] Monitor CloudWatch for security events
- [ ] Keep Terraform and Docker images updated

---

## 🧪 Testing Checklist

### Before Each Deployment
- [ ] All local tests pass
- [ ] Docker Compose works locally
- [ ] No linter errors
- [ ] Code reviewed and approved
- [ ] Database migrations tested

### After Each Deployment
- [ ] Health endpoints return 200
- [ ] Database connections work
- [ ] RabbitMQ is accessible
- [ ] Redis cache is working
- [ ] API endpoints respond correctly
- [ ] No errors in CloudWatch logs

### Production Deployment
- [ ] Tested in dev environment first
- [ ] Load testing completed
- [ ] Security scan passed
- [ ] Backup created
- [ ] Rollback plan documented

---

## 📊 Monitoring Checklist

### Initial Setup
- [ ] CloudWatch log groups created
- [ ] CloudWatch alarms configured:
  - [ ] High CPU usage (> 80%)
  - [ ] High memory usage (> 80%)
  - [ ] Failed health checks
  - [ ] High error rate (> 5%)
- [ ] AWS Budget alerts set ($100/month dev, $200/month prod)
- [ ] SNS topic for alerts created

### Daily Monitoring
- [ ] Check CloudWatch dashboard
- [ ] Review error logs
- [ ] Verify all services healthy
- [ ] Check RDS connections

### Weekly Monitoring
- [ ] Review CloudWatch metrics
- [ ] Analyze error trends
- [ ] Check for performance issues
- [ ] Review costs vs budget

---

## 🔄 Nuke AWS Checklist

### Before Nuking
- [ ] Verify environment (dev or prod)
- [ ] Confirm no critical work in progress
- [ ] Check if anyone else is using the environment
- [ ] Backup important data if needed

### During Nuke
- [ ] Run "Nuke AWS" workflow
- [ ] Type "NUKE" exactly in confirmation
- [ ] Ensure keep_ecr_images is checked
- [ ] Ensure keep_rds is checked
- [ ] Monitor workflow progress

### After Nuke
- [ ] Verify ECS clusters deleted
- [ ] Verify ALBs deleted
- [ ] Verify CloudWatch logs deleted
- [ ] Verify RDS still exists ✅
- [ ] Verify ECR repositories still exist ✅
- [ ] Check AWS bill reduced

---

## 🚨 Emergency Checklist

### Service Down in Production
1. [ ] Check ECS console for task status
2. [ ] Review CloudWatch logs for errors
3. [ ] Check RDS connections
4. [ ] Check RabbitMQ status
5. [ ] Check Redis status
6. [ ] Restart service if needed:
   ```bash
   aws ecs update-service \
     --cluster healink-prod \
     --service {service-name} \
     --force-new-deployment
   ```
7. [ ] Monitor recovery
8. [ ] Document incident

### Deployment Failed
1. [ ] Check GitHub Actions logs
2. [ ] Identify which step failed:
   - [ ] Build phase
   - [ ] Stateful infrastructure
   - [ ] Application infrastructure
   - [ ] Health checks
3. [ ] Fix the issue
4. [ ] Re-run deployment
5. [ ] Verify successful deployment

### Cannot Delete Resources
1. [ ] Check for dependencies
2. [ ] Try manual deletion in AWS Console
3. [ ] Use AWS CLI force delete:
   ```bash
   # ECS Service
   aws ecs delete-service --cluster {cluster} --service {service} --force
   
   # ALB
   aws elbv2 delete-load-balancer --load-balancer-arn {arn}
   
   # Target Group
   aws elbv2 delete-target-group --target-group-arn {arn}
   ```
4. [ ] Wait and retry
5. [ ] Contact AWS support if stuck

### Accidentally Deleted Database
1. [ ] Don't panic - RDS has automated backups
2. [ ] Go to RDS Console → Automated backups
3. [ ] Select most recent backup
4. [ ] Restore to new instance
5. [ ] Update connection strings
6. [ ] Test database access
7. [ ] Update security groups if needed

---

## 📚 Documentation Checklist

### Team Documentation
- [ ] README.md reviewed and understood
- [ ] QUICK_START.md scenarios practiced
- [ ] ARCHITECTURE.md diagrams reviewed
- [ ] Team trained on workflows
- [ ] Runbooks created for common issues

### Incident Documentation
- [ ] Incident response plan documented
- [ ] Rollback procedures documented
- [ ] Contact list for escalation
- [ ] Post-mortem template created

---

## 🎓 Training Checklist

### New Team Member Onboarding
- [ ] Access to GitHub repository
- [ ] Access to AWS Console (read-only)
- [ ] Read all documentation (4 hours)
- [ ] Shadow deployment (1 deployment)
- [ ] Perform supervised deployment (1 deployment)
- [ ] Perform independent deployment (1 deployment)
- [ ] Practice nuke workflow in dev
- [ ] Troubleshoot simulated failure

### Quarterly Training
- [ ] Review any changes to workflows
- [ ] Practice emergency procedures
- [ ] Review cost optimization strategies
- [ ] Update knowledge base

---

## 🔧 Maintenance Checklist

### Weekly
- [ ] Review CloudWatch logs for errors
- [ ] Check service health
- [ ] Verify backups are running
- [ ] Clean old Docker images (> 7 days in dev)

### Monthly
- [ ] Update Terraform modules
- [ ] Update Docker base images
- [ ] Review and update documentation
- [ ] Review security groups
- [ ] Patch any security vulnerabilities

### Quarterly
- [ ] Disaster recovery test
- [ ] Security audit
- [ ] Performance review
- [ ] Cost optimization review
- [ ] Update dependencies

### Annually
- [ ] Full system review
- [ ] Architecture review
- [ ] Security assessment
- [ ] Capacity planning
- [ ] Budget review

---

## ✅ Production Readiness Checklist

### Infrastructure
- [ ] All services deployed successfully
- [ ] Health checks passing
- [ ] Monitoring configured
- [ ] Alerts configured
- [ ] Backups configured
- [ ] Disaster recovery plan documented

### Security
- [ ] SSL/TLS certificates configured
- [ ] Security groups properly configured
- [ ] Secrets rotated
- [ ] Security scanning enabled
- [ ] Audit logging enabled
- [ ] Compliance requirements met

### Performance
- [ ] Load testing completed
- [ ] Performance benchmarks met
- [ ] Auto-scaling configured
- [ ] CDN configured (if needed)
- [ ] Database optimized

### Operations
- [ ] Runbooks created
- [ ] Team trained
- [ ] On-call rotation established
- [ ] Incident response plan documented
- [ ] Communication plan established

---

## 📊 Success Metrics

### Deployment Success
- [ ] Deployment success rate > 95%
- [ ] Average deployment time < 30 minutes
- [ ] Zero failed deployments in last week
- [ ] All services healthy 99% of time

### Cost Efficiency
- [ ] Dev costs < $100/month
- [ ] Prod costs < $200/month
- [ ] Actual costs within 10% of estimate
- [ ] Nuke strategy saving > $50/month

### Performance
- [ ] All endpoints respond < 500ms
- [ ] Error rate < 1%
- [ ] Uptime > 99.5%
- [ ] No critical incidents in last 30 days

---

## 📞 Support Contacts

### Internal
- DevOps Lead: [Contact Info]
- Team Lead: [Contact Info]
- On-Call: [Rotation Schedule]

### External
- AWS Support: [Case Portal]
- GitHub Support: [Support Portal]
- Emergency: [Emergency Contacts]

---

## 🎯 Final Pre-Launch Checklist

### T-1 Week
- [ ] All checklists completed
- [ ] Team trained
- [ ] Documentation reviewed
- [ ] Monitoring configured
- [ ] Backups tested

### T-1 Day
- [ ] Deploy to production
- [ ] Verify all services
- [ ] Test critical paths
- [ ] Verify monitoring
- [ ] Brief team

### Launch Day
- [ ] Monitor closely
- [ ] Be ready for issues
- [ ] Document any problems
- [ ] Communicate status

### Post-Launch (T+1 Day)
- [ ] Review metrics
- [ ] Check for errors
- [ ] Gather feedback
- [ ] Document lessons learned
- [ ] Celebrate success! 🎉

---

**Last Updated**: September 30, 2025  
**Version**: 2.0  
**Status**: Production Ready ✅
