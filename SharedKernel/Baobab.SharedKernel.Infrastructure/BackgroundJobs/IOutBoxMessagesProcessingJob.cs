namespace Baobab.SharedKernel.Infrastructure.BackgroundJobs;

public interface IOutBoxMessagesProcessingJob
{
    Task Execute(CancellationToken cancellationToken);
}