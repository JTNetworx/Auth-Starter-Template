using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Application.Services;
using Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services;

/// <summary>
/// Stores profile images in an S3-compatible bucket (AWS S3 or Cloudflare R2).
/// Images are served via the configured PublicBaseUrl (CDN or public bucket URL).
/// </summary>
public sealed class S3ProfileImageStore : IProfileImageStore
{
    private readonly S3Settings _s3;

    public S3ProfileImageStore(IOptions<S3Settings> s3Settings)
    {
        _s3 = s3Settings.Value;
    }

    public async Task<string> SaveAsync(string userId, Stream imageStream, string contentType, CancellationToken ct = default)
    {
        using var client = CreateClient();

        var key = $"profile-images/{userId}";

        var request = new PutObjectRequest
        {
            BucketName = _s3.BucketName,
            Key = key,
            InputStream = imageStream,
            ContentType = contentType,
            DisablePayloadSigning = true // required for Cloudflare R2
        };

        await client.PutObjectAsync(request, ct);

        var baseUrl = _s3.PublicBaseUrl.TrimEnd('/');
        return $"{baseUrl}/{key}";
    }

    public Task<(byte[] Data, string ContentType)?> GetAsync(string userId, CancellationToken ct = default)
    {
        // S3 images are served directly from the CDN — no API passthrough needed.
        return Task.FromResult<(byte[] Data, string ContentType)?>(null);
    }

    public async Task DeleteAsync(string userId, CancellationToken ct = default)
    {
        using var client = CreateClient();

        var request = new DeleteObjectRequest
        {
            BucketName = _s3.BucketName,
            Key = $"profile-images/{userId}"
        };

        await client.DeleteObjectAsync(request, ct);
    }

    private AmazonS3Client CreateClient()
    {
        var credentials = new BasicAWSCredentials(_s3.AccessKey, _s3.SecretKey);

        var config = new AmazonS3Config
        {
            ForcePathStyle = true // required for Cloudflare R2 and most S3-compatible providers
        };

        if (!string.IsNullOrEmpty(_s3.ServiceUrl))
            config.ServiceURL = _s3.ServiceUrl;
        else
            config.RegionEndpoint = RegionEndpoint.GetBySystemName(_s3.Region);

        return new AmazonS3Client(credentials, config);
    }
}
