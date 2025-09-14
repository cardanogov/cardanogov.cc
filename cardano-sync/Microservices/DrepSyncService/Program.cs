using DrepSyncService.Jobs;
using DrepSyncService.Services;
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

        // Register DatabaseSyncService for direct database access with failover
        services.AddScoped<DatabaseSyncService>();


        services.AddScoped<DatabaseHealthCheckService>();



        // DRep List Sync Job Configuration
        var enableListDailyTrigger = context.Configuration.GetValue<bool>(
            "DrepListSyncJob:EnableDailyTrigger"
        );
        var enableListOnStartup = context.Configuration.GetValue<bool>(
            "DrepListSyncJob:EnableOnStartup"
        );
        var listDailyCronExpression = context.Configuration["DrepListSyncJob:DailyCronExpression"];

        // DRep Epoch Summary Sync Job Configuration
        var enableSummaryDailyTrigger = context.Configuration.GetValue<bool>(
            "DrepEpochSummarySyncJob:EnableDailyTrigger"
        );
        var enableSummaryOnStartup = context.Configuration.GetValue<bool>(
            "DrepEpochSummarySyncJob:EnableOnStartup"
        );
        var summaryDailyCronExpression = context.Configuration[
            "DrepEpochSummarySyncJob:DailyCronExpression"
        ];

        // DRep Voting Power History Sync Job Configuration
        var enableVotingPowerDailyTrigger = context.Configuration.GetValue<bool>(
            "DrepVotingPowerHistorySyncJob:EnableDailyTrigger"
        );
        var enableVotingPowerOnStartup = context.Configuration.GetValue<bool>(
            "DrepVotingPowerHistorySyncJob:EnableOnStartup"
        );
        var votingPowerDailyCronExpression = context.Configuration[
            "DrepVotingPowerHistorySyncJob:DailyCronExpression"
        ];

        // DRep Info Sync Job Configuration
        var enableInfoDailyTrigger = context.Configuration.GetValue<bool>(
            "DrepInfoSyncJob:EnableDailyTrigger"
        );
        var enableInfoOnStartup = context.Configuration.GetValue<bool>(
            "DrepInfoSyncJob:EnableOnStartup"
        );
        var infoDailyCronExpression = context.Configuration[
            "DrepInfoSyncJob:DailyCronExpression"
        ];

        // DRep Metadata Sync Job Configuration
        var enableMetadataDailyTrigger = context.Configuration.GetValue<bool>(
            "DrepMetadataSyncJob:EnableDailyTrigger"
        );
        var enableMetadataOnStartup = context.Configuration.GetValue<bool>(
            "DrepMetadataSyncJob:EnableOnStartup"
        );
        var metadataDailyCronExpression = context.Configuration[
            "DrepMetadataSyncJob:DailyCronExpression"
        ];

        // DRep Delegators Sync Job Configuration
        var enableDelegatorsDailyTrigger = context.Configuration.GetValue<bool>(
            "DrepDelegatorsSyncJob:EnableDailyTrigger"
        );
        var enableDelegatorsOnStartup = context.Configuration.GetValue<bool>(
            "DrepDelegatorsSyncJob:EnableOnStartup"
        );
        var delegatorsDailyCronExpression = context.Configuration[
            "DrepDelegatorsSyncJob:DailyCronExpression"
        ];

        // DRep Updates Sync Job Configuration
        var enableUpdatesDailyTrigger = context.Configuration.GetValue<bool>(
            "DrepUpdatesSyncJob:EnableDailyTrigger"
        );
        var enableUpdatesOnStartup = context.Configuration.GetValue<bool>(
            "DrepUpdatesSyncJob:EnableOnStartup"
        );
        var updatesDailyCronExpression = context.Configuration[
            "DrepUpdatesSyncJob:DailyCronExpression"
        ];



        // Account Updates Sync Job Configuration
        var enableAccountUpdatesOnStartup = context.Configuration.GetValue<bool>(
            "AccountUpdatesSyncJob:EnableOnStartup"
        );
        var enableAccountUpdatesDailyTrigger = context.Configuration.GetValue<bool>(
            "AccountUpdatesSyncJob:EnableDailyTrigger"
        );
        var accountUpdatesDailyCronExpression = context.Configuration[
            "AccountUpdatesSyncJob:DailyCronExpression"
        ];

        // Quartz Configuration
        services.AddQuartz(q =>
        {
            // Distinct scheduler identity for clustering
            q.SchedulerId = "AUTO";
            q.SchedulerName = "drep-scheduler";

            // Use in-memory store (remove clustering/persistence)
            // Default in-memory store is used when no persistent store is configured

            // DRep List Sync Job

            q.AddJob<DrepListSyncJob>(opts => opts.WithIdentity("DrepListSyncJob"));

            // DRep Epoch Summary Sync Job
            q.AddJob<DrepEpochSummarySyncJob>(opts => opts.WithIdentity("DrepEpochSummarySyncJob"));

            // DRep Voting Power History Sync Job
            q.AddJob<DrepVotingPowerHistorySyncJob>(opts =>
                opts.WithIdentity("DrepVotingPowerHistorySyncJob")
            );

            /*
            // Removed accidental nested AddQuartz block
	        // Quartz Configuration
	        services.AddQuartz(q =>
	        {
	            // Job coordination listener
	            if (jobCoordConfig.Enabled)
	            {
	                q.AddJobListener(new SharedLibrary.Utils.QuartzJobCoordinatorListener(
	                    services.BuildServiceProvider().GetRequiredService<SharedLibrary.Interfaces.IJobCoordinator>(),
	                    jobCoordConfig,
	                    services.BuildServiceProvider().GetRequiredService<ILogger<SharedLibrary.Utils.QuartzJobCoordinatorListener>>()
	                ));
	            }

            );
            */

            // DRep Info Sync Job
            q.AddJob<DrepInfoSyncJob>(opts => opts.WithIdentity("DrepInfoSyncJob"));

            // DRep Metadata Sync Job
            q.AddJob<DrepMetadataSyncJob>(opts => opts.WithIdentity("DrepMetadataSyncJob"));

            // DRep Delegators Sync Job
            q.AddJob<DrepDelegatorsSyncJob>(opts => opts.WithIdentity("DrepDelegatorsSyncJob"));

            // DRep Updates Sync Job
            q.AddJob<DrepUpdatesSyncJob>(opts => opts.WithIdentity("DrepUpdatesSyncJob"));

            // Account Updates Sync Job
            q.AddJob<AccountUpdatesSyncJob>(opts => opts.WithIdentity("AccountUpdatesSyncJob"));

            if (enableListDailyTrigger)
            {
                q.AddTrigger(opts =>
                    opts.ForJob("DrepListSyncJob")
                        .WithIdentity("DrepListSyncJob-daily-trigger")
                        .WithCronSchedule(
                            listDailyCronExpression,
                            x => x.InTimeZone(TimeZoneInfo.Utc)
                        )
                );
            }

            if (enableSummaryDailyTrigger)
            {
                q.AddTrigger(opts =>
                    opts.ForJob("DrepEpochSummarySyncJob")
                        .WithIdentity("DrepEpochSummarySyncJob-daily-trigger")
                        .WithCronSchedule(
                            summaryDailyCronExpression,
                            x => x.InTimeZone(TimeZoneInfo.Utc)
                        )
                );
            }

            if (enableVotingPowerDailyTrigger)
            {
                q.AddTrigger(opts =>
                    opts.ForJob("DrepVotingPowerHistorySyncJob")
                        .WithIdentity("DrepVotingPowerHistorySyncJob-daily-trigger")
                        .WithCronSchedule(
                            votingPowerDailyCronExpression,
                            x => x.InTimeZone(TimeZoneInfo.Utc)
                        )
                );
            }

            if (enableInfoDailyTrigger)
            {
                q.AddTrigger(opts =>
                    opts.ForJob("DrepInfoSyncJob")
                        .WithIdentity("DrepInfoSyncJob-daily-trigger")
                        .WithCronSchedule(
                            infoDailyCronExpression,
                            x => x.InTimeZone(TimeZoneInfo.Utc)
                        )
                );
            }

            if (enableMetadataDailyTrigger)
            {
                q.AddTrigger(opts =>
                    opts.ForJob("DrepMetadataSyncJob")
                        .WithIdentity("DrepMetadataSyncJob-daily-trigger")
                        .WithCronSchedule(
                            metadataDailyCronExpression,
                            x => x.InTimeZone(TimeZoneInfo.Utc)
                        )
                );
            }

            if (enableDelegatorsDailyTrigger)
            {
                q.AddTrigger(opts =>
                    opts.ForJob("DrepDelegatorsSyncJob")
                        .WithIdentity("DrepDelegatorsSyncJob-daily-trigger")
                        .WithCronSchedule(
                            delegatorsDailyCronExpression,
                            x => x.InTimeZone(TimeZoneInfo.Utc)
                        )
                );
            }

            if (enableUpdatesDailyTrigger)
            {
                q.AddTrigger(opts =>
                    opts.ForJob("DrepUpdatesSyncJob")
                        .WithIdentity("DrepUpdatesSyncJob-daily-trigger")
                        .WithCronSchedule(
                            updatesDailyCronExpression,
                            x => x.InTimeZone(TimeZoneInfo.Utc)
                        )
                );
            }

            if (enableAccountUpdatesDailyTrigger)
            {
                q.AddTrigger(opts =>
                    opts.ForJob("AccountUpdatesSyncJob")
                        .WithIdentity("AccountUpdatesSyncJob-daily-trigger")
                        .WithCronSchedule(
                            accountUpdatesDailyCronExpression,
                            x => x.InTimeZone(TimeZoneInfo.Utc)
                        )
                );
            }

            // Startup triggers

            if (enableListOnStartup)
            {
                q.AddTrigger(opts =>
                    opts.ForJob("DrepListSyncJob")
                        .WithIdentity("DrepListSyncJob-startup-trigger")
                        .StartNow()
                        .WithSimpleSchedule(x => x.WithMisfireHandlingInstructionFireNow())
                );
            }

            if (enableSummaryOnStartup)
            {
                q.AddTrigger(opts =>
                    opts.ForJob("DrepEpochSummarySyncJob")
                        .WithIdentity("DrepEpochSummarySyncJob-startup-trigger")
                        .StartAt(DateTimeOffset.UtcNow.AddSeconds(30)) // Delay 30 seconds after DrepSyncJob
                        .WithSimpleSchedule(x => x.WithMisfireHandlingInstructionFireNow())
                );
            }

            if (enableVotingPowerOnStartup)
            {
                q.AddTrigger(opts =>
                    opts.ForJob("DrepVotingPowerHistorySyncJob")
                        .WithIdentity("DrepVotingPowerHistorySyncJob-startup-trigger")
                        .StartAt(DateTimeOffset.UtcNow.AddSeconds(60)) // Delay 60 seconds after other jobs
                        .WithSimpleSchedule(x => x.WithMisfireHandlingInstructionFireNow())
                );
            }

            if (enableInfoOnStartup)
            {
                q.AddTrigger(opts =>
                    opts.ForJob("DrepInfoSyncJob")
                        .WithIdentity("DrepInfoSyncJob-startup-trigger")
                        .StartAt(DateTimeOffset.UtcNow.AddSeconds(90)) // Delay 90 seconds after other jobs
                        .WithSimpleSchedule(x => x.WithMisfireHandlingInstructionFireNow())
                );
            }

            if (enableMetadataOnStartup)
            {
                q.AddTrigger(opts =>
                    opts.ForJob("DrepMetadataSyncJob")
                        .WithIdentity("DrepMetadataSyncJob-startup-trigger")
                        .StartAt(DateTimeOffset.UtcNow.AddSeconds(120)) // Delay 120 seconds after DrepInfo job
                        .WithSimpleSchedule(x => x.WithMisfireHandlingInstructionFireNow())
                );
            }

            if (enableDelegatorsOnStartup)
            {
                q.AddTrigger(opts =>
                    opts.ForJob("DrepDelegatorsSyncJob")
                        .WithIdentity("DrepDelegatorsSyncJob-startup-trigger")
                        .StartAt(DateTimeOffset.UtcNow.AddSeconds(150)) // Delay 150 seconds after Metadata job
                        .WithSimpleSchedule(x => x.WithMisfireHandlingInstructionFireNow())
                );
            }

            if (enableUpdatesOnStartup)
            {
                q.AddTrigger(opts =>
                    opts.ForJob("DrepUpdatesSyncJob")
                        .WithIdentity("DrepUpdatesSyncJob-startup-trigger")
                        .StartAt(DateTimeOffset.UtcNow.AddSeconds(180)) // Delay 180 seconds after Metadata job
                        .WithSimpleSchedule(x => x.WithMisfireHandlingInstructionFireNow())
                );
            }

            if (enableAccountUpdatesOnStartup)
            {
                q.AddTrigger(opts =>
                    opts.ForJob("AccountUpdatesSyncJob")
                        .WithIdentity("AccountUpdatesSyncJob-startup-trigger")
                        .StartAt(DateTimeOffset.UtcNow.AddSeconds(200)) // Delay 200 seconds after Updates job
                        .WithSimpleSchedule(x => x.WithMisfireHandlingInstructionFireNow())
                );
            }
        });
        services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);
    }
);

await builder.Build().RunAsync();
