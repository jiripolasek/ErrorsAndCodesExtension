// ------------------------------------------------------------
// 
// Copyright (c) Jiří Polášek. All rights reserved.
// 
// ------------------------------------------------------------

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace JPSoftworks.ErrorsAndCodes.Models;

internal sealed class HeaderFile
{
    public required string HeaderFileName { get; init; }

    public required List<ErrorCodeDto> ErrorCodes { get; init; } = [];

    public required List<FacilityDto> Facilities { get; init; } = [];
}

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
internal sealed class FacilityDto(string name, int code)
{
    public required string Name { get; init; } = name;
    public required int Code { get; init; } = code;
}

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
internal sealed record ErrorCodeDto
{
    public string Id { get; init; }
    public string Message { get; init; }
    public int DecimalCode { get; init; } // Decimal representation of the error code; value is equivalent to HexCode
    public string HexCode { get; init; } // Hexadecimal representation of the error code; value is equivalent to DecimalCode

    [JsonConverter(typeof(JsonStringEnumConverter<CodeType>))]
    public CodeType Type { get; init; }

    public ErrorCodeDto(string id, string message, int decimalCode, string hexCode, CodeType type)
    {
        this.Id = id;
        this.Message = message;
        this.DecimalCode = decimalCode;
        this.HexCode = hexCode;
        this.Type = type;
    }
}

internal enum CodeType
{
    Unknown,
    HResult,
    NtStatus,
    Win32Error,
    PlainNumber
}