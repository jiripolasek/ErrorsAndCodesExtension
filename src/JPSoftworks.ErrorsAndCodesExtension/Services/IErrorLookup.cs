// ------------------------------------------------------------
// 
// Copyright (c) Jiří Polášek. All rights reserved.
// 
// ------------------------------------------------------------

using System.Collections.Generic;

namespace JPSoftworks.ErrorsAndCodes.Services;

internal interface IErrorLookup
{
    IEnumerable<LookupResult> Lookup(string input);
}