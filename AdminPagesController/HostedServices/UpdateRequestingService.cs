using System.Text.Json;
using System.Text.Json.Serialization;
using AdminPagesController.HostedServices.Options;
using AdminSideServices.Service;
using Data.Requests;
using Data.Tasks;
using Microsoft.Extensions.Options;
using Utilities.Messaging.Publisher.Factory;
using Utilities.Queue;
using Utilities.Tasks;
using Utilities.Updating;
using static Utilities.Controller.ControllerTools;

namespace AdminPagesController.HostedServices;

public class UpdateRequestingService : BackgroundService, IDisposable
{
    private Service? _service;
    private readonly UpdateController _updateController;
    private readonly ITaskQueue _queue;
    private readonly IFormStateService _formService;
    private readonly TimeSpan _evaluationTime;
    private readonly int _updateRateTicks;

    public UpdateRequestingService(UpdateController updateController, IFormStateService formService,
        IOptions<EvaluationTimeOptions> options, IControllerQueueFactory queueFactory)
    {
        _updateController = updateController;
        _formService = formService;
        _evaluationTime = options.Value.Time ?? throw new ArgumentException("Параметр EvaluationTime не передан");
        _updateRateTicks = options.Value.UpdateInterval ??
                           throw new ArgumentException("Параметр UpdateInterval не передан"); 
        _queue = queueFactory.GetRecordTaskQueue();
    }
    
    public override void Dispose()
    {
        _service?.Dispose();
        base.Dispose();
    }
    
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _service = new Service(_updateController, _queue, _formService, _evaluationTime, _updateRateTicks);
        
        return Task.CompletedTask;
    }
    public class Service : IUpdateable, IDisposable
    {
        private readonly UpdateController _updateController;
        private readonly ITaskQueue _queue;
        private readonly IFormStateService _formService;
        private readonly TimeSpan _evaluationTime;
    
        public Service(UpdateController updateController, ITaskQueue queue, IFormStateService formService,
            TimeSpan evaluationTime, int updateRateTicks)
        {
            _updateController = updateController;
            _queue = queue;
            _formService = formService;
            _evaluationTime = evaluationTime;
            _updateController.Add(this, updateRateTicks);
        }
    
        private class EvaluationStateUpdateTask : EventBasedQueuedTask
        {
            private EvaluationStateUpdateRequest Request { get; }

            public EvaluationStateUpdateTask(EvaluationStateUpdateRequest request)
            {
                Request = request;
            }
        
            public override string SerializeTask()
            {
                return new RequestMessageWrapper(RequestType.EvaluationStateUpdate, JsonSerializer.Serialize(Request))
                    .ToString();
            }
        }
    
        public void Update()
        {
            var request = new EvaluationStateUpdateRequest(_evaluationTime);
            var task = new EvaluationStateUpdateTask(request);
            _ = CompleteFireAndForgetAction(task, _queue,
                async () => await _formService.UpdateEvaluationState(request));
        }

        public void Dispose()
        {
            _updateController.Stop(this);
        }
    }
}