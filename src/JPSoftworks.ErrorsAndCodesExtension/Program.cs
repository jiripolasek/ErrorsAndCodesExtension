// ------------------------------------------------------------
//
// Copyright (c) Jiří Polášek. All rights reserved.
//
// ------------------------------------------------------------

using System;
using System.Threading.Tasks;
using JPSoftworks.CommandPalette.Extensions.Toolkit;

namespace JPSoftworks.ErrorsAndCodes;

public static class Program
{
    [MTAThread]
    public static async Task Main(string[] args)
    {
        await ExtensionHostRunner.RunAsync(
            args,
            new ExtensionHostRunnerParameters
            {
                PublisherMoniker = "JPSoftworks",
                ProductMoniker = "ErrorsAndCodes",
                ExtensionFactories = [
                    new DelegateExtensionFactory(manualResetEvent => new ErrorsAndCodes(manualResetEvent))
                ]
            });
    }
}