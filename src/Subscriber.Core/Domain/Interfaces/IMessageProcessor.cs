using Subscriber.Core.Domain.Entities;

namespace Subscriber.Core.Domain.Interfaces;

public interface IMessageProcessor
{
	Task ProcessAsync(Message message);
}
