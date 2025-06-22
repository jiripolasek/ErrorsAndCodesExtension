// ------------------------------------------------------------
// 
// Copyright (c) Jiří Polášek. All rights reserved.
// 
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using JPSoftworks.ErrorsAndCodes.Helpers;
using JPSoftworks.ErrorsAndCodes.Models;
using JPSoftworks.ErrorsAndCodes.Services.WindowsErrors;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace JPSoftworks.ErrorsAndCodes.Pages;

internal sealed partial class ErrorListItem : ListItem
{
    public ErrorListItem(string explanation, ErrorCodeWithSource? entry)
    {
        if (entry != null)
        {
            this.Title = entry.ErrorCode.Id;
            this.Subtitle = BuildSubtitle(entry);
            this.Details = BuildDetails(entry);
            this.Command = new ErrorDetailsPage(entry);
            this.MoreCommands = [.. BuildCommands(entry)];
        }
        else
        {
            this.Title = "Unknown Error Symbol or Code";
            this.Subtitle = explanation;
        }

        this.Icon = Icons.Message;
    }

    private static IEnumerable<IContextItem> BuildCommands(ErrorCodeWithSource entry)
    {
        yield return new CommandContextItem(new CopyTextCommand(entry.ErrorCode.Id) { Name = "Copy symbolic name" });
        yield return new CommandContextItem(new CopyTextCommand(entry.ErrorCode.HexCode) { Name = "Copy hex value" });
        yield return new CommandContextItem(new CopyTextCommand(entry.ErrorCode.DecimalCode.ToString(CultureInfo.InvariantCulture)) { Name = "Copy decimal value" });
        yield return new CommandContextItem(new CopyTextCommand(entry.ErrorCode.Message) { Name = "Copy message" });
    }

    private static string BuildSubtitle(ErrorCodeWithSource entry)
    {
        var sb = new StringBuilder();
        sb.Append("• ").Append(entry.ErrorCode.HexCode).Append(" / ").Append(entry.ErrorCode.DecimalCode).AppendLine()
          .Append("• ").Append(entry.SourceFile);

        if (!string.IsNullOrWhiteSpace(entry.ErrorCode.Message))
        {
            sb.AppendLine()
              .Append("• ").Append(entry.ErrorCode.Message);
        }

        return sb.ToString();
    }

    private static Details BuildDetails(ErrorCodeWithSource entry)
    {
        return new Details
        {
            Title = entry.ErrorCode.Id,
            Body = MarkdownHelper.EscapeMarkdown(entry.ErrorCode.Message),
            Metadata =
            [
                new DetailsElement { Key = "Source", Data = new DetailsLink { Text = entry.SourceFile } },
                new DetailsElement { Key = "Value (hex)", Data = new DetailsLink { Text = entry.ErrorCode.HexCode } },
                new DetailsElement
                {
                    Key = "Value (decimal)",
                    Data = new DetailsLink
                    {
                        Text = entry.ErrorCode.DecimalCode.ToString(CultureInfo.InvariantCulture)
                    }
                }
            ]
        };
    }
}