using MassTransit;

namespace SubscriptionService.Infrastructure.Saga;

public class RegisterSubscriptionSagaState : SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; }
}