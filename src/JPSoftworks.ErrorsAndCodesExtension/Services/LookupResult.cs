// ------------------------------------------------------------
// 
// Copyright (c) Jiří Polášek. All rights reserved.
// 
// ------------------------------------------------------------

using JPSoftworks.ErrorsAndCodes.Services.WindowsErrors;

namespace JPSoftworks.ErrorsAndCodes.Services;

internal sealed record LookupResult(
    Interpretation Interpretation,
    ErrorCodeWithSource Entry,
    HeaderFilePriority Priority);