using MediatR;
using Microsoft.Extensions.Logging;
using SharedLibrary.Commons.Models;
using SharedLibrary.Commons.Outbox;
using SharedLibrary.Commons.Services;

namespace SubscriptionService.Application.Features.Subscriptions.Commands.RegisterSubscription;

public class RegisterSubscriptionCommandHandler : IRequestHandler<RegisterSubscriptionCommand, Result>
{
    private readonly IOutboxUnitOfWork _outboxUnitOfWork;
    private readonly ILogger<RegisterSubscriptionCommandHandler> _logger;
    private readonly ICurrentUserService _currentUserService;
    
    public Task<Result> Handle(RegisterSubscriptionCommand request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
