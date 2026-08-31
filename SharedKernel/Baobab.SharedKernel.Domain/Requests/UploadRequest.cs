namespace Baobab.SharedKernel.Domain.Requests;

public record UploadRequest(byte[] FileBytes, string FileName);
