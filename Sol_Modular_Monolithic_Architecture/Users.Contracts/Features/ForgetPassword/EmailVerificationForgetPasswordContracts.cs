using Microsoft.AspNetCore.Mvc;

namespace Users.Contracts.Features.ForgetPassword;

public class EmailVerificationForgetPasswordApiRequestDTO
{
    [FromRoute]
    [JsonIgnore]
    public Guid? Token { get; set; }

    [FromBody]
    public EmailVerificationForgetPasswordRequestDTO Body { get; set; }
}

public class EmailVerificationForgetPasswordRequestDTO
{
    public string EmailId { get; set; }
}

public class EmailVerificationForgetPasswordResponseDTO
{
    public DateTime GenerateDateTime { get; set; }
}

public class EmailVerificationForgetPasswordCommand : EmailVerificationForgetPasswordApiRequestDTO, IRequest<DataResponse<EmailVerificationForgetPasswordResponseDTO>>
{
}