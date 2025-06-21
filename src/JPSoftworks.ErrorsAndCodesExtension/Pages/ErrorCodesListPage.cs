// ------------------------------------------------------------
// 
// Copyright (c) Jiří Polášek. All rights reserved.
// 
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using JPSoftworks.ErrorsAndCodes.Helpers;
using JPSoftworks.ErrorsAndCodes.Resources;
using JPSoftworks.ErrorsAndCodes.Services;

using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace JPSoftworks.ErrorsAndCodes.Pages;

internal sealed partial class ErrorCodesListPage : AsyncDynamicListPage, IDisposable
{
    private readonly ListItem _empty = new(new NoOpCommand())
    {
        Icon = Icons.ErrorsCodesIconLarge,
        Title = "Enter error or status code number to search"
    };

    private readonly ErrorDataService _errorDataService;

    private readonly ListItem _nothingFound = new(new NoOpCommand())
    {
        Icon = Icons.ErrorsCodesIconLarge,
        Title = "It's so empty here",
        Subtitle = "No result matched the input query"
    };

    private readonly SettingsManager _settingsManager;

    public ErrorCodesListPage(ErrorDataService errorDataService, SettingsManager settingsManager)
    {
        ArgumentNullException.ThrowIfNull(errorDataService);
        ArgumentNullException.ThrowIfNull(settingsManager);

        this._errorDataService = errorDataService;
        this._settingsManager = settingsManager;
        this.Icon = Icons.ErrorsCodesIcon;
        this.Title = Strings.ErrorsAndCodesPage_Title!;
        this.Name = Strings.Command_Open!;
        this.ShowDetails = this._settingsManager.ShowDetails;

        this._settingsManager.Settings.SettingsChanged += this.SettingsOnSettingsChanged;
    }

    private void SettingsOnSettingsChanged(object sender, Settings args)
    {
        this.ShowDetails = this._settingsManager.ShowDetails;
    }


    protected override Task<IListItem[]> LoadInitialItemsAsync(CancellationToken cancellationToken)
    {
        this.EmptyContent = this._empty;
        return Task.FromResult(Array.Empty<IListItem>());
    }

    protected override async Task<IListItem[]> SearchItemsAsync(string searchText, CancellationToken cancellationToken)
    {
        var lookup = await this._errorDataService.GetErrorLookup();

        var results = new List<IListItem>();

        foreach (var group in lookup.Lookup(searchText).GroupBy(static t => t.Interpretation))
        {
            results.Add(new ListItem(new NoOpCommand())
            {
                Title = group.Key.Description,
                Subtitle = (group.Key.Details != null ? group.Key.Details + Environment.NewLine : "")  + group.Count() + " results",
                Icon = Icons.MessageGroup
            });

            results.AddRange(group.Take(64).Select(static lookupResult =>
                new ErrorListItem(lookupResult.Interpretation.Description, lookupResult.Entry)));
        }

        this.EmptyContent = this._nothingFound;

        return [.. results];
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            this._settingsManager.Settings.SettingsChanged -= this.SettingsOnSettingsChanged;
        }

        base.Dispose(disposing);
    }
}