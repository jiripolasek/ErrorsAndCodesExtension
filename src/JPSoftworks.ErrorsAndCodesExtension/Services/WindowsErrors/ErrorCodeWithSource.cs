// ------------------------------------------------------------
// 
// Copyright (c) Jiří Polášek. All rights reserved.
// 
// ------------------------------------------------------------

using JPSoftworks.ErrorsAndCodes.Models;

namespace JPSoftworks.ErrorsAndCodes.Services.WindowsErrors;

internal record ErrorCodeWithSource(ErrorCodeDto ErrorCode, string SourceFile);