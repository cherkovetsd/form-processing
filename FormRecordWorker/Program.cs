using FormRecordWorker;
using Utilities.Updating;
using Utilities.Worker;
using Utilities.Messaging.Consumer;
using DatabaseInfo;
using Microsoft.EntityFrameworkCore;
using UserSideServices.Options;
using UserSideServices.Service;
using Utilities.Messaging.Consumer.Options;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddPooledDbContextFactory<Context>(
    o => o.UseNpgsql(builder.Configuration.GetConnectionString("Connection")).EnableSensitiveDataLogging());

builder.Services.Configure<UpdateStateTransitionOptions>(
    builder.Configuration.GetSection(UpdateStateTransitionOptions.Position));

builder.Services.Configure<RabbitMQWorkerOptions>(
    builder.Configuration.GetSection(RabbitMQWorkerOptions.Position));

builder.Services.Configure<UpdateControllerOptions>(
    builder.Configuration.GetSection(UpdateControllerOptions.Position));

builder.Services.AddSingleton<IEncodedTaskHandler, FormRecordEncodedTaskHandler>();

builder.Services.AddSingleton<IFormRecordService, FormRecordService>();

builder.Services.Configure<RabbitMQWorkerOptions>(
    builder.Configuration.GetSection(RabbitMQWorkerOptions.Position));

builder.Services.AddHostedService<RabbitMQWorkerWithId>();

builder.Services.AddSingleton<UpdateController>();

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var host = builder.Build();

host.Run();