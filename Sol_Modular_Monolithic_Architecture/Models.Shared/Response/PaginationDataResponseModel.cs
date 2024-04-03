namespace Models.Shared.Response;

public class PaginationDataResponseModel
{
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }

    public bool HasPrevious { get; set; }
    public bool HasNext { get; set; }
}

public class PaginationDataResponse<TData>
{
    public bool? Success { get; set; }

    public int? StatusCode { get; set; }

    public TData? Data { get; set; }

    public string? Message { get; set; }

    public PaginationDataResponseModel? Pagination { get; set; }
}

public static class PaginationDataResponse
{
    public static PaginationDataResponse<TData> Response<TData>(bool? success, int? statusCode, TData? data, string? message, PaginationDataResponseModel? pagination)
    {
        return new PaginationDataResponse<TData> { Success = success, StatusCode = statusCode, Data = data, Message = message, Pagination = pagination };
    }
}