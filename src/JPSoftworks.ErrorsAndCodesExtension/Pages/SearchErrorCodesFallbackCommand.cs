// ------------------------------------------------------------
//
// Copyright (c) Jiří Polášek. All rights reserved.
//
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using JPSoftworks.ErrorsAndCodes.Helpers;
using JPSoftworks.ErrorsAndCodes.Services;
using JPSoftworks.ErrorsAndCodes.Services.WindowsErrors;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace JPSoftworks.ErrorsAndCodes.Pages;

internal sealed partial class NoOpCommandWithId : NoOpCommand
{
    public override string Id { get; protected set; }

    public NoOpCommandWithId(string id, IconInfo icon)
    {
        this.Id = id;
        this.Icon = icon;
    }
}

internal class SearchErrorCodesFallbackCommand : FallbackCommandItem
{
    private static readonly NoOpCommandWithId BaseCommandWithId = new("com.jpsoftworks.cmdpal.errorsandcodes.index", Icons.ErrorsCodesIcon);

    private readonly ErrorDataService _errorDataService;
    private readonly SettingsManager _settingsManager;

    public SearchErrorCodesFallbackCommand(ErrorDataService errorDataService, SettingsManager settingsManager) : base(BaseCommandWithId, "Find error by code or message")
    {
        ArgumentNullException.ThrowIfNull(errorDataService);
        ArgumentNullException.ThrowIfNull(settingsManager);

        this._errorDataService = errorDataService;
        this._settingsManager = settingsManager;
    }

    public override void UpdateQuery(string query)
    {
        if (string.IsNullOrWhiteSpace(query) || !this._errorDataService.IsReady)
        {
            this.Command = new NoOpCommand();
            this.Title = string.Empty;
            this.Subtitle = string.Empty;
            this.Icon = null;
            this.MoreCommands = [];
            return;
        }

        query = query.Trim();

        // this is quite fast, so we can run it every time (> 0.01 s)
        // since we passed IsReady check above, we can safely call Result here
        var q = this._errorDataService.GetErrorLookup().Result.Lookup(query).ToList();

        if (q.Count > 0)
        {
            this.Command = new ErrorCodesListPage(this._errorDataService, this._settingsManager) { SearchText = query };
            this.Title = $"""Search for "{query}" in errors and codes""";
            this.Subtitle = $"""
                             Browse {q.Count} matches for error or status code "{query}" — first: {q[0].Entry.ErrorCode.Id}
                             """;
            this.Icon = Icons.ErrorsCodesIcon;
            this.MoreCommands = [.. BuildCommands(q[0].Entry)];
            return;
        }

        this.Command = new NoOpCommand();
        this.Title = string.Empty;
        this.Subtitle = string.Empty;
        this.Icon = null;
        this.MoreCommands = [];
    }

    private static IEnumerable<IContextItem> BuildCommands(ErrorCodeWithSource entry)
    {
        yield return new CommandContextItem(new CopyTextCommand(entry.ErrorCode.Id) { Name = "Copy symbolic name" });
        yield return new CommandContextItem(new CopyTextCommand(entry.ErrorCode.HexCode) { Name = "Copy hex value" });
        yield return new CommandContextItem(new CopyTextCommand(entry.ErrorCode.DecimalCode.ToString(CultureInfo.InvariantCulture)) { Name = "Copy decimal value" });
        yield return new CommandContextItem(new CopyTextCommand(entry.ErrorCode.Message) { Name = "Copy message" });
    }
}