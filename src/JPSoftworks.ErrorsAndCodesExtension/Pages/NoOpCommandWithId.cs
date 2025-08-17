// ------------------------------------------------------------
//
// Copyright (c) Jiří Polášek. All rights reserved.
//
// ------------------------------------------------------------

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