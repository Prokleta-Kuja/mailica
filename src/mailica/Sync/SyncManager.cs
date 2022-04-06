using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using authica.Entities;
using mailica.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace mailica.Sync;

public class SyncManager
{
    readonly Dictionary<int, SyncInstance> _instances;
    readonly IDbContextFactory<AppDbContext> _dbFactory;
    readonly IServiceProvider _sp;
    public SyncManager(IServiceProvider sp)
    {
        _instances = new();
        _sp = sp;
        _dbFactory = sp.GetRequiredService<IDbContextFactory<AppDbContext>>();
    }
    public async Task RestartAsync()
    {
        await StopAsync();

        using var db = await _dbFactory.CreateDbContextAsync();
        var jobs = await db.Jobs
            .AsNoTracking()
            .Where(j => !j.Disabled.HasValue)
            .ToDictionaryAsync(j => j.JobId);

        var rules = await db.IncomingRules
            .AsNoTracking()
            .Where(ir => !ir.Disabled.HasValue)
            .Include(ir => ir.IncomingRuleCredentials)
            .ToListAsync();

        var credentials = await db.Credentials
            .AsNoTracking()
            .Include(c => c.IncomingRuleCredentials)
            .Where(c => !c.Disabled.HasValue)
            .ToDictionaryAsync(c => c.CredentialId);

        var jobRulesDict = new Dictionary<int, List<IncomingRule>>();
        foreach (var rule in rules)
            if (jobRulesDict.ContainsKey(rule.JobId))
                jobRulesDict[rule.JobId].Add(rule);
            else
                jobRulesDict.Add(rule.JobId, new() { rule });

        foreach (var job in jobs)
        {
            var syncFrom = credentials[job.Value.CredentialId];
            var syncRules = new List<SyncRule>();
            if (jobRulesDict.TryGetValue(job.Key, out var jobRules))
                foreach (var rule in jobRules)
                {
                    var ruleDestinations = new List<SyncDestination>(rule.IncomingRuleCredentials.Count);
                    foreach (var ruleCredential in rule.IncomingRuleCredentials)
                        ruleDestinations.Add(new(credentials[ruleCredential.CredentialId], ruleCredential.Folder));

                    var syncRule = new SyncRule(rule.Filter, ruleDestinations);
                    syncRules.Add(syncRule);
                }

            // var syncInstance = new SyncInstance(job.Key, syncFrom, syncRules);
            var syncInstance = ActivatorUtilities.CreateInstance<SyncInstance>(_sp, job.Key, syncFrom, syncRules);
            if (job.Value.CatchAllCredentialId.HasValue && credentials.TryGetValue(job.Value.CatchAllCredentialId.Value, out var catchAll))
                syncInstance.ToCatchAll = new(catchAll, job.Value.CatchAllFolder);

            _instances.Add(job.Key, syncInstance);
        }

        foreach (var instance in _instances)
        {
            // TODO: start instance
        }
    }
    public async Task StopAsync()
    {
        if (_instances.Any())
        {
            foreach (var instance in _instances)
            {
                // TODO: stop instance
            }
        }
        // TODO: make sure all stoped
        _instances.Clear();
    }
}