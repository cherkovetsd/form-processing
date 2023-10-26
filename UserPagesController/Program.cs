using DatabaseInfo;
using Microsoft.EntityFrameworkCore;
using UserSideServices.Options;
using UserSideServices.Service;
using Utilities.Messaging.Publisher.Factory;
using Utilities.Messaging.Publisher.Factory.Options;
using Utilities.Updating;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddPooledDbContextFactory<Context>(
    o => o.UseNpgsql(builder.Configuration.GetConnectionString("Connection")).EnableSensitiveDataLogging());

builder.Services.AddControllers();

builder.Services.Configure<PageTaskQueueOptions>(
    builder.Configuration.GetSection(PageTaskQueueOptions.Position));

builder.Services.Configure<RecordTaskQueueOptions>(
    builder.Configuration.GetSection(RecordTaskQueueOptions.Position));

builder.Services.Configure<UpdateStateTransitionOptions>(
    builder.Configuration.GetSection(UpdateStateTransitionOptions.Position));

builder.Services.AddSingleton<IControllerQueueFactory, RabbitMQQueueFactory>();

builder.Services.AddSingleton<IUserPagesService, UserPagesService>();

builder.Services.AddSingleton<IFormRecordService, FormRecordService>();

builder.Services.AddSingleton<UpdateController>();

var app = builder.Build();

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=UserPages}/{action=Index}/{id?}");

app.Run();
