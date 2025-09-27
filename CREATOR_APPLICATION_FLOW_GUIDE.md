# Creator Application Flow Guide

This document explains the Content Creator Application workflow implementation.

## Implemented Features

1. **Creator Application Submission**
   - Users can submit applications to become Content Creators
   - Application includes experience, portfolio links, social media profiles, and motivation

2. **Admin Review Process**
   - Admins can view pending applications
   - Admins can approve or reject applications with notes
   - Approval automatically grants ContentCreator role

3. **Event-Driven Integration**
   - Events published when applications are submitted, approved, or rejected
   - ContentService listens for approval events to grant content creation permissions

## API Endpoints

### User Service

- `POST /api/creatorapplications` - Submit a new application
- `GET /api/creatorapplications/pending` - List pending applications (Admin only)
- `GET /api/creatorapplications/{id}` - Get application details (Admin only)
- `POST /api/creatorapplications/{id}/approve` - Approve application (Admin only)
- `POST /api/creatorapplications/{id}/reject` - Reject application (Admin only)

## Testing the Flow

### 1. Submit an Application (User role)

```bash
# Make sure you have a JWT token for a regular user
curl -X POST http://localhost:5010/api/creatorapplications \
  -H "Authorization: Bearer YOUR_USER_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "experience": "5 years creating health podcasts",
    "portfolio": "https://example.com/my-portfolio",
    "motivation": "I want to create content that helps others with mental health",
    "social_media": {
      "instagram": "@healthcreator",
      "youtube": "HealthCreator"
    },
    "additional_info": "I specialize in anxiety and meditation content"
  }'
```

### 2. List Pending Applications (Admin role)

```bash
# Make sure you have a JWT token for an admin user
curl -X GET http://localhost:5010/api/creatorapplications/pending \
  -H "Authorization: Bearer YOUR_ADMIN_JWT_TOKEN"
```

### 3. Get Application Details (Admin role)

```bash
# Replace APPLICATION_ID with the actual ID from step 2
curl -X GET http://localhost:5010/api/creatorapplications/APPLICATION_ID \
  -H "Authorization: Bearer YOUR_ADMIN_JWT_TOKEN"
```

### 4. Approve Application (Admin role)

```bash
# Replace APPLICATION_ID with the actual ID
curl -X POST http://localhost:5010/api/creatorapplications/APPLICATION_ID/approve \
  -H "Authorization: Bearer YOUR_ADMIN_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "application_id": "APPLICATION_ID",
    "notes": "Great portfolio and experience. Approved!"
  }'
```

### 5. Test Content Creation (After approval)

The user should now be able to create content using their JWT token which will have the ContentCreator role.

```bash
curl -X POST http://localhost:5010/api/content/podcasts \
  -H "Authorization: Bearer USER_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "title": "My First Podcast",
    "description": "A test podcast",
    "audioUrl": "https://example.com/podcast.mp3",
    "duration": "00:30:00"
  }'
```

## Event Flow Verification

1. Check RabbitMQ management UI (http://localhost:15672) - guest/guest
2. Verify these events are published:
   - `CreatorApplicationSubmittedEvent`
   - `CreatorApplicationApprovedEvent`
   - `RoleAddedToUserEvent`

3. Check ContentService logs to verify the consumer processed the events
4. Verify the user can now create content in ContentService

## Troubleshooting

If you encounter issues:

1. Check logs in each service container
2. Verify JWT token contains correct claims (user_id, roles)
3. Ensure all services are running and MassTransit is connecting to RabbitMQ
4. Check database for application status updates
