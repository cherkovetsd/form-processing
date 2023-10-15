using Utilities.Updating;
using AdminSideServices.Options;
using DatabaseInfo;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using AdminSideServices.Service;
using FormStateWorker;
using Utilities.Worker;
using Utilities.Messaging.Consumer.Options;
using Utilities.Messaging.Consumer;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddPooledDbContextFactory<Context>(
    o => o.UseNpgsql(builder.Configuration.GetConnectionString("Connection")).EnableSensitiveDataLogging());

builder.Services.Configure<UpdateControllerOptions>(
    builder.Configuration.GetSection(UpdateControllerOptions.Position));

builder.Services.Configure<FormStateServiceOptions>(
    builder.Configuration.GetSection(FormStateServiceOptions.Position));

builder.Services.AddSingleton<UpdateController>();

builder.Services.AddSingleton<IFormStateService, FormStateService>();

builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(opt =>
{
    opt.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddSingleton<IEncodedTaskHandler, FormStateEncodedTaskHandler>();

builder.Services.Configure<RabbitMQWorkerOptions>(
    builder.Configuration.GetSection(RabbitMQWorkerOptions.Position));

builder.Services.AddHostedService<RabbitMQWorkerWithId>();

var host = builder.Build();

host.Run();