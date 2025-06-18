﻿// ------------------------------------------------------------
// 
// Copyright (c) Jiří Polášek. All rights reserved.
// 
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JPSoftworks.CommandPalette.Extensions.Toolkit.Logging;
using JPSoftworks.ErrorsAndCodes.Models;
using JPSoftworks.ErrorsAndCodes.Services.WindowsErrors;
using Microsoft.VisualStudio.Threading;

namespace JPSoftworks.ErrorsAndCodes.Services;

internal class ErrorDataService
{
    private readonly AsyncLazy<ErrorDataServiceModel> _lazyInit = new(InitializeAsync);

    public Task<ErrorDataServiceModel> GetModelAsync() => this._lazyInit.GetValueAsync();

    public async Task<WindowsErrorLookup> GetErrorLookup()
    {
        var model = await this._lazyInit.GetValueAsync();
        return model.Lookup;
    }

    private static async Task<ErrorDataServiceModel> InitializeAsync()
    {
        try
        {
            var headerFiles
                = await HeaderFilesLoader.LoadHeaderFiles(
                    Windows.ApplicationModel.Package.Current!.InstalledLocation.Path + @"\Assets\Data");
            return new ErrorDataServiceModel(headerFiles);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex);
            throw;
        }
    }

    public class ErrorDataServiceModel(IEnumerable<HeaderFile> headerFiles)
    {
        public WindowsErrorLookup Lookup { get; } = new WindowsErrorLookup(headerFiles);
    }
}