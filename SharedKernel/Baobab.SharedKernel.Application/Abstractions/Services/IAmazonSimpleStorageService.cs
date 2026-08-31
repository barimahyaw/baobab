using Baobab.SharedKernel.Domain.Requests;
using Microsoft.AspNetCore.Http;

namespace Baobab.SharedKernel.Application.Abstractions.Services;

public interface IAmazonSimpleStorageService
{
    Task<(bool, string)> Upload(IFormFile file);
    Task<(bool, string)> Upload(byte[] fileBytes, string fileName);
    Task<byte[]> Download(string fileName, string contentType, string? bucketName = null);
    Task<(bool, string)> ListUpload(List<IFormFile> file);
    Task<(bool, string)> ListUpload(List<UploadRequest> req);
}
