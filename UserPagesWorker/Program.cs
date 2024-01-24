using DatabaseInfo;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using Npgsql;
using UserPagesWorker;
using UserSideServices.Options;
using UserSideServices.Service;
using Utilities.Messaging.Consumer;
using Utilities.Messaging.Consumer.Options;
using Utilities.Updating;
using Utilities.Worker;

var builder = Host.CreateApplicationBuilder(args);

var conStrBuilder = new NpgsqlConnectionStringBuilder(builder.Configuration.GetConnectionString("Connection"))
{
    Password = builder.Configuration["DbPassword"]
};
builder.Services.AddPooledDbContextFactory<Context>(
    o => o.UseNpgsql(conStrBuilder.ConnectionString).EnableSensitiveDataLogging());

builder.Services.Configure<UpdateStateTransitionOptions>(
    builder.Configuration.GetSection(UpdateStateTransitionOptions.Position));

builder.Services.AddSingleton<UpdateController>();

builder.Services.AddSingleton<IUserPagesService, UserPagesService>();

builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(opt =>
{
    opt.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.Configure<RabbitMQWorkerOptions>(
    builder.Configuration.GetSection(RabbitMQWorkerOptions.Position));

builder.Services.AddSingleton<IEncodedRequestHandler, UserPagesEncodedRequestHandler>();

builder.Services.AddHostedService<RabbitMQWorkerWithId>();

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var host = builder.Build();

host.Run();
