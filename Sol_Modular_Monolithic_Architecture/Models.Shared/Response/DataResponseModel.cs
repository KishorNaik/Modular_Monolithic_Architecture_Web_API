namespace Models.Shared.Response;

public class DataResponse<TData>
{
    public bool? Success { get; set; }

    public int? StatusCode { get; set; }

    public TData? Data { get; set; }

    public string? Message { get; set; }
}

public static class DataResponse
{
    public static DataResponse<TData> Response<TData>(bool? success, int? statusCode, TData? data, string? message)
    {
        return new DataResponse<TData> { Success = success, StatusCode = statusCode, Data = data, Message = message };
    }
}