using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using RabbitMQ.Client;
using System.Text;
using Utilities.Updating;
using System.Text.Json;
using Utilities.Messaging.Consumer.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Utilities.Worker;

namespace Utilities.Messaging.Consumer
{
    public class RabbitMQWorkerWithId : BackgroundService, IDisposable
    {
        private Worker? _worker;
        private readonly IUpdateController _updateController;
        private readonly RabbitMQWorkerOptions _options;
        private readonly IEncodedTaskHandler _taskHandler;
        public RabbitMQWorkerWithId(IOptions<RabbitMQWorkerOptions> options, IUpdateController updateController, IEncodedTaskHandler taskHandler)
        {
            _options = options.Value;
            _taskHandler = taskHandler;
            _updateController = updateController;
        }

        public override void Dispose()
        {
            _updateController.Stop();
            if (_worker != null ) 
            {
                _updateController.Remove(_worker);
                _worker.Dispose();
            }
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                _worker = new Worker((_options.BrokerAddress ?? throw new ArgumentException()).Hostname,
                _options.BrokerAddress.Port,
                _options.BrokerAddress.Uri,
                        _options.OutcomingQueueName ?? throw new ArgumentException(),
                        _options.IncomingQueueName ?? throw new ArgumentException(),
                        _options.ContinuationTimeout ?? throw new ArgumentException(),
                        _taskHandler);
                _updateController.Add(_worker);
                _updateController.Start();
            }
            catch (Exception)
            {
                _worker?.Dispose();
                throw new ArgumentException("Некорректные опции для RabbitMQWorkerWithId");
            }

            return Task.CompletedTask;
        }

        public class Worker : IUpdateable, IDisposable
        {
            private const int ConsumerDispatchConcurrency = 5;

            private IConnection? _connection;
            private IModel? _outcomingQueueChannel;
            private IModel? _incomingQueueChannel;
            private readonly string _outcomingQueueName;
            private readonly string _incomingQueueName;
            private bool _isAvailable = false;
            private bool _outcomingQueueEstablished = false;
            private bool _incomingQueueEstablished = false;
            private readonly TimeSpan _continuationTimeout;
            private AsyncEventingBasicConsumer? _consumer = null;
            private readonly IEncodedTaskHandler _taskHandler;

            private bool _isBlocked = false;
            private readonly ConnectionFactory _factory;
            private bool _isActive = false;

            public Worker(string? hostName, int? port, Uri? uri, string outcomingQueueName, string incomingQueueName, TimeSpan continuationTimeout, IEncodedTaskHandler taskHandler)
            {
                _factory = new ConnectionFactory();
                _factory.DispatchConsumersAsync = true;
                _factory.ConsumerDispatchConcurrency = ConsumerDispatchConcurrency;
                if (hostName != null)
                {
                    _factory.HostName = hostName;
                    if (port != null)
                    {
                        _factory.Port = (int)port;
                    }
                }
                else if (uri != null)
                {
                    _factory.Uri = uri;
                }
                else
                {
                    throw new ArgumentException("No host address for a queue");
                }

                _outcomingQueueName = outcomingQueueName;
                _incomingQueueName = incomingQueueName;
                _continuationTimeout = continuationTimeout;
                _taskHandler = taskHandler;
                _ = AttemptConnecting();
            }

            private void OnConnectionBlocked(object? sender, ConnectionBlockedEventArgs args)
            {
                _isBlocked = true;
            }

            private void OnConnectionUnblocked(object? sender, EventArgs args)
            {
                _isBlocked = false;
            }

            private void OnConnectionShutdown(object? sender, ShutdownEventArgs args)
            {
                _isActive = false;
                _connection?.Dispose();
                _outcomingQueueEstablished = false;
                _incomingQueueEstablished = false;
                _outcomingQueueChannel = null;
                _incomingQueueChannel = null;
            }

            public void Dispose()
            {
                if (_connection != null)
                {
                    _incomingQueueChannel?.Abort();
                    _incomingQueueChannel?.Dispose();
                    _outcomingQueueChannel?.Abort();
                    _outcomingQueueChannel?.Dispose();
                    _connection.Abort();
                    _connection.Dispose();
                }
                _isActive = false;
            }

            private void OnOutcomingQueueChannelShutdown(object? sender, ShutdownEventArgs? args)
            {
                _outcomingQueueEstablished = false;
                _outcomingQueueChannel = null;
            }

            private void OnIncomingQueueChannelShutdown(object? sender, ShutdownEventArgs? args)
            {
                _incomingQueueEstablished = false;
                _incomingQueueChannel = null;
            }

            public IConnection? GetConnection()
            {
                if (_isActive)
                {
                    return _connection;
                }

                try
                {
                    _connection = _factory.CreateConnection();
                    _connection.ConnectionShutdown += OnConnectionShutdown;
                    _isActive = true;
                    _isBlocked = false;
                    _connection.ConnectionBlocked += OnConnectionBlocked;
                    _connection.ConnectionUnblocked += OnConnectionUnblocked;
                    return _connection;
                }
                catch (BrokerUnreachableException)
                {
                    return null;
                }
            }

            private bool ConnectToBroker()
            {
                _connection = GetConnection();
                return _connection != null;
            }

            private bool ConnectToOutcomingQueue()
            {
                if (_connection == null)
                {
                    return false;
                }
                try
                {
                    _outcomingQueueChannel = _connection.CreateModel();
                    _outcomingQueueChannel.ModelShutdown += OnOutcomingQueueChannelShutdown;
                    _outcomingQueueChannel.QueueDeclare(queue: _outcomingQueueName, durable: false, exclusive: false, autoDelete: false, arguments: null);
                    _outcomingQueueEstablished = true;
                    _outcomingQueueChannel.ContinuationTimeout = _continuationTimeout;
                    _outcomingQueueChannel.ConfirmSelect();
                    return true;
                }
                catch (Exception)
                {
                    _outcomingQueueChannel?.Abort();
                    return false;
                }
            }

            private bool ConnectToIncomingQueue()
            {
                if (_connection == null)
                {
                    return false;
                }
                try
                {
                    _incomingQueueChannel = _connection.CreateModel();
                    _incomingQueueChannel.ModelShutdown += OnIncomingQueueChannelShutdown;
                    _incomingQueueChannel.QueueDeclare(queue: _incomingQueueName, durable: false, exclusive: false, autoDelete: false, arguments: null);
                    _incomingQueueEstablished = true;
                    _incomingQueueChannel.ContinuationTimeout = _continuationTimeout;
                    _consumer = new AsyncEventingBasicConsumer(_incomingQueueChannel);
                    _consumer.Received += ConsumeResponse;
                    _incomingQueueChannel.BasicConsume(queue: _incomingQueueName,
                            autoAck: false,
                            consumer: _consumer);
                    return true;
                }
                catch (Exception)
                {
                    _incomingQueueChannel?.Abort();
                    return false;
                }
            }

            private bool AttemptConnecting()
            {
                if (_isActive || ConnectToBroker())
                {
                    if ((_outcomingQueueEstablished || ConnectToOutcomingQueue()) && (_incomingQueueEstablished || ConnectToIncomingQueue()))
                    {
                        _outcomingQueueEstablished = true;
                        _incomingQueueEstablished = true;
                        _isAvailable = true;
                        return true;
                    }
                }
                return false;
            }

            private async Task ConsumeResponse(object? sender, BasicDeliverEventArgs args)
            {
                if (_incomingQueueChannel == null || _outcomingQueueChannel == null || !_isActive || !_isAvailable || _isBlocked)
                {
                    return;
                }

                try
                {
                    var idWrapper = JsonSerializer.Deserialize<IdWrapper>(Encoding.UTF8.GetString(args.Body.ToArray()));

                    Task<string> task;

                    if (await Task.WhenAny(task = _taskHandler.CompleteTask(idWrapper.Body), Task.Delay(_continuationTimeout)) != task)
                    {
                        return;
                    }

                    _incomingQueueChannel.BasicAck(args.DeliveryTag, false);

                    var responseBody = Encoding.UTF8.GetBytes(new IdWrapper(idWrapper.Id, task.Result).ToString());

                    _outcomingQueueChannel.BasicPublish(exchange: "",
                                    routingKey: _outcomingQueueName,
                                    basicProperties: null,
                                    body: responseBody);
                }
                catch (Exception) { }
            }

            public void UpdateAvailability()
            {
                if (!_isActive || !_incomingQueueEstablished || !_outcomingQueueEstablished)
                {
                    AttemptConnecting();
                }
                if (!_isAvailable && _outcomingQueueChannel != null)
                {
                    try
                    {
                        _outcomingQueueChannel.BasicGet(_outcomingQueueName, false);
                        _isAvailable = true;
                    }
                    catch (Exception) { }
                }
            }

            public void Update()
            {
                AttemptConnecting();
            }
        }
    }
}
