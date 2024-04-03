namespace Users.Contracts.Features.ForgetPassword;

public class UpdateForgetPasswordRequestDTO
{
    public string? NewPassword { get; set; }

    public Guid? ResetPasswordToken { get; set; }
}

public class UpdateForgetPasswordResponseDTO
{
    public DateTime GenerateDateTime { get; set; }
}