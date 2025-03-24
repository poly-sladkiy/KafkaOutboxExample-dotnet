namespace Subscriber.Core.Application.Interfaces;

public interface IMessageHandlingService
{
	Task StartAsync(CancellationToken cancellationToken);
	Task StopAsync();
	void Subscribe(string topic);
}