namespace Models.Shared.Response;

public class ErrorHandlerModel
{
    public ErrorHandlerModel(bool success, int statusCode, string message)
    {
        this.Message = message;
        this.Success = success;
        this.StatusCode = statusCode;
    }

    public bool Success { get; }

    public int StatusCode { get; }

    public string Message { get; }
}