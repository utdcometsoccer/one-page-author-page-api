namespace InkStainedWretch.OnePageAuthorAPI.API.ImageServices.Models;

/// <summary>
/// Result of an image upload operation.
/// </summary>
public class ImageUploadResult : ServiceResult
{
    /// <summary>
    /// The uploaded image metadata (only populated on success).
    /// </summary>
    public UploadImageResponse? ImageData { get; set; }
}

/// <summary>
/// Result of retrieving user images.
/// </summary>
public class UserImagesResult : ServiceResult
{
    /// <summary>
    /// List of user images (only populated on success).
    /// </summary>
    public List<UserImageResponse>? Images { get; set; }
}

/// <summary>
/// Result of an image deletion operation.
/// </summary>
public class ImageDeleteResult : ServiceResult
{
    /// <summary>
    /// Success message (only populated on success).
    /// </summary>
    public string? Message { get; set; }
}

/// <summary>
/// Base class for service operation results.
/// </summary>
public abstract class ServiceResult
{
    /// <summary>
    /// Indicates if the operation was successful.
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// HTTP status code for the result.
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// Error message (only populated on failure).
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static T Success<T>(int statusCode = 200) where T : ServiceResult, new()
    {
        return new T { IsSuccess = true, StatusCode = statusCode };
    }

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static T Failure<T>(string errorMessage, int statusCode = 400) where T : ServiceResult, new()
    {
        return new T { IsSuccess = false, ErrorMessage = errorMessage, StatusCode = statusCode };
    }
}

/// <summary>
/// Response model for successful image upload.
/// </summary>
public class UploadImageResponse
{
    public string Id { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public long Size { get; set; }
}

/// <summary>
/// Response model for user image list item.
/// </summary>
public class UserImageResponse
{
    public string Id { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public long Size { get; set; }
    public DateTime UploadedAt { get; set; }
}