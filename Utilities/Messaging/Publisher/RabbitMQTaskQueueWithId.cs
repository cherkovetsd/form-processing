using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using Utilities.Messaging.Options;
using Utilities.Queue;
using Utilities.Tasks;
using Utilities.Updating;

namespace Utilities.Messaging.Publisher
{
    public class RabbitMQTaskQueueWithId : ITaskQueue, IDisposable
    {
        private readonly Queue _queue;
        public RabbitMQTaskQueueWithId(RabbitMQServiceOptions options, UpdateController updateController)
        {
            try
            {
                _queue = new Queue(
                    (options.BrokerAddress ?? throw new ArgumentException()).Hostname,
                    options.BrokerAddress.Port,
                    options.BrokerAddress.Uri,
                    options.OutcomingQueueName ?? throw new ArgumentException(),
                    options.IncomingQueueName ?? throw new ArgumentException(),
                    options.ContinuationTimeout ?? throw new ArgumentException(),
                    options.UpdateRateTicks ?? throw new ArgumentException(),
                    updateController);
            }
            catch (Exception)
            {
                _queue?.Dispose();
                throw new ArgumentException("Некорректные опции для RabbitMQTaskQueueWithId");
            }
        }
        public void Dispose()
        {
            Console.WriteLine("DONE2");
            _queue.Dispose();
            GC.SuppressFinalize(this);
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
            private readonly UpdateController _updateController;
            private bool _isBlocked = false;
            private readonly ConnectionFactory _factory;
            private bool _isActive = false;
            private bool _connectingInProcess = false;

            public Queue(string? hostName, int? port, Uri? uri, string outcomingQueueName, string incomingQueueName,
                TimeSpan continuationTimeout, int updateRateTicks, UpdateController updateController)
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

                _updateController = updateController;
                _updateController.Add(this, updateRateTicks);
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
                _isActive = false;
                _updateController.Stop(this);
                
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
                    Console.WriteLine("DONE");
                }
                CancelQueue();
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
                if (_connectingInProcess)
                {
                    return _isAvailable;
                }
                _connectingInProcess = true;

                try
                {
                    if (_isActive || ConnectToBroker())
                    {
                        if ((_outcomingQueueEstablished || ConnectToOutcomingQueue()) &&
                            (_incomingQueueEstablished || ConnectToIncomingQueue()))
                        {
                            _outcomingQueueEstablished = true;
                            _incomingQueueEstablished = true;
                            _isAvailable = true;
                            return true;
                        }
                    }
                }
                catch (Exception)  {}
                finally
                {
                    _connectingInProcess = false;
                }
                return false;
            }

            private void CancelQueue()
            {
                _isAvailable = false;
                try
                {
                    _outcomingQueueChannel?.QueuePurge(_outcomingQueueName);    
                } catch (Exception) {}

                foreach (var response in _queueResponses)
                {
                    response.Value.TrySetCanceled();
                }
            }

            private void ConsumeResponse(object? sender, BasicDeliverEventArgs args)
            {
                try
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
                        response.TrySetResult(idWrapper.Body);
                    }
                    //_incomingQueueChannel.BasicAck(args.DeliveryTag, false);
                } catch (Exception) {}
            }

            private void TerminateOutcomingQueue()
            {
                _outcomingQueueEstablished = false;
                _outcomingQueueChannel?.Abort();
                _outcomingQueueChannel = null;
                CancelQueue();
            }

            private async Task ProcessTaskResponse(Guid id, IQueuedTask task)
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
                    id = Guid.NewGuid();
                } while (_queueResponses.ContainsKey(id));

                var body = Encoding.UTF8.GetBytes(new IdWrapper(id, task.SerializeTask()).ToString());
                try
                {
                    _outcomingQueueChannel.BasicPublish(exchange: "",
                                    routingKey: _outcomingQueueName,
                                    basicProperties: null,
                                    body: body);
                    _outcomingQueueChannel.WaitForConfirmsOrDie();

                    _ = ProcessTaskResponse(id, task);
                    
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


