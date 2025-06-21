// ------------------------------------------------------------
// 
// Copyright (c) Jiří Polášek. All rights reserved.
// 
// ------------------------------------------------------------

using JPSoftworks.ErrorsAndCodes.Helpers;
using JPSoftworks.ErrorsAndCodes.Pages;
using JPSoftworks.ErrorsAndCodes.Resources;
using JPSoftworks.ErrorsAndCodes.Services;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace JPSoftworks.ErrorsAndCodes;

public sealed partial class ErrorsAndCodesCommandsProvider : CommandProvider
{
    private readonly ICommandItem[] _commands;
    private readonly ErrorDataService _errorDataService;
    private readonly SettingsManager _settingsManager = new();

    public ErrorsAndCodesCommandsProvider()
    {
        this._errorDataService = new ErrorDataService();

        this.Id = "JPSoftworks.CmdPal.ErrorsAndCodes";
        this.DisplayName = Strings.ErrorsAndCodesPage_Title!;
        this.Icon = Icons.ErrorsCodesIconLarge;
        this.Settings = this._settingsManager.Settings;

        this._commands =
        [
            new CommandItem(new ErrorCodesListPage(this._errorDataService, _settingsManager))
            {
                Title = this.DisplayName,
                Subtitle = Strings.ErrorsAndCodesPage_Subtitle!,
                MoreCommands = [new CommandContextItem(this.Settings.SettingsPage!)]
            },
        ];
    }

    public override ICommandItem[] TopLevelCommands()
    {
        return this._commands;
    }
}