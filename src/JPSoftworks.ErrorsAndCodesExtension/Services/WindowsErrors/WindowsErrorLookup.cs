// ------------------------------------------------------------
// 
// Copyright (c) Jiří Polášek. All rights reserved.
// 
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using JPSoftworks.ErrorsAndCodes.Models;

namespace JPSoftworks.ErrorsAndCodes.Services.WindowsErrors;

/// <summary>
/// Provides efficient lookup of Windows error codes from multiple header files with priority support
/// </summary>
internal sealed partial class WindowsErrorLookup : IErrorLookup
{
    private static readonly Regex HexPattern = HexRegex();

    private static readonly Dictionary<string, HeaderFilePriority> DefaultPriorities
        = new(StringComparer.OrdinalIgnoreCase)
        {
            // High priority - primary Windows error sources
            ["winerror.h"] = HeaderFilePriority.High,
            ["ntstatus.h"] = HeaderFilePriority.High,

            // Low priority - deprecated or legacy files
            ["d3d9.h"] = HeaderFilePriority.Low,
            ["d3d8.h"] = HeaderFilePriority.Low,
            ["ddraw.h"] = HeaderFilePriority.Low,
            ["dinput.h"] = HeaderFilePriority.Low,

            // Everything else gets Normal priority by default
        };

    private readonly List<ErrorCodeWithSource> _all;
    private readonly Dictionary<long, List<ErrorCodeWithSource>> _byNumericCode;
    private readonly Dictionary<string, List<ErrorCodeWithSource>> _bySymbol;
    private readonly Dictionary<string, Dictionary<int, string>> _facilitiesByFile;

    private readonly Dictionary<string, HeaderFilePriority> _filePriorities;
    private readonly Dictionary<int, string> _globalFacilities;

    private readonly HResultProcessor _hresultProcessor;

    private readonly Dictionary<string, HashSet<ErrorCodeWithSource>> _symbolIndex;

    /// <summary>
    /// Initializes a new instance of the ErrorLookup class
    /// </summary>
    /// <param name="headerFiles">Collection of header files containing error definitions</param>
    /// <param name="customPriorities">Optional custom priority mappings for header files. If null, default priorities are used.</param>
    public WindowsErrorLookup(
        IEnumerable<HeaderFile> headerFiles,
        Dictionary<string, HeaderFilePriority>? customPriorities = null)
    {
        ArgumentNullException.ThrowIfNull(headerFiles);

        var headerFilesList = headerFiles.ToList();

        // Initialize collections
        this._all = [];
        this._byNumericCode = new Dictionary<long, List<ErrorCodeWithSource>>();
        this._bySymbol = new Dictionary<string, List<ErrorCodeWithSource>>(StringComparer.OrdinalIgnoreCase);
        this._facilitiesByFile = new Dictionary<string, Dictionary<int, string>>(StringComparer.OrdinalIgnoreCase);
        this._globalFacilities = new Dictionary<int, string>();
        this._symbolIndex = new Dictionary<string, HashSet<ErrorCodeWithSource>>(StringComparer.OrdinalIgnoreCase);

        this._filePriorities = customPriorities ?? DefaultPriorities;

        foreach (var headerFile in headerFilesList)
        {
            this.ProcessHeaderFile(headerFile);
        }

        this.SortCollectionsByPriority();
        this.BuildGlobalFacilities(headerFilesList);
        this._hresultProcessor = new HResultProcessor(this._facilitiesByFile, this._globalFacilities);
    }

    public IEnumerable<LookupResult> Lookup(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            yield break;

        var normalizedInput = input.Trim();
        var parsedValue = ParsedInput.ParseInput(normalizedInput);
        var searched = new HashSet<ErrorCodeWithSource>();

        // Try numeric lookups
        foreach (var result in this.LookupNumeric(parsedValue, searched))
        {
            yield return result;
        }

        // Try symbol lookups
        foreach (var result in this.LookupSymbol(normalizedInput, searched))
        {
            yield return result;
        }

        // Try partial symbol matches
        foreach (var result in this.LookupPartialSymbol(normalizedInput, searched))
        {
            yield return result;
        }
    }

    private HeaderFilePriority GetFilePriority(string fileName)
    {
        return this._filePriorities.GetValueOrDefault(fileName, HeaderFilePriority.Normal);
    }

    private void ProcessHeaderFile(HeaderFile headerFile)
    {
        this._facilitiesByFile[headerFile.HeaderFileName] = new Dictionary<int, string>();
        foreach (var headerFileFacility in headerFile.Facilities)
        {
            this._facilitiesByFile[headerFile.HeaderFileName][headerFileFacility.Code] = headerFileFacility.Name;
        }

        var priority = this.GetFilePriority(headerFile.HeaderFileName);

        foreach (var errorCode in headerFile.ErrorCodes)
        {
            var errorWithSource = new PrioritizedErrorCodeWithSource(errorCode, headerFile.HeaderFileName, priority);
            this._all.Add(errorWithSource);

            this.AddToNumericLookup(errorCode.DecimalCode, errorWithSource);

            if (!string.IsNullOrWhiteSpace(errorCode.Id))
            {
                var symbolKey = errorCode.Id.Trim();
                this.AddToSymbolLookup(symbolKey, errorWithSource);
                this.BuildSymbolIndex(symbolKey, errorWithSource);
            }
        }
    }

    private void AddToNumericLookup(long code, ErrorCodeWithSource entry)
    {
        if (!this._byNumericCode.TryGetValue(code, out var list))
        {
            list = [];
            this._byNumericCode[code] = list;
        }

        list.Add(entry);
    }

    private void AddToSymbolLookup(string symbol, ErrorCodeWithSource entry)
    {
        if (!this._bySymbol.TryGetValue(symbol, out var list))
        {
            list = [];
            this._bySymbol[symbol] = list;
        }

        list.Add(entry);
    }

    private void BuildSymbolIndex(string symbol, ErrorCodeWithSource entry)
    {
        var words = symbol.Split('_');
        foreach (var word in words.Where(static w => w.Length > 2))
        {
            if (!this._symbolIndex.TryGetValue(word, out var set))
            {
                set = [];
                this._symbolIndex[word] = set;
            }

            set.Add(entry);
        }
    }

    private void SortCollectionsByPriority()
    {
        this._all.Sort(ComparePriority);

        foreach (var list in this._byNumericCode.Values)
        {
            list.Sort(ComparePriority);
        }

        foreach (var list in this._bySymbol.Values)
        {
            list.Sort(ComparePriority);
        }
    }

    private static int ComparePriority(ErrorCodeWithSource x, ErrorCodeWithSource y)
    {
        var xPriority = (x as PrioritizedErrorCodeWithSource)?.Priority ?? HeaderFilePriority.Normal;
        var yPriority = (y as PrioritizedErrorCodeWithSource)?.Priority ?? HeaderFilePriority.Normal;
        return yPriority.CompareTo(xPriority);
    }

    private void BuildGlobalFacilities(List<HeaderFile> headerFiles)
    {
        // Sort header files by priority (low to high, so high priority overwrites)
        var sortedFiles = headerFiles
            .OrderBy(h => this.GetFilePriority(h.HeaderFileName))
            .ToList();

        foreach (var headerFile in sortedFiles)
        {
            foreach (var facility in headerFile.Facilities)
            {
                this._globalFacilities[facility.Code] = facility.Name;
            }
        }
    }

    private IEnumerable<LookupResult> LookupNumeric(ParsedInput parsed, HashSet<ErrorCodeWithSource> searched)
    {
        if (parsed.HasDecimalValue)
        {
            foreach (var entry in this.FindByNumericCode(parsed.DecimalValue!.Value, searched))
            {
                if (parsed.DecimalValue is >= int.MinValue and < 0)
                {
                    yield return CreateLookupResult($"Matches for {parsed.DecimalValue} / 0x{(int)parsed.DecimalValue:X}", entry);
                }
                else
                {
                    yield return CreateLookupResult($"Matches for {parsed.DecimalValue} / 0x{parsed.DecimalValue:X}",
                        entry);
                }
            }

            if (parsed.IsInRangeOfUnsignedInt32)
            {
                foreach (var result in this.LookupAsHResult((uint)parsed.DecimalValue.Value, searched))
                {
                    yield return result;
                }
            }
        }

        if (parsed.HasHexValue)
        {
            var value = (long)parsed.HexValue!.Value;

            var directMatchAsHex
                = new Interpretation($"Matches for 0x{parsed.HexValue:X} / {parsed.HexValue}", MatchType.Exact);
            foreach (var entry in this.FindByNumericCode(value, searched))
            {
                yield return CreateLookupResult(directMatchAsHex, entry);
            }

            if (parsed.HexValue.Value > int.MaxValue)
            {
                var signedValue = (int)parsed.HexValue.Value;
                if (signedValue != value)
                {
                    foreach (var entry in this.FindByNumericCode(signedValue, searched))
                    {
                        yield return CreateLookupResult($"Matches for 0x{parsed.HexValue:X} / {signedValue}", entry);
                    }
                }
            }

            foreach (var result in this.LookupAsHResult((uint)parsed.HexValue.Value, searched))
            {
                yield return result;
            }
        }
    }

    private IEnumerable<LookupResult> LookupSymbol(string input, HashSet<ErrorCodeWithSource> searched)
    {
        if (this._bySymbol.TryGetValue(input, out var list))
        {
            var interpretation = new Interpretation($"Exact symbol match: {input}", MatchType.Exact);
            foreach (var entry in list.Where(searched.Add))
            {
                yield return CreateLookupResult(interpretation, entry);
            }
        }
    }

    private IEnumerable<LookupResult> LookupPartialSymbol(string input, HashSet<ErrorCodeWithSource> searched)
    {
        var upperInput = input.ToUpperInvariant();

        if (this._symbolIndex.TryGetValue(upperInput, out var indexedEntries))
        {
            var sortedEntries
                = indexedEntries.OrderBy(static e => e, Comparer<ErrorCodeWithSource>.Create(ComparePriority));
            var interpretation = new Interpretation($"Partial symbol match", MatchType.Partial);

            foreach (var entry in sortedEntries.Where(searched.Add))
            {
                yield return CreateLookupResult(interpretation, entry);
            }
        }

        // Fallback to full scan for complex patterns
        foreach (var entry in this._all)
        {
            if (searched.Contains(entry)
                || !entry.ErrorCode.Id.Contains(upperInput, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (searched.Add(entry))
            {
                yield return CreateLookupResult("Partial symbol match", entry);
            }
        }
    }

    private IEnumerable<ErrorCodeWithSource> FindByNumericCode(long code, HashSet<ErrorCodeWithSource> searched)
    {
        if (this._byNumericCode.TryGetValue(code, out var list))
        {
            foreach (var entry in list.Where(searched.Add))
            {
                yield return entry;
            }
        }
    }

    private IEnumerable<LookupResult> LookupAsHResult(uint hresult, HashSet<ErrorCodeWithSource> searched)
    {
        var components = HResultProcessor.HResultComponents.FromHResult(hresult);
        var matchingErrors = new List<ErrorCodeWithSource>();


        var hresultDescription = this._hresultProcessor.FormatComponents(components, matchingErrors);
        var interpretation = new Interpretation($"As HRESULT 0x{hresult:X}", MatchType.Inferred, hresultDescription);

        foreach (var entry in this.FindByNumericCode(components.Code, searched))
        {
            matchingErrors.Add(entry);
            yield return CreateLookupResult(interpretation, entry);
        }
    }

    private static LookupResult CreateLookupResult(string explanation, ErrorCodeWithSource entry)
    {
        var priority = (entry as PrioritizedErrorCodeWithSource)?.Priority ?? HeaderFilePriority.Normal;
        return new LookupResult(new Interpretation(explanation, MatchType.Exact), entry, priority);
    }

    private static LookupResult CreateLookupResult(Interpretation interpretation, ErrorCodeWithSource entry)
    {
        var priority = (entry as PrioritizedErrorCodeWithSource)?.Priority ?? HeaderFilePriority.Normal;
        return new LookupResult(interpretation, entry, priority);
    }

    private sealed class ParsedInput
    {
        public required string Original { get; internal init; }

        public ulong? HexValue { get; private set; }
        public long? DecimalValue { get; private set; }

        public bool IsInRangeOfSignedInt32 => this.DecimalValue is >= int.MinValue and <= int.MaxValue;
        public bool IsInRangeOfUnsignedInt32 => this.DecimalValue is >= 0 and <= uint.MaxValue;

        public int? SignedValue => this.DecimalValue is > int.MaxValue ? (int)(uint)this.DecimalValue.Value : null;

        public bool HasHexValue => this.HexValue.HasValue;
        public bool HasDecimalValue => this.DecimalValue.HasValue;
        public bool HasSignedValue => this.SignedValue.HasValue;


        internal static ParsedInput ParseInput(string input)
        {
            var result = new ParsedInput { Original = input };

            var normalizedInput = input.Replace(" ", "").Replace("_", "").ToUpperInvariant();

            if (HexPattern.IsMatch(normalizedInput) || normalizedInput.Any(char.IsAsciiHexDigit))
            {
                if (ulong.TryParse(normalizedInput.TrimStart('0', 'X', '&', 'H'),
                        System.Globalization.NumberStyles.HexNumber, null, out var hexValue))
                {
                    result.HexValue = hexValue;
                }
            }

            if (normalizedInput.StartsWith('-') && long.TryParse(normalizedInput, out var decimalValue))
            {
                result.DecimalValue = decimalValue;
            }

            if (long.TryParse(normalizedInput, out var decimalValueWithoutPrefix))
            {
                result.DecimalValue = decimalValueWithoutPrefix;
            }

            return result;
        }
    }

    internal sealed record PrioritizedErrorCodeWithSource(
        ErrorCodeDto ErrorCode,
        string SourceFile,
        HeaderFilePriority Priority)
        : ErrorCodeWithSource(ErrorCode, SourceFile);

    [GeneratedRegex(@"^(?:0x|&h)([0-9a-f]+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex HexRegex();
}