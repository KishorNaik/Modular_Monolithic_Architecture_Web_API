namespace Organization.Contracts.Features;

public class GetOrganizationByIdentifierRequestDTO
{
    [JsonIgnore]
    public Guid? Identifier { get; set; }
}

public class GetOrganizationByIdentifierResponseDTO
{
    [JsonIgnore]
    public decimal Id { get; set; }

    public Guid Identifier { get; set; }

    public string Name { get; set; }

    public int OrgType { get; set; }
}

public class GetOrganizationByIdentifierIntegrationService : GetOrganizationByIdentifierRequestDTO, IRequest<DataResponse<GetOrganizationByIdentifierResponseDTO>>
{
}