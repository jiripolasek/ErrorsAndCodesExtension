// ------------------------------------------------------------
// 
// Copyright (c) Jiří Polášek. All rights reserved.
// 
// ------------------------------------------------------------

using Microsoft.CommandPalette.Extensions.Toolkit;

namespace JPSoftworks.ErrorsAndCodes.Helpers;

internal static class Icons
{
    public static IconInfo Message { get; } = new IconInfo("\uE8BD");

    public static IconInfo MessageGroup { get; } = IconHelpers.FromRelativePath("Assets\\Icons\\BookOfErrors20.png");

    public static IconInfo ErrorsCodesIcon { get; } = IconHelpers.FromRelativePath("Assets\\Square44x44Logo.altform-unplated_targetsize-20.png");

    public static IconInfo ErrorsCodesIconLarge { get; } = IconHelpers.FromRelativePath("Assets\\Square44x44Logo.altform-unplated_targetsize-48.png");
}