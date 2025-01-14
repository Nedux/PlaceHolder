﻿using GP3.Common.Constants;
using GP3.Common.DB;
using GP3.Common.Repositories;
using GP3.Funcs.DesignTimeDB;
using GP3.Funcs.Functions.ServiceBus;
using GP3.Funcs.Services;
using GP3.Funcs.Services.HistoryProviders;
using GP3.Scraper;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.Functions.Extensions.JwtCustomHandler;
using Microsoft.Azure.Functions.Extensions.JwtCustomHandler.Interface;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Configurations;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;
using Polly.Timeout;
using PuppeteerSharp;
using System;
using System.IO;
using System.Net.Http;

[assembly: FunctionsStartup(typeof(GP3.Funcs.Startup))]
namespace GP3.Funcs
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            var conf = builder.GetContext().Configuration;

            builder.Services.AddSingleton(new BrowserFetcherOptions { Path = Path.GetTempPath() });

            builder.Services.AddSingleton<ReqAuthService>();

            builder.Services.AddScoped<IPriceBrowser, PriceBrowser>();
            builder.Services.AddScoped<IPricePage, PricePage>();
            builder.Services.AddScoped<IPriceFetcher, PriceFetcher>();

            builder.Services.AddScoped<FetcherService>();
            builder.Services.AddScoped<PriceService>();

            /* Register electricity history providers */
            builder.Services.AddSingleton<Eso>();
            builder.Services.AddSingleton<Ignitis>();
            builder.Services.AddSingleton<Perlas>();
            builder.Services.AddSingleton<ProviderFactory>();
            builder.Services.AddScoped<ProviderService>();

            builder.Services.AddTransient<IHistoryRegistrationRepository, HistoryRegistrationRepository>();
            builder.Services.AddTransient<IIntegrationRepository, IntegrationRepository>();
            builder.Services.AddTransient<IDayPriceRepository, DayPriceRepository>();
            builder.Services.Decorate<IDayPriceRepository, CachedDayPriceRepository>();

            builder.Services
                .AddHttpClient<IntegrationServiceBus>()
                .AddPolicyHandler(HttpPolicyExtensions
                    .HandleTransientHttpError()
                    .Or<TimeoutRejectedException>()
                    .RetryAsync(2))
                .AddPolicyHandler(Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromMilliseconds(500)));

            builder.Services.AddStackExchangeRedisCache(o => o.Configuration = conf.GetConnectionString(ConnStrings.Redis));

            var dbStr = conf.GetConnectionString(ConnStrings.SQL);
            builder.Services
                .UseMySqlMig<DayPriceDbContext>(dbStr, ConnStrings.ContextAssembly)
                .UseMySqlMig<IntegrationDbContext>(dbStr, ConnStrings.ContextAssembly)
                .UseMySqlMig<HistoryRegistrationDbContext>(dbStr, ConnStrings.ContextAssembly);

            builder.Services.AddTransient<IFirebaseTokenProvider, CustomTokenProvider>(provider => new CustomTokenProvider(
                issuer: "https://securetoken.google.com/gp3-auth",
                audience: "gp3-auth"));
        }
    }

    internal class OpenApiConfigurationOptions : DefaultOpenApiConfigurationOptions
    {
        public override OpenApiVersionType OpenApiVersion { get; set; } = OpenApiVersionType.V3;
    }
}
