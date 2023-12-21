namespace Aspire.Demo.Architecture.Messaging;

using System.Threading.Tasks;

public interface IMessageSender
{
    Task SendAsync<T>(string queue, T message);
}
