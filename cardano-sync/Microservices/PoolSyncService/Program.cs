using Microsoft.EntityFrameworkCore;
using PoolSyncService.Jobs;
using PoolSyncService.Services;
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

        // Register DatabaseSyncService for backup database access
        services.AddSingleton<DatabaseSyncService>();

        // Pool List Sync Job Configuration



        var enablePoolListDailyTrigger = context.Configuration.GetValue<bool>(
            "PoolListSyncJob:EnableDailyTrigger"
        );
        var enablePoolListOnStartup = context.Configuration.GetValue<bool>(
            "PoolListSyncJob:EnableOnStartup"
        );
        var poolListDailyCronExpression = context.Configuration[
            "PoolListSyncJob:DailyCronExpression"
        ];

        // Pool Voting Power History Sync Job Configuration
        var enablePoolVotingPowerHistoryDailyTrigger = context.Configuration.GetValue<bool>(
            "PoolVotingPowerHistorySyncJob:EnableDailyTrigger"
        );
        var enablePoolVotingPowerHistoryOnStartup = context.Configuration.GetValue<bool>(
            "PoolVotingPowerHistorySyncJob:EnableOnStartup"
        );
        var poolVotingPowerHistoryDailyCronExpression = context.Configuration[
            "PoolVotingPowerHistorySyncJob:DailyCronExpression"
        ];

        // Pool Metadata Sync Job Configuration
        var enablePoolMetadataDailyTrigger = context.Configuration.GetValue<bool>(
            "PoolMetadataSyncJob:EnableDailyTrigger"
        );
        var enablePoolMetadataOnStartup = context.Configuration.GetValue<bool>(
            "PoolMetadataSyncJob:EnableOnStartup"
        );
        var poolMetadataDailyCronExpression = context.Configuration[
            "PoolMetadataSyncJob:DailyCronExpression"
        ];

        // Pool Stake Snapshot Sync Job Configuration
        var enablePoolStakeSnapshotDailyTrigger = context.Configuration.GetValue<bool>(
            "PoolStakeSnapshotSyncJob:EnableDailyTrigger"
        );
        var enablePoolStakeSnapshotOnStartup = context.Configuration.GetValue<bool>(
            "PoolStakeSnapshotSyncJob:EnableOnStartup"
        );
        var poolStakeSnapshotDailyCronExpression = context.Configuration[
            "PoolStakeSnapshotSyncJob:DailyCronExpression"
        ];

        // Pool Delegators Sync Job Configuration
        var enablePoolDelegatorsDailyTrigger = context.Configuration.GetValue<bool>(
            "PoolDelegatorsSyncJob:EnableDailyTrigger"
        );
        var enablePoolDelegatorsOnStartup = context.Configuration.GetValue<bool>(
            "PoolDelegatorsSyncJob:EnableOnStartup"
        );
        var poolDelegatorsDailyCronExpression = context.Configuration[
            "PoolDelegatorsSyncJob:DailyCronExpression"
        ];

        // Utxo Info Sync Job Configuration
        var enableUtxoInfoDailyTrigger = context.Configuration.GetValue<bool>(
            "UtxoInfoSyncJob:EnableDailyTrigger"
        );
        var enableUtxoInfoOnStartup = context.Configuration.GetValue<bool>(
            "UtxoInfoSyncJob:EnableOnStartup"
        );
        var utxoInfoDailyCronExpression = context.Configuration[
            "UtxoInfoSyncJob:DailyCronExpression"
        ];

        // Pool Updates Sync Job Configuration
        var enablePoolUpdatesDailyTrigger = context.Configuration.GetValue<bool>(
            "PoolUpdatesSyncJob:EnableDailyTrigger"
        );
        var enablePoolUpdatesOnStartup = context.Configuration.GetValue<bool>(
            "PoolUpdatesSyncJob:EnableOnStartup"
        );
        var poolUpdatesDailyCronExpression = context.Configuration[
            "PoolUpdatesSyncJob:DailyCronExpression"
        ];

        // Pool Information Sync Job Configuration
        var enablePoolInformationDailyTrigger = context.Configuration.GetValue<bool>(
            "PoolInformationSyncJob:EnableDailyTrigger"
        );
        var enablePoolInformationOnStartup = context.Configuration.GetValue<bool>(
            "PoolInformationSyncJob:EnableOnStartup"
        );
        var poolInformationDailyCronExpression = context.Configuration[
            "PoolInformationSyncJob:DailyCronExpression"
        ];

        // Quartz Configuration
        services.AddQuartz(q =>
        {
            // Distinct scheduler identity for clustering
            q.SchedulerId = "AUTO";
            q.SchedulerName = "pool-scheduler";

            // Use in-memory store (no persistence/clustering)
            // Default in-memory store is used when no persistent store is configured

            // Limit concurrent jobs to reduce DB load at startup - set to 1 for strict sequencing
            q.UseDefaultThreadPool(tp => tp.MaxConcurrency = 1);


            // Pool List Sync Job
            q.AddJob<PoolListSyncJob>(opts => opts.WithIdentity("PoolListSyncJob"));

            if (enablePoolListDailyTrigger)
            {
                q.AddTrigger(opts =>
                    opts.ForJob("PoolListSyncJob")
                        .WithIdentity("PoolListSyncJob-daily-trigger")
                        .WithCronSchedule(
                            poolListDailyCronExpression,
                            x => x.InTimeZone(TimeZoneInfo.Utc)
                        )
                );
            }

            // Startup trigger
            if (enablePoolListOnStartup)
            {
                q.AddTrigger(opts =>
                    opts.ForJob("PoolListSyncJob")
                        .WithIdentity("PoolListSyncJob-startup-trigger")
                        .StartNow()
                        .WithSimpleSchedule(x => x.WithMisfireHandlingInstructionFireNow())
                );
            }

            // Pool Voting Power History Sync Job
            q.AddJob<PoolVotingPowerHistorySyncJob>(opts => opts.WithIdentity("PoolVotingPowerHistorySyncJob"));

            if (enablePoolVotingPowerHistoryDailyTrigger)
            {
                q.AddTrigger(opts =>
                    opts.ForJob("PoolVotingPowerHistorySyncJob")
                        .WithIdentity("PoolVotingPowerHistorySyncJob-daily-trigger")
                        .WithCronSchedule(
                            poolVotingPowerHistoryDailyCronExpression,
                            x => x.InTimeZone(TimeZoneInfo.Utc)
                        )
                );
            }

            // Startup trigger for Pool Voting Power History
            if (enablePoolVotingPowerHistoryOnStartup)
            {
                q.AddTrigger(opts =>
                    opts.ForJob("PoolVotingPowerHistorySyncJob")
                        .WithIdentity("PoolVotingPowerHistorySyncJob-startup-trigger")
                        .StartAt(DateTimeOffset.UtcNow.AddSeconds(10)) // Delay 150 seconds after Metadata job
                        .WithSimpleSchedule(x => x.WithMisfireHandlingInstructionFireNow())
                );
            }

            // Pool Metadata Sync Job
            q.AddJob<PoolMetadataSyncJob>(opts => opts.WithIdentity("PoolMetadataSyncJob"));

            if (enablePoolMetadataDailyTrigger)
            {
                q.AddTrigger(opts =>
                    opts.ForJob("PoolMetadataSyncJob")
                        .WithIdentity("PoolMetadataSyncJob-daily-trigger")
                        .WithCronSchedule(
                            poolMetadataDailyCronExpression,
                            x => x.InTimeZone(TimeZoneInfo.Utc)
                        )
                );
            }

            // Startup trigger for Pool Metadata - let dependency coordination control timing
            if (enablePoolMetadataOnStartup)
            {
                q.AddTrigger(opts =>
                    opts.ForJob("PoolMetadataSyncJob")
                        .WithIdentity("PoolMetadataSyncJob-startup-trigger")
                        .StartNow()
                        .WithSimpleSchedule(x => x.WithMisfireHandlingInstructionFireNow())
                );
            }

            // Pool Stake Snapshot Sync Job
            q.AddJob<PoolStakeSnapshotSyncJob>(opts => opts.WithIdentity("PoolStakeSnapshotSyncJob"));

            if (enablePoolStakeSnapshotDailyTrigger)
            {
                q.AddTrigger(opts =>
                    opts.ForJob("PoolStakeSnapshotSyncJob")
                        .WithIdentity("PoolStakeSnapshotSyncJob-daily-trigger")
                        .WithCronSchedule(
                            poolStakeSnapshotDailyCronExpression,
                            x => x.InTimeZone(TimeZoneInfo.Utc)
                        )
                );
            }

            // Startup trigger for Pool Stake Snapshot
            if (enablePoolStakeSnapshotOnStartup)
            {
                q.AddTrigger(opts =>
                    opts.ForJob("PoolStakeSnapshotSyncJob")
                        .WithIdentity("PoolStakeSnapshotSyncJob-startup-trigger")
                        .StartNow()
                        .WithSimpleSchedule(x => x.WithMisfireHandlingInstructionFireNow())
                );
            }

            // Pool Delegators Sync Job
            q.AddJob<PoolDelegatorsSyncJob>(opts => opts.WithIdentity("PoolDelegatorsSyncJob"));

            if (enablePoolDelegatorsDailyTrigger)
            {
                q.AddTrigger(opts =>
                    opts.ForJob("PoolDelegatorsSyncJob")
                        .WithIdentity("PoolDelegatorsSyncJob-daily-trigger")
                        .WithCronSchedule(
                            poolDelegatorsDailyCronExpression,
                            x => x.InTimeZone(TimeZoneInfo.Utc)
                        )
                );
            }

            // Startup trigger for Pool Delegators
            if (enablePoolDelegatorsOnStartup)
            {
                q.AddTrigger(opts =>
                    opts.ForJob("PoolDelegatorsSyncJob")
                        .WithIdentity("PoolDelegatorsSyncJob-startup-trigger")
                        .StartNow()
                        .WithSimpleSchedule(x => x.WithMisfireHandlingInstructionFireNow())
                );
            }

            // Utxo Info Sync Job
            q.AddJob<UtxoInfoSyncJob>(opts => opts.WithIdentity("UtxoInfoSyncJob"));

            if (enableUtxoInfoDailyTrigger)
            {
                q.AddTrigger(opts =>
                    opts.ForJob("UtxoInfoSyncJob")
                        .WithIdentity("UtxoInfoSyncJob-daily-trigger")
                        .WithCronSchedule(
                            utxoInfoDailyCronExpression,
                            x => x.InTimeZone(TimeZoneInfo.Utc)
                        )
                );
            }

            // Startup trigger for Utxo Info
            if (enableUtxoInfoOnStartup)
            {
                q.AddTrigger(opts =>
                    opts.ForJob("UtxoInfoSyncJob")
                        .WithIdentity("UtxoInfoSyncJob-startup-trigger")
                        .StartAt(DateTimeOffset.UtcNow.AddSeconds(1))
                        .WithSimpleSchedule(x => x.WithMisfireHandlingInstructionFireNow())
                );
            }

            // Pool Updates Sync Job
            q.AddJob<PoolUpdatesSyncJob>(opts => opts.WithIdentity("PoolUpdatesSyncJob"));

            if (enablePoolUpdatesDailyTrigger)
            {
                q.AddTrigger(opts =>
                    opts.ForJob("PoolUpdatesSyncJob")
                        .WithIdentity("PoolUpdatesSyncJob-daily-trigger")
                        .WithCronSchedule(
                            poolUpdatesDailyCronExpression,
                            x => x.InTimeZone(TimeZoneInfo.Utc)
                        )
                );
            }

            // Startup trigger for Pool Updates
            if (enablePoolUpdatesOnStartup)
            {
                q.AddTrigger(opts =>
                    opts.ForJob("PoolUpdatesSyncJob")
                        .WithIdentity("PoolUpdatesSyncJob-startup-trigger")
                        .StartAt(DateTimeOffset.UtcNow.AddSeconds(1))
                        .WithSimpleSchedule(x => x.WithMisfireHandlingInstructionFireNow())
                );
            }

            // Pool Information Sync Job
            q.AddJob<PoolInformationSyncJob>(opts => opts.WithIdentity("PoolInformationSyncJob"));

            if (enablePoolInformationDailyTrigger)
            {
                q.AddTrigger(opts =>
                    opts.ForJob("PoolInformationSyncJob")
                        .WithIdentity("PoolInformationSyncJob-daily-trigger")
                        .WithCronSchedule(
                            poolInformationDailyCronExpression,
                            x => x.InTimeZone(TimeZoneInfo.Utc)
                        )
                );
            }

            // Startup trigger for Pool Information
            if (enablePoolInformationOnStartup)
            {
                q.AddTrigger(opts =>
                    opts.ForJob("PoolInformationSyncJob")
                        .WithIdentity("PoolInformationSyncJob-startup-trigger")
                        .StartAt(DateTimeOffset.UtcNow.AddSeconds(260))
                        .WithSimpleSchedule(x => x.WithMisfireHandlingInstructionFireNow())
                );
            }
        });
        services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

        // Register FailedBatchRetryService for handling failed batches
        services.AddSingleton<FailedBatchRetryService>();
    }
);

await builder.Build().RunAsync();
