# Healink Business Context Diagram

## Business Context Diagram

```mermaid
graph TB
    %% External Actors
    Guest[Guest User<br/>- Browse podcasts<br/>- View content<br/>- Sign up/Login]
    Reader[Reader/User<br/>- Listen to podcasts<br/>- Subscribe to plans<br/>- Manage profile]
    Creator[Content Creator<br/>- Upload podcasts<br/>- Manage content<br/>- View analytics]
    Admin[Administrator<br/>- Manage users<br/>- Approve content<br/>- Monitor system]
    Moderator[Community Moderator<br/>- Moderate content<br/>- Review reports<br/>- Manage community]

    %% Core System
    HealinkSystem[Healink Platform<br/>Podcast Learning Ecosystem]

    %% External Systems
    MoMo[MoMo Payment Gateway<br/>- Payment processing<br/>- Transaction verification<br/>- IPN callbacks]
    S3[Amazon S3<br/>- File storage<br/>- Podcast audio files<br/>- Thumbnail images]
    Firebase[Firebase<br/>- Push notifications<br/>- Real-time messaging<br/>- User engagement]
    AI[AI Recommendation Service<br/>- Content analysis<br/>- Personalized suggestions<br/>- Machine learning]
    SMTP[SMTP Server<br/>- Email delivery<br/>- OTP verification<br/>- Notifications]
    RabbitMQ[RabbitMQ<br/>- Message broker<br/>- Event communication<br/>- Service coordination]

    %% Frontend Applications
    WebApp[Next.js Web Application<br/>- User interface<br/>- Podcast browsing<br/>- Content management]
    MobileApp[Mobile Application<br/>- iOS/Android<br/>- Podcast streaming<br/>- Offline listening]

    %% Backend Services
    Gateway[API Gateway<br/>- Request routing<br/>- Authentication<br/>- Load balancing]
    
    AuthService[Auth Service<br/>- User authentication<br/>- JWT tokens<br/>- Role management]
    UserService[User Service<br/>- Profile management<br/>- Creator applications<br/>- File uploads]
    ContentService[Content Service<br/>- Podcast management<br/>- Community features<br/>- Content approval]
    SubscriptionService[Subscription Service<br/>- Plan management<br/>- Billing<br/>- Usage tracking]
    PaymentService[Payment Service<br/>- Payment processing<br/>- Transaction management<br/>- Refund handling]
    NotificationService[Notification Service<br/>- Email/SMS<br/>- Push notifications<br/>- Alert management]
    RecommendationService[Recommendation Service<br/>- AI suggestions<br/>- Content analysis<br/>- Personalization]

    %% Database Systems
    AuthDB[(Auth Database<br/>- Users<br/>- Roles<br/>- Permissions)]
    UserDB[(User Database<br/>- Profiles<br/>- Applications<br/>- Activity logs)]
    ContentDB[(Content Database<br/>- Podcasts<br/>- Community<br/>- Interactions)]
    SubscriptionDB[(Subscription Database<br/>- Plans<br/>- Subscriptions<br/>- Billing)]
    PaymentDB[(Payment Database<br/>- Transactions<br/>- Payment methods<br/>- History)]
    NotificationDB[(Notification Database<br/>- Templates<br/>- Delivery logs<br/>- Settings)]

    %% User Interactions
    Guest --> WebApp
    Reader --> WebApp
    Creator --> WebApp
    Admin --> WebApp
    Moderator --> WebApp
    
    Guest --> MobileApp
    Reader --> MobileApp
    Creator --> MobileApp

    %% Frontend to Backend
    WebApp --> Gateway
    MobileApp --> Gateway

    %% Gateway to Services
    Gateway --> AuthService
    Gateway --> UserService
    Gateway --> ContentService
    Gateway --> SubscriptionService
    Gateway --> PaymentService
    Gateway --> NotificationService
    Gateway --> RecommendationService

    %% Service to Database
    AuthService --> AuthDB
    UserService --> UserDB
    ContentService --> ContentDB
    SubscriptionService --> SubscriptionDB
    PaymentService --> PaymentDB
    NotificationService --> NotificationDB
    RecommendationService --> ContentDB

    %% External System Integrations
    PaymentService --> MoMo
    UserService --> S3
    ContentService --> S3
    NotificationService --> Firebase
    NotificationService --> SMTP
    RecommendationService --> AI
    AuthService --> RabbitMQ
    UserService --> RabbitMQ
    ContentService --> RabbitMQ
    SubscriptionService --> RabbitMQ
    PaymentService --> RabbitMQ
    NotificationService --> RabbitMQ
    RecommendationService --> RabbitMQ

    %% Inter-service Communication
    AuthService -.-> UserService
    UserService -.-> ContentService
    ContentService -.-> SubscriptionService
    SubscriptionService -.-> PaymentService
    PaymentService -.-> NotificationService
    ContentService -.-> RecommendationService

    %% Styling
    classDef userClass fill:#e1f5fe,stroke:#01579b,stroke-width:2px
    classDef systemClass fill:#f3e5f5,stroke:#4a148c,stroke-width:2px
    classDef serviceClass fill:#e8f5e8,stroke:#1b5e20,stroke-width:2px
    classDef dbClass fill:#fff3e0,stroke:#e65100,stroke-width:2px
    classDef externalClass fill:#fce4ec,stroke:#880e4f,stroke-width:2px

    class Guest,Reader,Creator,Admin,Moderator userClass
    class HealinkSystem,WebApp,MobileApp,Gateway systemClass
    class AuthService,UserService,ContentService,SubscriptionService,PaymentService,NotificationService,RecommendationService serviceClass
    class AuthDB,UserDB,ContentDB,SubscriptionDB,PaymentDB,NotificationDB dbClass
    class MoMo,S3,Firebase,AI,SMTP,RabbitMQ externalClass
```

## Business Context Description

### **Core Business Domain**
Healink is a **Podcast Learning Ecosystem** that connects content creators with learners through an AI-powered platform for educational podcast content.

### **Primary Actors**
1. **Guest Users**: Browse content, discover podcasts, sign up for accounts
2. **Readers/Users**: Subscribe to podcasts, manage learning progress, interact with community
3. **Content Creators**: Upload and manage podcast content, track analytics, monetize content
4. **Administrators**: Manage platform operations, approve content, monitor system health
5. **Community Moderators**: Moderate user-generated content, handle reports, maintain community standards

### **Key Business Processes**
1. **Content Creation & Management**: Creators upload podcasts, manage metadata, track performance
2. **User Onboarding**: Registration, email verification, profile setup, subscription selection
3. **Content Discovery**: AI-powered recommendations, trending content, personalized suggestions
4. **Subscription Management**: Plan selection, payment processing, access control, billing
5. **Community Engagement**: User interactions, content sharing, social features
6. **Content Moderation**: Review process, approval workflows, quality control

### **External Dependencies**
- **MoMo Payment Gateway**: Vietnamese payment processing
- **Amazon S3**: Scalable file storage for audio content
- **Firebase**: Real-time notifications and messaging
- **AI Service**: Machine learning for content recommendations
- **SMTP Server**: Email delivery for notifications and verification
- **RabbitMQ**: Event-driven communication between services

### **Value Propositions**
- **For Learners**: Access to quality educational content, personalized learning paths, community engagement
- **For Creators**: Easy content upload, analytics insights, monetization opportunities
- **For Platform**: Scalable architecture, reliable payment processing, AI-driven user engagement

### **Business Rules**
- Content creators must be approved before publishing
- Users need active subscriptions for premium content access
- All payments processed through secure MoMo gateway
- AI recommendations based on user behavior and content analysis
- Community content moderated by designated moderators
- Email verification required for account activation

