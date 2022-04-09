using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using authica.Entities;
using mailica.Entities;
using mailica.Enums;
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

        var rand = new Random();
        foreach (var job in jobs)
        {
            var syncFrom = credentials[job.Value.CredentialId];
            var syncRules = new List<SyncRule>();
            if (jobRulesDict.TryGetValue(job.Key, out var jobRules))
                foreach (var rule in jobRules)
                {
                    var ruleDestinations = new List<SyncDestination>(rule.IncomingRuleCredentials.Count);
                    foreach (var ruleCredential in rule.IncomingRuleCredentials)
                        if (credentials.ContainsKey(ruleCredential.CredentialId))
                            ruleDestinations.Add(new(credentials[ruleCredential.CredentialId], ruleCredential.Folder));

                    var syncRule = new SyncRule(rule.Filter, ruleDestinations);
                    syncRules.Add(syncRule);
                }

            var syncInstance = ActivatorUtilities.CreateInstance<SyncInstance>(_sp, job.Key, syncFrom, syncRules);
            if (job.Value.CatchAllCredentialId.HasValue && credentials.TryGetValue(job.Value.CatchAllCredentialId.Value, out var catchAll))
                syncInstance.ToCatchAll = new(catchAll, job.Value.CatchAllFolder);

            if (_instances.TryAdd(job.Key, syncInstance))
                // _instances[job.Key].Start(job.Value.SyncEvery, rand.Next(30_000));
                if (job.Key == 1) // TODO: remove after test
                    _instances[job.Key].Start(job.Value.SyncEvery, 1);
        }
    }
    public async Task StopAsync()
    {
        if (_instances.Any())
            foreach (var instance in _instances)
                instance.Value.Stop();

        var waitFor = 10;
        while (!_instances.Values.All(i => i.Status == SyncStatus.Stopped))
        {
            --waitFor;
            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        _instances.Clear();
    }
}