namespace LearningMap.Domain.Common;

/// <summary>
/// Same response envelope SafeSpace uses: every endpoint returns
/// { responseStatus, errorMessage, statusCode, data }.
/// </summary>
public class ApiResponseData
{
    public bool ResponseStatus { get; set; } = true;
    public string ErrorMessage { get; set; } = string.Empty;
    public int StatusCode { get; set; } = 200;
    public object? Data { get; set; }

    public static ApiResponseData Success(object? data, int statusCode = 200) =>
        new() { ResponseStatus = true, StatusCode = statusCode, Data = data };

    public static ApiResponseData Fail(int statusCode, string errorMessage, object? data = null) =>
        new() { ResponseStatus = false, StatusCode = statusCode, ErrorMessage = errorMessage, Data = data };
}
