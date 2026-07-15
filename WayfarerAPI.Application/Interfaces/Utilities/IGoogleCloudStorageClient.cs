namespace WayfarerAPI.Application.Interfaces.Utilities;

public interface IGoogleCloudStorageClient
{
    /// <summary>
    /// 上傳檔案到 Google Cloud Storage
    /// </summary>
    /// <param name="bucketName">Bucket 名稱</param>
    /// <param name="objectName">物件名稱 (路徑)</param>
    /// <param name="fileStream">檔案流</param>
    /// <param name="contentType">MIME 類型</param>
    /// <returns>上傳後的公開 URL</returns>
    Task<string> UploadFileAsync(string bucketName, string objectName, Stream fileStream, string contentType);

    /// <summary>
    /// 刪除 Google Cloud Storage 中的檔案
    /// </summary>
    /// <param name="bucketName">Bucket 名稱</param>
    /// <param name="objectName">物件名稱 (路徑)</param>
    Task DeleteFileAsync(string bucketName, string objectName);

    /// <summary>
    /// 依路徑前綴列出 Google Cloud Storage 檔案名稱
    /// </summary>
    Task<List<string>> ListObjectNamesAsync(string bucketName, string prefix);

    /// <summary>
    /// 產生可簽署的讀取 URL
    /// </summary>
    /// <param name="bucketName">Bucket 名稱</param>
    /// <param name="objectName">物件名稱 (路徑)</param>
    /// <returns>可簽署的 URL</returns>
    Task<string> GenerateSignedReadUrlAsync(string bucketName, string objectName);

    /// <summary>
    /// Bucket 名稱
    /// </summary>
    string BucketName { get; }
}
