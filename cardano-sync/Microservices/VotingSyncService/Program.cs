using Microsoft.EntityFrameworkCore;
using Quartz;
using Serilog;
using SharedLibrary.DatabaseContext;
using VotingSyncService.Jobs;
using VotingSyncService.Services;

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



        // Register DatabaseSyncService for backup database operations
        services.AddSingleton<DatabaseSyncService>();

        // Vote List Sync Job Configuration
        var enableVoteListDailyTrigger = context.Configuration.GetValue<bool>(
            "VoteListSyncJob:EnableDailyTrigger"
        );
        var enableVoteListOnStartup = context.Configuration.GetValue<bool>(
            "VoteListSyncJob:EnableOnStartup"
        );
        var voteListDailyCronExpression = context.Configuration[
            "VoteListSyncJob:DailyCronExpression"
        ];



        // Voter Proposal List Sync Job Configuration
        var enableVoterProposalListDailyTrigger = context.Configuration.GetValue<bool>(
            "VoterProposalListSyncJob:EnableDailyTrigger"
        );
        var enableVoterProposalListOnStartup = context.Configuration.GetValue<bool>(
            "VoterProposalListSyncJob:EnableOnStartup"
        );
        var voterProposalListDailyCronExpression = context.Configuration[
            "VoterProposalListSyncJob:DailyCronExpression"
        ];

        // Quartz Configuration
        services.AddQuartz(q =>
        {
            // Distinct scheduler identity for clustering
            q.SchedulerId = "AUTO";
            q.SchedulerName = "voting-scheduler";

            // Use in-memory store (no persistence/clustering)
            // Default in-memory store is used when no persistent store is configured


            // Vote List Sync Job
            q.AddJob<VoteListSyncJob>(opts => opts.WithIdentity("VoteListSyncJob"));

            if (enableVoteListDailyTrigger)
            {
                q.AddTrigger(opts =>
                    opts.ForJob("VoteListSyncJob")
                        .WithIdentity("VoteListSyncJob-daily-trigger")
                        .WithCronSchedule(
                            voteListDailyCronExpression,
                            x => x.InTimeZone(TimeZoneInfo.Utc)
                        )
                );
            }

            // Startup trigger (VoteList first)
            if (enableVoteListOnStartup)
            {
                q.AddTrigger(opts =>
                    opts.ForJob("VoteListSyncJob")
                        .WithIdentity("VoteListSyncJob-startup-trigger")
                        .StartNow()
                        .WithSimpleSchedule(x => x.WithMisfireHandlingInstructionFireNow())
                );
            }

            // Voter Proposal List Sync Job
            q.AddJob<VoterProposalListSyncJob>(opts => opts.WithIdentity("VoterProposalListSyncJob"));

            if (enableVoterProposalListDailyTrigger)
            {
                q.AddTrigger(opts =>
                    opts.ForJob("VoterProposalListSyncJob")
                        .WithIdentity("VoterProposalListSyncJob-daily-trigger")
                        .WithCronSchedule(
                            voterProposalListDailyCronExpression,
                            x => x.InTimeZone(TimeZoneInfo.Utc)
                        )
                );
            }

            // Startup trigger for VoterProposalListSyncJob (staggered by +30s)
            if (enableVoterProposalListOnStartup)
            {
                q.AddTrigger(opts =>
                    opts.ForJob("VoterProposalListSyncJob")
                        .WithIdentity("VoterProposalListSyncJob-startup-trigger")
                        .StartAt(DateTimeOffset.UtcNow.AddSeconds(30))
                        .WithSimpleSchedule(x => x.WithMisfireHandlingInstructionFireNow())
                );
            }
        });
        services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);
    }
);

await builder.Build().RunAsync();
