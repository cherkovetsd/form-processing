using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using Utilities.Messaging.Options;
using Utilities.Queue;
using Utilities.Tasks;
using Utilities.Updating;

namespace Utilities.Messaging.Publisher
{
    public class RabbitMQTaskQueueWithId : ITaskQueue
    {
        private readonly Queue _queue;
        private readonly IUpdateController _updateController;

        public RabbitMQTaskQueueWithId(RabbitMQServiceOptions options, IUpdateController updateController)
        {
            try
            {
                _queue = new Queue(
                    (options.BrokerAddress ?? throw new ArgumentException()).Hostname,
                    options.BrokerAddress.Port,
                    options.BrokerAddress.Uri,
                    options.OutcomingQueueName ?? throw new ArgumentException(),
                    options.IncomingQueueName ?? throw new ArgumentException(),
                    options.ContinuationTimeout ?? throw new ArgumentException());

                _updateController = updateController;
                _updateController.Add(_queue);
            }
            catch (Exception)
            {
                _queue?.Dispose();
                throw new ArgumentException("Некорректные опции для RabbitMQTaskQueueWithId");
            }
        }
        public void Dispose()
        {
            _updateController.Stop();
            _updateController.Remove(_queue);
            _queue.Dispose();
        }

        public async Task<bool> Push(IQueuedTask task)
        {
            return await _queue.Push(task);
        }


        public class Queue : ITaskQueue, IUpdateable
        {
            private IConnection? _connection;
            private IModel? _outcomingQueueChannel;
            private IModel? _incomingQueueChannel;
            private readonly string _outcomingQueueName;
            private readonly string _incomingQueueName;
            private bool _isAvailable = false;
            private bool _outcomingQueueEstablished = false;
            private bool _incomingQueueEstablished = false;
            private readonly Dictionary<Guid, TaskCompletionSource<string>> _queueResponses = new();
            private readonly TimeSpan _continuationTimeout;
            private EventingBasicConsumer? _consumer = null;

            private bool _isBlocked = false;
            private readonly ConnectionFactory _factory;
            private bool _isActive = false;

            public Queue(string? hostName, int? port, Uri? uri, string outcomingQueueName, string incomingQueueName, TimeSpan continuationTimeout)
            {
                _factory = new ConnectionFactory();
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
                    throw new ArgumentException();
                }

                _outcomingQueueName = outcomingQueueName ?? throw new ArgumentException();
                _incomingQueueName = incomingQueueName ?? throw new ArgumentException();
                _continuationTimeout = continuationTimeout;
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
                CancelQueue();
            }

            public void Dispose()
            {
                if (_connection != null)
                {
                    _incomingQueueChannel.QueueDeleteNoWait(_incomingQueueName);
                    _incomingQueueChannel.QueueDeleteNoWait(_outcomingQueueName);
                    _incomingQueueChannel?.Abort();
                    _incomingQueueChannel?.Dispose();
                    _outcomingQueueChannel?.Abort();
                    _outcomingQueueChannel?.Dispose();
                    _connection.Abort();
                    _connection.Dispose();
                }
                CancelQueue();
                _isActive = false;
            }

            private void OnOutcomingQueueChannelShutdown(object? sender, ShutdownEventArgs? args)
            {
                _outcomingQueueEstablished = false;
                _outcomingQueueChannel = null;
                CancelQueue();
            }

            private void OnIncomingQueueChannelShutdown(object? sender, ShutdownEventArgs? args)
            {
                _incomingQueueEstablished = false;
                _incomingQueueChannel = null;
                CancelQueue();
            }

            private IConnection? GetConnection()
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
                if (_isActive)
                {
                    return true;
                }

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
                    _consumer = new EventingBasicConsumer(_incomingQueueChannel);
                    _consumer.Received += ConsumeResponse;
                    _incomingQueueChannel.BasicConsume(queue: _incomingQueueName,
                            autoAck: true,
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

            private void CancelQueue()
            {
                _isAvailable = false;
                _outcomingQueueChannel?.QueuePurge(_outcomingQueueName);

                foreach (var response in _queueResponses)
                {
                    response.Value.SetCanceled();
                }
            }

            private void ConsumeResponse(object? sender, BasicDeliverEventArgs args)
            {
                if (_incomingQueueChannel == null)
                {
                    return;
                }
                var body = args.Body.ToArray();
                var bodyString = Encoding.UTF8.GetString(body);
                var idWrapper = JsonSerializer.Deserialize<IdWrapper>(bodyString);
                var response = _queueResponses[idWrapper.Id];
                if (response != null)
                {
                    response.SetResult(idWrapper.Body);
                }
                _incomingQueueChannel.BasicAck(args.DeliveryTag, false);
            }

            private void TerminateOutcomingQueue()
            {
                _outcomingQueueEstablished = false;
                _outcomingQueueChannel?.Abort();
                _outcomingQueueChannel = null;
                CancelQueue();
            }

            private async Task ProcessTask(Guid id, IQueuedTask task)
            {
                var taskSource = new TaskCompletionSource<string>();
                _queueResponses[id] = taskSource;

                var result = await Task.WhenAny(taskSource.Task, Task.Delay(_continuationTimeout));
                if (result == taskSource.Task && result.IsCompleted)
                {
                    task.RespondBack(taskSource.Task.Result);
                }
                else
                {
                    await task.OnCanceled();
                }
            }

            public Task<bool> Push(IQueuedTask task)
            {
                if (_outcomingQueueChannel == null || !_isActive || !_isAvailable || _isBlocked)
                {
                    return Task.FromResult(false);
                }

                Guid id;
                do
                {
                    id = new Guid();
                } while (_queueResponses.ContainsKey(id));

                var body = Encoding.UTF8.GetBytes(new IdWrapper(id, task.SerializeTask()).ToString());
                try
                {
                    _outcomingQueueChannel.BasicPublish(exchange: "",
                                    routingKey: _outcomingQueueName,
                                    basicProperties: null,
                                    body: body);
                    _outcomingQueueChannel.WaitForConfirmsOrDie();

                    _ = ProcessTask(id, task);
                    
                    return Task.FromResult(true);
                }
                catch (Exception) { }
                TerminateOutcomingQueue();
                return Task.FromResult(false);
            }

            public void Update()
            {
                AttemptConnecting();
            }
        }
    }
}


