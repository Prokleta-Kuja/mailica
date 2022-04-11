using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using mailica.Entities;
using mailica.Models;
using mailica.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace mailica.Pages.Credentials;
public partial class CredentialEdit : IDisposable
{
    [Inject] NavigationManager Nav { get; set; } = null!;
    [Inject] IDataProtectionProvider DpProvider { get; set; } = null!;
    [Inject] IDbContextFactory<AppDbContext> DbFactory { get; set; } = null!;
    [Inject] ToastService Toast { get; set; } = null!;
    [Parameter] public Guid AliasId { get; set; }
    AppDbContext _db = null!;
    CredentialCreateModel? _create;
    CredentialEditModel? _edit;
    Credential? _item;
    IDataProtector _protector = null!;
    readonly HashSet<string> _usernames = new(StringComparer.InvariantCultureIgnoreCase);
    Dictionary<string, string>? _errors;

    protected override async Task OnInitializedAsync()
    {
        _db = await DbFactory.CreateDbContextAsync();
    }
    public void Dispose() => _db?.Dispose();

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
            return;

        var credentials = await _db.Credentials.ToListAsync();

        if (AliasId == Guid.Empty)
            _create = new();
        else
        {
            _item = credentials.FirstOrDefault(a => a.AliasId == AliasId);

            if (_item != null)
                _edit = new(_item);
        }

        foreach (var cred in credentials)
            if (cred.AliasId != AliasId)
                _usernames.Add(cred.Username);

        _protector = DpProvider.CreateProtector(nameof(Credential));
        StateHasChanged();
    }
    void CancelClicked() => Nav.NavigateTo(C.Routes.Credentials);
    async Task SaveCreateClicked()
    {
        if (_create == null)
            return;

        _errors = _create.Validate(_usernames);
        if (_errors != null)
            return;

        var protectedPassword = _protector.Protect(_create.Password!);

        _item = new(_create.Type, _create.Host!, _create.Port!.Value, _create.Username!, protectedPassword);

        if (_create.Disabled)
            _item.Disabled = DateTime.UtcNow;
        else
        {
            // Validate and prevent save
        }

        _db.Credentials.Add(_item);
        await _db.SaveChangesAsync();

        _create = null;
        _edit = new(_item);
        Nav.NavigateTo(C.Routes.CredentialFor(_item.AliasId));
        StateHasChanged();
        Toast.ShowSuccess($"Credential {_item.Username} added");
    }
    async Task SaveEditClicked()
    {
        if (_edit == null || _item == null)
            return;

        _errors = _edit.Validate(_usernames);
        if (_errors != null)
            return;

        _item.Type = _edit.Type;
        _item.Host = _edit.Host!;
        _item.Port = _edit.Port!.Value;
        _item.Username = _edit.Username!;

        if (!string.IsNullOrWhiteSpace(_edit.NewPassword))
            _item.Password = _protector.Protect(_edit.NewPassword!);

        if (_edit.Disabled)
        {
            if (!_item.Disabled.HasValue)
                _item.Disabled = DateTime.UtcNow;
        }
        else
        {
            _item.Disabled = null;
            // Validate and prevent save
        }


        await _db.SaveChangesAsync();
        Toast.ShowSuccess($"Credential {_item.Username} saved");

        // TODO: reload affected jobs
    }
}