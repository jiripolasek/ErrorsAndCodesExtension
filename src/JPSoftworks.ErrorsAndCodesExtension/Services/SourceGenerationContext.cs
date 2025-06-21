// ------------------------------------------------------------
// 
// Copyright (c) Jiří Polášek. All rights reserved.
// 
// ------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using JPSoftworks.ErrorsAndCodes.Models;

namespace JPSoftworks.ErrorsAndCodes.Services;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(HeaderFile))]
[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global", Justification = "Instantiated implicitly by SourceGenerationContext.Default")]
internal partial class SourceGenerationContext : JsonSerializerContext;