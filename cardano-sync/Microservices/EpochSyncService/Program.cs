using EpochSyncService.Jobs;
using EpochSyncService.Services;
using Microsoft.EntityFrameworkCore;
using Quartz;
using Serilog;
using SharedLibrary.DatabaseContext;

// Fast health check path for container health probes
if (args.Contains("--health-check"))
{
    Console.WriteLine("OK");
    return;
}

var builder = Host.CreateDefaultBuilder(args);

builder.UseSerilog(
    (context, services, configuration) =>
        configuration.ReadFrom.Configuration(context.Configuration)
);

builder.ConfigureServices(
    (context, services) =>
    {
        // Database Context - Entity Framework will use Npgsql provider based on connection string
        var connectionString = context.Configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<CardanoDbContext>(options => options.UseNpgsql(connectionString));

        // HTTP Client for Adastat API
        services.AddHttpClient(
            "AdastatApi",
            c =>
            {
                c.BaseAddress = new Uri("https://api.adastat.net/");
                c.Timeout = TimeSpan.FromSeconds(30);
            })
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler()
            {
                MaxConnectionsPerServer = 10,
                UseCookies = false
            });

        // Register AdastatApiClient
        services.AddSingleton<IAdastatApiClient>(sp => new AdastatApiClient(
            sp.GetRequiredService<IHttpClientFactory>().CreateClient("AdastatApi"),
            sp.GetRequiredService<ILogger<AdastatApiClient>>()
        ));

        // Register DatabaseSyncService for backup database operations
        services.AddSingleton<DatabaseSyncService>();





        // Epoch Sync Job Configuration
        var enableDailyTrigger = context.Configuration.GetValue<bool>("EpochSyncJob:EnableDailyTrigger");
        var enableOnStartup = context.Configuration.GetValue<bool>("EpochSyncJob:EnableOnStartup");
        var dailyCronExpression = context.Configuration["EpochSyncJob:DailyCronExpression"];

        // Epoch Protocol Parameters Sync Job Configuration
        var enableParamsDailyTrigger = context.Configuration.GetValue<bool>("EpochProtocolParametersSyncJob:EnableDailyTrigger");
        var enableParamsOnStartup = context.Configuration.GetValue<bool>("EpochProtocolParametersSyncJob:EnableOnStartup");
        var paramsDailyCronExpression = context.Configuration["EpochProtocolParametersSyncJob:DailyCronExpression"];



        // Adastat Epoch Sync Job Configuration
        var enableAdastatDailyTrigger = context.Configuration.GetValue<bool>("AdastatEpochSyncJob:EnableDailyTrigger", true);
        var enableAdastatOnStartup = context.Configuration.GetValue<bool>("AdastatEpochSyncJob:EnableOnStartup", true);
        var adastatDailyCronExpression = context.Configuration["AdastatEpochSyncJob:DailyCronExpression"]; // Daily at 2 AM UTC

        // Adastat DReps Sync Job Configuration
        var enableAdastatDrepsDailyTrigger = context.Configuration.GetValue<bool>("AdastatDrepsSyncJob:EnableDailyTrigger", true);
        var enableAdastatDrepsOnStartup = context.Configuration.GetValue<bool>("AdastatDrepsSyncJob:EnableOnStartup", true);
        var adastatDrepsDailyCronExpression = context.Configuration["AdastatDrepsSyncJob:DailyCronExpression"]; // Daily at 2:30 AM UTC

        // Quartz Configuration
        services.AddQuartz(q =>
        {
            // Distinct scheduler identity per microservice
            q.SchedulerId = "AUTO";
            q.SchedulerName = "epoch-scheduler";

            // Use in-memory store (no persistence/clustering)
            // Default in-memory store is used when no persistent store is configured

            // Epoch Sync Job
            q.AddJob<EpochSyncJob>(opts => opts.WithIdentity("EpochSyncJob"));

            // Epoch Protocol Parameters Sync Job
            q.AddJob<EpochProtocolParametersSyncJob>(opts => opts.WithIdentity("EpochProtocolParametersSyncJob"));


            // Adastat Epoch Sync Job
            q.AddJob<AdastatEpochSyncJob>(opts => opts.WithIdentity("AdastatEpochSyncJob"));

            // Adastat DReps Sync Job
            q.AddJob<AdastatDrepsSyncJob>(opts => opts.WithIdentity("AdastatDrepsSyncJob"));

            // Daily triggers
            if (enableDailyTrigger)
            {
                q.AddTrigger(opts => opts
                    .ForJob("EpochSyncJob")
                    .WithIdentity("EpochSyncJob-daily-trigger")
                    .WithCronSchedule(dailyCronExpression, x => x.InTimeZone(TimeZoneInfo.Utc))
                );
            }

            if (enableParamsDailyTrigger)
            {
                q.AddTrigger(opts => opts
                    .ForJob("EpochProtocolParametersSyncJob")
                    .WithIdentity("EpochProtocolParametersSyncJob-daily-trigger")
                    .WithCronSchedule(paramsDailyCronExpression, x => x.InTimeZone(TimeZoneInfo.Utc))
                );
            }

            if (enableAdastatDailyTrigger)
            {
                q.AddTrigger(opts => opts
                    .ForJob("AdastatEpochSyncJob")
                    .WithIdentity("AdastatEpochSyncJob-daily-trigger")
                    .WithCronSchedule(adastatDailyCronExpression, x => x.InTimeZone(TimeZoneInfo.Utc))
                );
            }

            if (enableAdastatDrepsDailyTrigger)
            {
                q.AddTrigger(opts => opts
                    .ForJob("AdastatDrepsSyncJob")
                    .WithIdentity("AdastatDrepsSyncJob-daily-trigger")
                    .WithCronSchedule(adastatDrepsDailyCronExpression, x => x.InTimeZone(TimeZoneInfo.Utc))
                );
            }

            // Startup triggers
            if (enableOnStartup)
            {
                q.AddTrigger(opts => opts
                    .ForJob("EpochSyncJob")
                    .WithIdentity("EpochSyncJob-startup-trigger")
                    .StartNow()
                    .WithSimpleSchedule(x => x.WithMisfireHandlingInstructionFireNow())
                );
            }

            if (enableParamsOnStartup)
            {
                q.AddTrigger(opts => opts
                    .ForJob("EpochProtocolParametersSyncJob")
                    .WithIdentity("EpochProtocolParametersSyncJob-startup-trigger")
                    .StartAt(DateTime.UtcNow.AddSeconds(30))
                    .WithSimpleSchedule(x => x.WithMisfireHandlingInstructionFireNow())
                );
            }

            if (enableAdastatOnStartup)
            {
                q.AddTrigger(opts => opts
                    .ForJob("AdastatEpochSyncJob")
                    .WithIdentity("AdastatEpochSyncJob-startup-trigger")
                    .StartAt(DateTimeOffset.UtcNow.AddSeconds(30))
                    .WithSimpleSchedule(x => x.WithMisfireHandlingInstructionFireNow())
                );
            }

            if (enableAdastatDrepsOnStartup)
            {
                q.AddTrigger(opts => opts
                    .ForJob("AdastatDrepsSyncJob")
                    .WithIdentity("AdastatDrepsSyncJob-startup-trigger")
                    .StartAt(DateTimeOffset.UtcNow.AddSeconds(90)) // Delay 90s after other jobs
                    .WithSimpleSchedule(x => x.WithMisfireHandlingInstructionFireNow())
                );
            }
        });
        services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

    }
);

await builder.Build().RunAsync();
