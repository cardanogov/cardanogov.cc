using Microsoft.EntityFrameworkCore;
using ProposalSyncService.Jobs;
using ProposalSyncService.Services;
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



    // Register DatabaseSyncService for backup database operations
    services.AddSingleton<DatabaseSyncService>();

    // Job Configuration
    var enableProposalDailyTrigger = context.Configuration.GetValue<bool>("ProposalSyncJob:EnableDailyTrigger");
    var enableProposalOnStartup = context.Configuration.GetValue<bool>("ProposalSyncJob:EnableOnStartup");
    var proposalDailyCronExpression = context.Configuration["ProposalSyncJob:DailyCronExpression"];

    var enableVotesDailyTrigger = context.Configuration.GetValue<bool>("ProposalVotesSyncJob:EnableDailyTrigger");
    var enableVotesOnStartup = context.Configuration.GetValue<bool>("ProposalVotesSyncJob:EnableOnStartup");
    var votesDailyCronExpression = context.Configuration["ProposalVotesSyncJob:DailyCronExpression"];

    var enableVotingSummaryDailyTrigger = context.Configuration.GetValue<bool>("ProposalVotingSummaryJob:EnableDailyTrigger");
    var enableVotingSummaryOnStartup = context.Configuration.GetValue<bool>("ProposalVotingSummaryJob:EnableOnStartup");
    var votingSummaryDailyCronExpression = context.Configuration["ProposalVotingSummaryJob:DailyCronExpression"];

    // Quartz Configuration
    services.AddQuartz(q =>
    {
        // Distinct scheduler identity for clustering
        q.SchedulerId = "AUTO";
        q.SchedulerName = "proposal-scheduler";

        // Use in-memory store (no persistence/clustering)
        // Default in-memory store is used when no persistent store is configured

        // Register Jobs
        q.AddJob<ProposalSyncJob>(opts => opts.WithIdentity("ProposalSyncJob"));
        q.AddJob<ProposalVotingSummaryJob>(opts => opts.WithIdentity("ProposalVotingSummaryJob"));
        q.AddJob<ProposalVotesSyncJob>(opts => opts.WithIdentity("ProposalVotesSyncJob"));

        // Daily Triggers
        if (enableProposalDailyTrigger)
        {
            q.AddTrigger(opts =>
                opts.ForJob("ProposalSyncJob")
                    .WithIdentity("ProposalSyncJob-daily-trigger")
                    .WithCronSchedule(
                        proposalDailyCronExpression,
                        x => x.InTimeZone(TimeZoneInfo.Utc)
                    )
            );
        }

        if (enableVotingSummaryDailyTrigger)
        {
            q.AddTrigger(opts =>
                opts.ForJob("ProposalVotingSummaryJob")
                    .WithIdentity("ProposalVotingSummaryJob-daily-trigger")
                    .WithCronSchedule(
                        votingSummaryDailyCronExpression,
                        x => x.InTimeZone(TimeZoneInfo.Utc)
                    )
            );
        }

        if (enableVotesDailyTrigger)
        {
            q.AddTrigger(opts =>
                opts.ForJob("ProposalVotesSyncJob")
                    .WithIdentity("ProposalVotesSyncJob-daily-trigger")
                    .WithCronSchedule(
                        votesDailyCronExpression,
                        x => x.InTimeZone(TimeZoneInfo.Utc)
                    )
            );
        }

        // Startup Triggers
        if (enableProposalOnStartup)
        {
            q.AddTrigger(opts =>
                opts.ForJob("ProposalSyncJob")
                    .WithIdentity("ProposalSyncJob-startup-trigger")
                    .StartNow()
                    .WithSimpleSchedule(x => x.WithMisfireHandlingInstructionFireNow())
            );
        }

        if (enableVotingSummaryOnStartup)
        {
            q.AddTrigger(opts =>
                opts.ForJob("ProposalVotingSummaryJob")
                    .WithIdentity("ProposalVotingSummaryJob-startup-trigger")
                   .StartAt(DateTime.UtcNow.AddSeconds(30))
                    .WithSimpleSchedule(x => x.WithMisfireHandlingInstructionFireNow())
            );
        }

        if (enableVotesOnStartup)
        {
            q.AddTrigger(opts =>
                opts.ForJob("ProposalVotesSyncJob")
                    .WithIdentity("ProposalVotesSyncJob-startup-trigger")
                    .StartAt(DateTime.UtcNow.AddSeconds(60))
                    .WithSimpleSchedule(x => x.WithMisfireHandlingInstructionFireNow())
                );
        }
    });
    services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);
});

await builder.Build().RunAsync();
