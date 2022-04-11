using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using mailica.Entities;
using mailica.QueryParams;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace mailica.Pages.Credentials;
public partial class CredentialList : IDisposable
{
    [Inject] IDbContextFactory<AppDbContext> DbFactory { get; set; } = null!;
    [Inject] NavigationManager Nav { get; set; } = null!;

    AppDbContext _db = null!;
    List<Credential> _items = new();
    readonly Params _params = new(CredentialCol.Username, true);

    protected override async Task OnInitializedAsync()
    {
        _db = await DbFactory.CreateDbContextAsync();
    }
    public void Dispose() => _db?.Dispose();
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
            return;

        var uri = new Uri(Nav.Uri);
        if (QueryHelpers.ParseQuery(uri.Query).TryGetValue("search", out var terms) && terms.Any())
            _params.SetSearchTerm(terms.First().ToLower());

        await RefreshItemsAsync();
    }
    async Task Search(string? term)
    {
        if (string.IsNullOrWhiteSpace(term))
            _params.ClearSearchTerm();
        else
            _params.SetSearchTerm(term.ToLower());

        await RefreshItemsAsync();
    }

    async Task RefreshItemsAsync()
    {
        var query = _db.Credentials.AsQueryable();

        if (!string.IsNullOrWhiteSpace(_params.SearchTerm))
        {
            var term = _params.SearchTerm.ToLowerInvariant();
            query = query.Where(t =>
                t.Host.Contains(term) ||
                t.Username.Contains(term)
            );
        }

        switch (_params.OrderBy)
        {
            case CredentialCol.IsValid:
                Expression<Func<Credential, object>> isValid = t => t.IsValid;
                query = _params.OrderDesc ? query.OrderByDescending(isValid) : query.OrderBy(isValid);
                break;
            case CredentialCol.Host:
                Expression<Func<Credential, object>> host = t => t.Host;
                query = _params.OrderDesc ? query.OrderByDescending(host) : query.OrderBy(host);
                break;
            case CredentialCol.Username:
                Expression<Func<Credential, object>> username = t => t.Username;
                query = _params.OrderDesc ? query.OrderByDescending(username) : query.OrderBy(username);
                break;
            case CredentialCol.Type:
                Expression<Func<Credential, object>> type = t => t.Type;
                query = _params.OrderDesc ? query.OrderByDescending(type) : query.OrderBy(type);
                break;
            case CredentialCol.Disabled:
                Expression<Func<Credential, object?>> disabled = t => t.Disabled;
                query = _params.OrderDesc ? query.OrderByDescending(disabled) : query.OrderBy(disabled);
                break;
            default: break;
        }

        _items = await query.Skip(_params.Skip).ToListAsync();
        StateHasChanged();
    }
}