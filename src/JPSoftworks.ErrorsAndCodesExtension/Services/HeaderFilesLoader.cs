// ------------------------------------------------------------
// 
// Copyright (c) Jiří Polášek. All rights reserved.
// 
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using JPSoftworks.CommandPalette.Extensions.Toolkit.Logging;
using JPSoftworks.ErrorsAndCodes.Models;
using Windows.Storage;

namespace JPSoftworks.ErrorsAndCodes.Services;

internal static class HeaderFilesLoader
{
    public static async Task<List<HeaderFile>> LoadHeaderFiles(string dirPath)
    {
        List<HeaderFile> headerFiles = new();

        try
        {
            // Get the folder
            StorageFolder folder = await StorageFolder.GetFolderFromPathAsync(dirPath)!;

            // Get all JSON files in the directory
            IReadOnlyList<StorageFile> jsonFiles = await folder.GetFilesAsync()!;
            var filteredJsonFiles = jsonFiles.Where(static file =>
                file.FileType?.Equals(".json", StringComparison.OrdinalIgnoreCase) == true);

            // Process each file
            foreach (var file in filteredJsonFiles)
            {
                try
                {
                    string jsonContent = await FileIO.ReadTextAsync(file)!;
                    var headerFile = JsonSerializer.Deserialize(jsonContent, SourceGenerationContext.Default.HeaderFile!);

                    if (headerFile == null)
                    {
                        Logger.LogError($"Failed to deserialize file '{file.Path}' into HeaderFile.");
                        continue;
                    }

                    if (string.IsNullOrEmpty(headerFile.HeaderFileName))
                    {
                        headerFile.HeaderFileName = Path.GetFileNameWithoutExtension(file.Name);
                    }

                    headerFiles.Add(headerFile);
                }
                catch (Exception ex)
                {
                    // Log exception and continue with next file
                    Logger.LogError($"Failed to load or deserialize file '{file.Path}': {ex.Message}", ex);
                }
            }
        }
        catch (Exception ex)
        {
            // Log the directory access exception
            Logger.LogError($"Failed to access directory '{dirPath}': {ex.Message}", ex);
        }

        return headerFiles;
    }
}