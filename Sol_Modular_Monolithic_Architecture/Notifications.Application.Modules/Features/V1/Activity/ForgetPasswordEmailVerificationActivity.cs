namespace Notifications.Application.Modules.Features.V1.Activity;

#region Event Service

public class ForgetPasswordEmailVerificationIntegrationEventHandler : INotificationHandler<ForgetPasswordEmailVerificationIntegrationEvent>
{
    private readonly ILogger<ForgetPasswordEmailVerificationIntegrationEventHandler> logger;

    public ForgetPasswordEmailVerificationIntegrationEventHandler(ILogger<ForgetPasswordEmailVerificationIntegrationEventHandler> logger)
    {
        this.logger = logger;
    }

    Task INotificationHandler<ForgetPasswordEmailVerificationIntegrationEvent>.Handle(ForgetPasswordEmailVerificationIntegrationEvent notification, CancellationToken cancellationToken)
    {
        this.logger.LogInformation(nameof(ForgetPasswordEmailVerificationIntegrationEventHandler), "Sent Forget Password Email Verification done.");

        return Task.CompletedTask;
    }
}

#endregion Event Service