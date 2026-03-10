namespace WebClient.Services;

public interface IUserApiService
{
    Task<ApiResult<UserProfileDto>> GetProfileAsync();
    Task<ApiResult<UserProfileDto>> UpdateProfileAsync(UpdateProfileRequest request);
    Task<ApiResult<string>> UploadProfileImageAsync(MultipartFormDataContent content);
    Task<ApiResult> DeleteProfileImageAsync();
}
