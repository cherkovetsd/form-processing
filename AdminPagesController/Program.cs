using Utilities.Updating;
using DatabaseInfo;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using AdminPagesController.HostedServices;
using AdminPagesController.HostedServices.Options;
using AdminSideServices.Options;
using AdminSideServices.Service;
using Npgsql;
using Utilities.Messaging.Publisher.Factory;
using Utilities.Messaging.Publisher.Factory.Options;

var builder = WebApplication.CreateBuilder(args);

var conStrBuilder = new NpgsqlConnectionStringBuilder(builder.Configuration.GetConnectionString("Connection"))
    {
        Password = builder.Configuration["DbPassword"]
    };
builder.Services.AddPooledDbContextFactory<Context>(
    o => o.UseNpgsql(conStrBuilder.ConnectionString).EnableSensitiveDataLogging());

builder.Services.AddControllers();

builder.Services.Configure<PageTaskQueueOptions>(
    builder.Configuration.GetSection(PageTaskQueueOptions.Position));

builder.Services.Configure<RecordTaskQueueOptions>(
    builder.Configuration.GetSection(RecordTaskQueueOptions.Position));

builder.Services.Configure<EvaluationStateTransitionOptions>(
    builder.Configuration.GetSection(EvaluationStateTransitionOptions.Position));

builder.Services.Configure<EvaluationTimeOptions>(
    builder.Configuration.GetSection(EvaluationTimeOptions.Position));

builder.Services.AddSingleton<UpdateController>();

builder.Services.AddSingleton<IControllerQueueFactory, RabbitMQQueueFactory>();

builder.Services.AddSingleton<IAdminPagesService, AdminPagesService>();

builder.Services.AddSingleton<IFormStateService, FormStateService>();

builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(opt =>
{
    opt.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

//builder.Services.AddHostedService<UpdateRequestingService>();

var app = builder.Build();

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/AdminPages/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=AdminPages}/{action=Index}/{id?}");

app.Run();