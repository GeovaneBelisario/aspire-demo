namespace Aspire.Demo.Architecture.Messaging;

using System;
using System.Threading.Tasks;

public interface IMessageReceiver
{
    Task ReceiveAsync<T>(string queue, Action<T> onMessage);
}
