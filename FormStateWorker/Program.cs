using Utilities.Updating;
using AdminSideServices.Options;
using DatabaseInfo;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using AdminSideServices.Service;
using FormStateWorker;
using Npgsql;
using Utilities.Worker;
using Utilities.Messaging.Consumer.Options;
using Utilities.Messaging.Consumer;

var builder = Host.CreateApplicationBuilder(args);

var conStrBuilder = new NpgsqlConnectionStringBuilder(builder.Configuration.GetConnectionString("Connection"))
{
    Password = builder.Configuration["DbPassword"]
};
builder.Services.AddPooledDbContextFactory<Context>(
    o => o.UseNpgsql(conStrBuilder.ConnectionString).EnableSensitiveDataLogging());

builder.Services.Configure<EvaluationStateTransitionOptions>(
    builder.Configuration.GetSection(EvaluationStateTransitionOptions.Position));

builder.Services.AddSingleton<UpdateController>();

builder.Services.AddSingleton<IFormStateService, FormStateService>();

builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(opt =>
{
    opt.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddSingleton<IEncodedRequestHandler, FormStateEncodedRequestHandler>();

builder.Services.Configure<RabbitMQWorkerOptions>(
    builder.Configuration.GetSection(RabbitMQWorkerOptions.Position));

builder.Services.AddHostedService<RabbitMQWorkerWithId>();

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var host = builder.Build();

host.Run();