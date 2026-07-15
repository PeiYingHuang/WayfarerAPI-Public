using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WayfarerAPI.Application.Interfaces.Utilities;

namespace WayfarerAPI.Infrastructure.Utilities;

public class GoogleCloudStorageClient : IGoogleCloudStorageClient
{
    private readonly StorageClient _storageClient;
    private readonly UrlSigner _urlSigner;
    private readonly ILogger<GoogleCloudStorageClient> _logger;
    private readonly string _bucketName;
    private readonly TimeSpan _signedUrlExpiresIn;

    public GoogleCloudStorageClient(ILogger<GoogleCloudStorageClient> logger, IConfiguration configuration)
    {
        _logger = logger;
        var gcpSection = configuration.GetSection("GcpStorage");
        _bucketName = gcpSection.GetValue<string>("BucketName") ?? throw new InvalidOperationException("GcpStorage:BucketName is missing");
        var keyFilePath = gcpSection.GetValue<string>("KeyFilePath") ?? throw new InvalidOperationException("GcpStorage:KeyFilePath is missing");
        var signedUrlExpiresMinutes = gcpSection.GetValue<int?>("SignedUrlExpiresMinutes") ?? 10;
        _signedUrlExpiresIn = TimeSpan.FromMinutes(Math.Max(1, signedUrlExpiresMinutes));

        if (!Path.IsPathRooted(keyFilePath))
        {
            keyFilePath = Path.GetFullPath(keyFilePath, AppContext.BaseDirectory);
        }

        var credential = CredentialFactory.FromFile<ServiceAccountCredential>(keyFilePath)
        .ToGoogleCredential();
        _storageClient = StorageClient.Create(credential);
        _urlSigner = UrlSigner.FromCredential(credential);
    }

    public string BucketName => _bucketName;

    public async Task<string> UploadFileAsync(string bucketName, string objectName, Stream fileStream, string contentType)
    {
        try
        {
            await _storageClient.UploadObjectAsync(bucketName, objectName, contentType, fileStream);
            _logger.LogInformation("檔案成功上傳到 GCS: {BucketName}/{ObjectName}", bucketName, objectName);
            return $"https://storage.googleapis.com/{bucketName}/{objectName}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "上傳檔案到 GCS 失敗: {BucketName}/{ObjectName}", bucketName, objectName);
            throw;
        }
    }

    public async Task DeleteFileAsync(string bucketName, string objectName)
    {
        try
        {
            await _storageClient.DeleteObjectAsync(bucketName, objectName);
            _logger.LogInformation("檔案成功刪除: {BucketName}/{ObjectName}", bucketName, objectName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "刪除 GCS 檔案失敗: {BucketName}/{ObjectName}", bucketName, objectName);
            throw;
        }
    }

    public async Task<List<string>> ListObjectNamesAsync(string bucketName, string prefix)
    {
        try
        {
            var names = new List<string>();
            await foreach (var obj in _storageClient.ListObjectsAsync(bucketName, prefix))
            {
                if (string.IsNullOrWhiteSpace(obj.Name) || obj.Name.EndsWith('/'))
                {
                    continue;
                }

                names.Add(obj.Name);
            }

            return names;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "列出 GCS 檔案失敗: {BucketName}/{Prefix}", bucketName, prefix);
            throw;
        }
    }

    public Task<string> GenerateSignedReadUrlAsync(string bucketName, string objectName)
    {
        var signedUrl = _urlSigner.Sign(bucketName, objectName, _signedUrlExpiresIn, HttpMethod.Get);
        return Task.FromResult(signedUrl);
    }
}
