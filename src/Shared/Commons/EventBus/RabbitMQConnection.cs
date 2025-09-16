using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProductAuthMicroservice.Commons.Configs;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System.Net.Sockets;

namespace ProductAuthMicroservice.Commons.EventBus;

public class RabbitMQConnection : IRabbitMQConnection
{
    private readonly ILogger<RabbitMQConnection> _logger;
    private readonly RabbitMQConfig _config;
    private readonly object _lockObject = new();
    private IConnection? _connection;
    private bool _disposed;

    public RabbitMQConnection(IOptions<RabbitMQConfig> config, ILogger<RabbitMQConnection> logger)
    {
        _config = config.Value;
        _logger = logger;
    }

    public bool IsConnected => _connection != null && _connection.IsOpen && !_disposed;

    public bool TryConnect()
    {
        _logger.LogInformation("RabbitMQ Client is trying to connect");

        lock (_lockObject)
        {
            var factory = new ConnectionFactory
            {
                HostName = _config.HostName,
                Port = _config.Port,
                UserName = _config.UserName,
                Password = _config.Password,
                VirtualHost = _config.VirtualHost
            };

            try
            {
                _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
                
                if (IsConnected)
                {
                    _connection.ConnectionShutdownAsync += OnConnectionShutdownAsync;
                    _connection.CallbackExceptionAsync += OnCallbackExceptionAsync;
                    _connection.ConnectionBlockedAsync += OnConnectionBlockedAsync;
                    
                    _logger.LogInformation("RabbitMQ Client acquired a persistent connection to '{HostName}' and is subscribed to failure events", _config.HostName);
                    return true;
                }

                _logger.LogCritical("FATAL ERROR: RabbitMQ connections could not be created and opened");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "FATAL ERROR: RabbitMQ connections could not be created and opened");
                return false;
            }
        }
    }

    public IConnection CreateConnection()
    {
        if (!IsConnected)
        {
            TryConnect();
        }

        return _connection ?? throw new InvalidOperationException("No RabbitMQ connections are available to perform this action");
    }

    public IChannel CreateChannel()
    {
        if (!IsConnected)
        {
            throw new InvalidOperationException("No RabbitMQ connections are available to perform this action");
        }

        return _connection!.CreateChannelAsync().GetAwaiter().GetResult();
    }

    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;

        try
        {
            _connection?.Dispose();
        }
        catch (IOException ex)
        {
            _logger.LogCritical(ex, "Error disposing RabbitMQ connection");
        }
    }

    private Task OnConnectionBlockedAsync(object sender, ConnectionBlockedEventArgs e)
    {
        if (_disposed) return Task.CompletedTask;

        _logger.LogWarning("A RabbitMQ connection is blocked. Trying to re-connect...");
        TryConnect();
        return Task.CompletedTask;
    }

    private Task OnCallbackExceptionAsync(object sender, CallbackExceptionEventArgs e)
    {
        if (_disposed) return Task.CompletedTask;

        _logger.LogWarning("A RabbitMQ connection throw exception. Trying to re-connect...");
        TryConnect();
        return Task.CompletedTask;
    }

    private Task OnConnectionShutdownAsync(object sender, ShutdownEventArgs reason)
    {
        if (_disposed) return Task.CompletedTask;

        _logger.LogWarning("A RabbitMQ connection is on shutdown. Trying to re-connect...");
        TryConnect();
        return Task.CompletedTask;
    }
}