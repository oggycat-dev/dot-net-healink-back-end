using AutoMapper;
using SharedLibrary.Contracts.Subscription;
using SharedLibrary.Contracts.Subscription.Events;
using SubscriptionService.Application.Commons.DTOs;
using SubscriptionService.Domain.Entities;
using SubscriptionService.Domain.Enums;

namespace SubscriptionService.Application.Commons.Mappings;

public class SubscriptionMappingProfile : Profile
{
    public SubscriptionMappingProfile()
    {
        // Subscription mappings
        CreateMap<Subscription, SubscriptionResponse>()
            .ForMember(dest => dest.PlanName, opt => opt.MapFrom(src => src.Plan.Name))
            .ForMember(dest => dest.PlanDisplayName, opt => opt.MapFrom(src => src.Plan.DisplayName))
            .ForMember(dest => dest.SubscriptionStatusName, opt => opt.MapFrom(src => src.SubscriptionStatus.ToString()))
            .ForMember(dest => dest.RenewalBehaviorName, opt => opt.MapFrom(src => src.RenewalBehavior.ToString()))
            .ForMember(dest => dest.Amount, opt => opt.MapFrom(src => src.Plan.Amount))
            .ForMember(dest => dest.Currency, opt => opt.MapFrom(src => src.Plan.Currency))
            .ForMember(dest => dest.BillingPeriodUnit, opt => opt.MapFrom(src => src.Plan.BillingPeriodUnit.ToString()));

        // SubscriptionPlan mappings
        CreateMap<SubscriptionPlan, SubscriptionPlanResponse>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.BillingPeriodUnitName, opt => opt.MapFrom(src => src.BillingPeriodUnit.ToString()));

        CreateMap<SubscriptionPlanRequest, SubscriptionPlan>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.Status, opt => opt.Ignore())
            .ForMember(dest => dest.FeatureConfig, opt => opt.MapFrom(src => src.FeatureConfig ?? "{}"))
            .ForMember(dest => dest.BillingPeriodUnit, opt => opt.MapFrom(src => src.BillingPeriodUnit));

        // SubscriptionPlan to Event mappings - tái sử dụng cho Create/Update events
        CreateMap<SubscriptionPlan, SubscriptionPlanCreatedEvent>()
            .ForMember(dest => dest.SubscriptionPlanId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.BillingPeriodUnit, opt => opt.MapFrom(src => src.BillingPeriodUnit.ToString()))
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore()); // Set manually in handler

        CreateMap<SubscriptionPlan, SubscriptionPlanUpdatedEvent>()
            .ForMember(dest => dest.SubscriptionPlanId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore()); // Set manually in handler

        CreateMap<SubscriptionPlan, SubscriptionPlanDeletedEvent>()
            .ForMember(dest => dest.SubscriptionPlanId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.DeletedBy, opt => opt.Ignore()) // Set manually in handler
            .ForMember(dest => dest.DeletedAt, opt => opt.MapFrom(src => DateTime.UtcNow));

        // Subscription to Event mappings
        CreateMap<Subscription, SubscriptionUpdatedEvent>()
            .ForMember(dest => dest.SubscriptionId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.PlanName, opt => opt.MapFrom(src => src.Plan.DisplayName))
            .ForMember(dest => dest.SubscriptionStatus, opt => opt.MapFrom(src => src.SubscriptionStatus.ToString()))
            .ForMember(dest => dest.RenewalBehavior, opt => opt.MapFrom(src => src.RenewalBehavior.ToString()))
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore()); // Set manually in handler

        CreateMap<Subscription, SubscriptionCanceledEvent>()
            .ForMember(dest => dest.SubscriptionId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.PlanName, opt => opt.MapFrom(src => src.Plan.DisplayName))
            .ForMember(dest => dest.CanceledBy, opt => opt.Ignore()) // Set manually in handler
            .ForMember(dest => dest.Reason, opt => opt.Ignore()); // Set manually in handler

        // Subscription to Activity Event for user activity logging
        CreateMap<Subscription, SubscriptionRegisteredActivityEvent>()
            .ForMember(dest => dest.SubscriptionId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.UserProfileId, opt => opt.MapFrom(src => src.UserProfileId))
            .ForMember(dest => dest.SubscriptionPlanId, opt => opt.MapFrom(src => src.SubscriptionPlanId))
            .ForMember(dest => dest.SubscriptionPlanName, opt => opt.MapFrom(src => src.Plan.Name))
            .ForMember(dest => dest.SubscriptionPlanDisplayName, opt => opt.MapFrom(src => src.Plan.DisplayName))
            .ForMember(dest => dest.Amount, opt => opt.MapFrom(src => src.Plan.Amount))
            .ForMember(dest => dest.Currency, opt => opt.MapFrom(src => src.Plan.Currency))
            .ForMember(dest => dest.ActivityType, opt => opt.MapFrom(src => "SubscriptionRegistered"))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => 
                $"User registered for subscription plan: {src.Plan.DisplayName}"))
            .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
            .ForMember(dest => dest.CorrelationId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.IpAddress, opt => opt.Ignore()) // Set manually in handler
            .ForMember(dest => dest.UserAgent, opt => opt.Ignore()); // Set manually in handler
    }
}
