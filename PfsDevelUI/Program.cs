/*
 * Copyright (c) 2021 Jami Suni
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

using MudBlazor.Services;
using Blazored.LocalStorage;
using BlazorDownloadFile;
using Serilog;

using PfsDevelUI;
using PfsDevelUI.Shared;
using PfsDevelUI.PFSLib;

Log.Logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .WriteTo.BrowserConsole()       // Serilog.Sinks.BrowserConsole
            .WriteTo.RecordPfsLogsSink()    // Self developed stink to capture all Log's from UI also to Debug Recording
            .CreateLogger();

Log.Information("Started PfsDevelUI(Args: {a})", args);

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddMudServices();
builder.Services.AddBlazorDownloadFile();
builder.Services.AddBlazoredLocalStorage();

builder.Services.AddScoped<PfsClientPlatform>()
                .AddScoped<PfsClientAccess>()
                .AddScoped<PfsUiState>();

Log.Information("Ready to run at {0}UTC", DateTime.UtcNow.ToLongTimeString());

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

await builder.Build().RunAsync();
