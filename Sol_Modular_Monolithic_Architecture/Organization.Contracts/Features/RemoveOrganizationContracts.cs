namespace Organization.Contracts.Features;

public class RemoveOrganizationRequestDTO
{
    public Guid? Identifier { get; set; }
}

public class RemoveOrganizationResponseDTO
{
    public DateTime? UpdatedTime { get; set; }
}

public class RemoveOrganizationIntegrationService : RemoveOrganizationRequestDTO, IRequest<DataResponse<RemoveOrganizationResponseDTO>>
{
}

public class RemoveOrganizationRollBackIntegrationService : RemoveOrganizationRequestDTO, IRequest<DataResponse<RemoveOrganizationResponseDTO>>
{
}