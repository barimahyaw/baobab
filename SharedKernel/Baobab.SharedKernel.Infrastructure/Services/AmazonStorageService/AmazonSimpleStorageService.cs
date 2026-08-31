using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Transfer;
using Baobab.SharedKernel.Application.Abstractions.Services;
using Baobab.SharedKernel.Domain.Requests;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Baobab.SharedKernel.Infrastructure.Services.AmazonStorageService;

public class AmazonSimpleStorageService() : IAmazonSimpleStorageService
{
    private string? AMAZON_S3_SETTINGS_BUCKET_NAME = Environment.GetEnvironmentVariable("AMAZON_S3_SETTINGS_BUCKET_NAME");
    private static AmazonS3Client S3Client()
    {
        var accessKeyID = Environment.GetEnvironmentVariable("AMAZON_S3_SETTINGS_ACCESS_KEY_ID") ?? throw new ArgumentNullException("AMAZON_S3_SETTINGS_ACCESS_KEY_ID");
        var secretAccessKey = Environment.GetEnvironmentVariable("AMAZON_S3_SETTINGS_SECRET_ACCESS_KEY") ?? throw new ArgumentNullException("AMAZON_S3_SETTINGS_SECRET_ACCESS_KEY");
        var credentials = new BasicAWSCredentials(accessKeyID, secretAccessKey);
        var config = new AmazonS3Config { RegionEndpoint = RegionEndpoint.EUWest2 };
        var client = new AmazonS3Client(credentials, config);
        return client;
    }

    public async Task<(bool, string)> Upload(IFormFile file)
    {
        try
        {
            //convert file to stream
            using var memoryStr = new MemoryStream();
            await file.CopyToAsync(memoryStr);
            //generate unique file name
            var fileExt = Path.GetExtension(file.FileName);
            var objName = $"{Guid.CreateVersion7()}{fileExt}";

            //create the upload request
            var uploadRequest = new TransferUtilityUploadRequest
            {
                InputStream = memoryStr,
                Key = objName,
                BucketName = AMAZON_S3_SETTINGS_BUCKET_NAME ?? throw new ArgumentNullException("AMAZON_S3_SETTINGS_BUCKET_NAME"),
                CannedACL = S3CannedACL.NoACL
            };
            //created an S3 client
            using var transferUtility = new TransferUtility(S3Client());
            await transferUtility.UploadAsync(uploadRequest);
            return (true, objName);
        }
        catch (AmazonS3Exception ex)
        {
            if (ex.ErrorCode != null && (ex.ErrorCode.Equals("InvalidAccessKeyId")
                || ex.ErrorCode.Equals("InvalidSecurity")))
                return (false, "Check the AWS Credentials");
            else return (false, "Error occured: " + ex.Message);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<(bool, string)> Upload(byte[] fileBytes, string fileName)
    {
        try
        {
            var memoryStr = new MemoryStream(fileBytes);
            //create the upload request
            var uploadRequest = new TransferUtilityUploadRequest
            {
                InputStream = memoryStr,
                Key = fileName,
                BucketName = AMAZON_S3_SETTINGS_BUCKET_NAME ?? throw new ArgumentNullException("AMAZON_S3_SETTINGS_BUCKET_NAME"),
                CannedACL = S3CannedACL.NoACL
            };
            // created an S3 client
            using var transferUtility = new TransferUtility(S3Client());
            await transferUtility.UploadAsync(uploadRequest);
            return (true, fileName);
        }
        catch (AmazonS3Exception ex)
        {
            if (ex.ErrorCode != null && (ex.ErrorCode.Equals("InvalidAccessKeyId")
                || ex.ErrorCode.Equals("InvalidSecurity")))
                return (false, "Check the AWS Credentials");
            else return (false, "Error occured: " + ex.Message);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<(bool, string)> ListUpload(List<IFormFile> file)
    {
        var result = (false, string.Empty);

        foreach (var fileItem in file)
        {
            result = await Upload(fileItem);
        }
        return result;
    }

    public async Task<(bool, string)> ListUpload(List<UploadRequest> req)
    {
        var result = (false, string.Empty);

        foreach (var reqItem in req)
        {
            result = await Upload(reqItem.FileBytes, reqItem.FileName);
        }
        return result;
    }

    public async Task<byte[]> Download(string fileName, string contentType, string? bucketName = null)
    {
        if (string.IsNullOrWhiteSpace(fileName) || string.IsNullOrWhiteSpace(contentType))
            return Array.Empty<byte>();
        try
        {
            MemoryStream ms = new();
            bucketName ??= AMAZON_S3_SETTINGS_BUCKET_NAME ?? throw new ArgumentNullException("AMAZON_S3_SETTINGS_BUCKET_NAME");
            using (var getObjectResponse = await S3Client().GetObjectAsync(bucketName, fileName))
            {
                getObjectResponse.ResponseStream.CopyTo(ms);
            }
            var download = new FileContentResult(ms.ToArray(), contentType);
            return download.FileContents;
        }
        catch (AmazonS3Exception ex)
        {
            if (ex.ErrorCode != null && (ex.ErrorCode.Equals("InvalidAccessKeyId")
                || ex.ErrorCode.Equals("InvalidSecurity")))
                Console.WriteLine(ex);
            else
                Console.WriteLine(ex.Message);
            return Array.Empty<byte>();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return Array.Empty<byte>();
        }
    }

}
