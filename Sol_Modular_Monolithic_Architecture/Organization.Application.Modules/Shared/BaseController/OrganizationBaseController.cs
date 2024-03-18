namespace Organization.Application.Modules.Shared.BaseController;

[ApiController]
[Produces("application/json")]
[Consumes("application/json")]
public abstract class OrganizationBaseController : ControllerBase
{
    private readonly IMediator mediator;

    public OrganizationBaseController(IMediator mediator)
    {
        this.mediator = mediator;
    }

    protected IMediator Mediator => mediator;
}