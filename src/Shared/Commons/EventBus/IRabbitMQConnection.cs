using RabbitMQ.Client;

namespace ProductAuthMicroservice.Commons.EventBus;

public interface IRabbitMQConnection : IDisposable
{
     bool IsConnected { get; }
    bool TryConnect();
    IConnection CreateConnection();
    IChannel CreateChannel();
}
