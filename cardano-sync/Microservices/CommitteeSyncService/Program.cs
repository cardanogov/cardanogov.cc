using CommitteeSyncService.Jobs;
using CommitteeSyncService.Services;
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

builder.ConfigureServices((context, services) =>
{
    // Database Context - Entity Framework will use Npgsql provider based on connection string
    var connectionString = context.Configuration.GetConnectionString("DefaultConnection");
    services.AddDbContext<CardanoDbContext>(options => options.UseNpgsql(connectionString));

    // Register Database Sync Service for reading from backup databases


    services.AddScoped<DatabaseSyncService>();

    // Committee Sync Job Configuration
    var enableDailyTrigger = context.Configuration.GetValue<bool>("CommitteeSyncJob:EnableDailyTrigger");
    var enableOnStartup = context.Configuration.GetValue<bool>("CommitteeSyncJob:EnableOnStartup");
    var dailyCronExpression = context.Configuration["CommitteeSyncJob:DailyCronExpression"];

    // Committee Votes Sync Job Configuration
    var enableVotesDailyTrigger = context.Configuration.GetValue<bool>("CommitteeVotesSyncJob:EnableDailyTrigger");
    var enableVotesOnStartup = context.Configuration.GetValue<bool>("CommitteeVotesSyncJob:EnableOnStartup");
    var votesDailyCronExpression = context.Configuration["CommitteeVotesSyncJob:DailyCronExpression"];


    // Treasury Withdrawals Sync Job Configuration
    var enableTreasuryDailyTrigger = context.Configuration.GetValue<bool>("TreasuryWithdrawalsSyncJob:EnableDailyTrigger");
    var enableTreasuryOnStartup = context.Configuration.GetValue<bool>("TreasuryWithdrawalsSyncJob:EnableOnStartup");
    var treasuryDailyCronExpression = context.Configuration["TreasuryWithdrawalsSyncJob:DailyCronExpression"];

    // Totals Sync Job Configuration
    var enableTotalsDailyTrigger = context.Configuration.GetValue<bool>("TotalsSyncJob:EnableDailyTrigger");
    var enableTotalsOnStartup = context.Configuration.GetValue<bool>("TotalsSyncJob:EnableOnStartup");
    var totalsDailyCronExpression = context.Configuration["TotalsSyncJob:DailyCronExpression"];

    // Quartz Configuration
    services.AddQuartz(q =>
    {

        // Use distinct scheduler name per service to isolate clusters
        q.SchedulerId = "AUTO";
        q.SchedulerName = "committee-scheduler";

        // Use in-memory store (no clustering/persistence)
        // Default in-memory store is used when no persistent store is configured

        // Committee Sync Job
        q.AddJob<CommitteeSyncJob>(opts => opts.WithIdentity("CommitteeSyncJob"));

        // Committee Votes Sync Job
        q.AddJob<CommitteeVotesSyncJob>(opts => opts.WithIdentity("CommitteeVotesSyncJob"));

        // Treasury Withdrawals Sync Job
        q.AddJob<TreasuryWithdrawalsSyncJob>(opts => opts.WithIdentity("TreasuryWithdrawalsSyncJob"));


        // Totals Sync Job
        q.AddJob<TotalsSyncJob>(opts => opts.WithIdentity("TotalsSyncJob"));

        // Daily triggers
        if (enableDailyTrigger)
        {
            q.AddTrigger(opts => opts
                .ForJob("CommitteeSyncJob")
                .WithIdentity("CommitteeSyncJob-daily-trigger")
                .WithCronSchedule(dailyCronExpression, x => x.InTimeZone(TimeZoneInfo.Utc))
            );
        }

        if (enableVotesDailyTrigger)
        {
            q.AddTrigger(opts => opts
                .ForJob("CommitteeVotesSyncJob")
                .WithIdentity("CommitteeVotesSyncJob-daily-trigger")
                .WithCronSchedule(votesDailyCronExpression, x => x.InTimeZone(TimeZoneInfo.Utc))
            );
        }

        if (enableTreasuryDailyTrigger)
        {
            q.AddTrigger(opts => opts
                .ForJob("TreasuryWithdrawalsSyncJob")
                .WithIdentity("TreasuryWithdrawalsSyncJob-daily-trigger")
                .WithCronSchedule(treasuryDailyCronExpression, x => x.InTimeZone(TimeZoneInfo.Utc))
            );
        }

        if (enableTotalsDailyTrigger)
        {
            q.AddTrigger(opts => opts
                .ForJob("TotalsSyncJob")
                .WithIdentity("TotalsSyncJob-daily-trigger")
                .WithCronSchedule(totalsDailyCronExpression, x => x.InTimeZone(TimeZoneInfo.Utc))
            );
        }

        // Startup triggers (staggered for dependency order)
        if (enableOnStartup)
        {
            q.AddTrigger(opts => opts
                .ForJob("CommitteeSyncJob")
                .WithIdentity("CommitteeSyncJob-startup-trigger")
                .StartNow()
                .WithSimpleSchedule(x => x.WithMisfireHandlingInstructionFireNow())
            );
        }

        if (enableVotesOnStartup)
        {
            q.AddTrigger(opts => opts
                .ForJob("CommitteeVotesSyncJob")
                .WithIdentity("CommitteeVotesSyncJob-startup-trigger")
                .StartAt(DateTimeOffset.UtcNow.AddSeconds(30))
                .WithSimpleSchedule(x => x.WithMisfireHandlingInstructionFireNow())
            );
        }

        if (enableTreasuryOnStartup)
        {
            q.AddTrigger(opts => opts
                .ForJob("TreasuryWithdrawalsSyncJob")
                .WithIdentity("TreasuryWithdrawalsSyncJob-startup-trigger")
                .StartAt(DateTimeOffset.UtcNow.AddSeconds(60))
                .WithSimpleSchedule(x => x.WithMisfireHandlingInstructionFireNow())
            );
        }

        if (enableTotalsOnStartup)
        {
            q.AddTrigger(opts => opts
                .ForJob("TotalsSyncJob")
                .WithIdentity("TotalsSyncJob-startup-trigger")
                .StartAt(DateTimeOffset.UtcNow.AddSeconds(90))
                .WithSimpleSchedule(x => x.WithMisfireHandlingInstructionFireNow())
            );
        }
    });
    services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);
});

await builder.Build().RunAsync();
