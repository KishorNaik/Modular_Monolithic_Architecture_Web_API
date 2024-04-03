namespace Organization.Contracts.Features;

public class AddOrganizationRequestDTO
{
    public string? Name { get; set; }
}

public class AddOrganizationResponseDTO
{
    public Guid? Identifier { get; set; }
}