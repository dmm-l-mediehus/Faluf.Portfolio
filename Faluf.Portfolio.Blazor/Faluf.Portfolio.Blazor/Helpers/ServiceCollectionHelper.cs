﻿using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.MSSqlServer;
using System.Net;
using System.Reflection;
using System.Text;

namespace Faluf.Portfolio.Blazor.Helpers;

public static class ServiceCollectionHelper
{
    public static void AddPortfolioCore(this WebApplicationBuilder builder)
    {
        // Logger
        builder.Host.UseSerilog((hostingContext, loggerConfiguration) =>
        {
            loggerConfiguration.MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning);
            loggerConfiguration.MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning);

            loggerConfiguration.WriteTo.Console(LogEventLevel.Information);
            loggerConfiguration.WriteTo.MSSqlServer(
                connectionString: builder.Configuration.GetConnectionString("PortfolioConnection"),
                sinkOptions: new MSSqlServerSinkOptions { TableName = "Logs", AutoCreateSqlTable = true },
                restrictedToMinimumLevel: LogEventLevel.Warning);
        });

        // Localization
        builder.Services.AddLocalization();

		// AddHttpClient
		CookieContainer cookieContainer = new();
		HttpClientHandler httpClientHandler = new() { CookieContainer = cookieContainer };
		builder.Services.AddSingleton(cookieContainer);
		builder.Services.AddHttpClient(Globals.APIClient, client => client.BaseAddress = new Uri(builder.Configuration["API:BaseURL"]!)).ConfigurePrimaryHttpMessageHandler(() => httpClientHandler);

        // Validations
        builder.Services.AddValidatorsFromAssembly(Assembly.Load("Faluf.Portfolio.Core"));
    }

    public static void AddPortfolioDatabases(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContextFactory<PortfolioDbContext>(options =>
        {
            options.UseSqlServer(configuration.GetConnectionString("PortfolioConnection"), options => options.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery));
            options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
        });
    }

    public static void AddPortfolioAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddCascadingAuthenticationState();
        services.AddScoped<JWTAuthenticationStateProvider>();
        services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<JWTAuthenticationStateProvider>());
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(options =>
		{
			options.TokenValidationParameters = new TokenValidationParameters
			{
				ValidateLifetime = false,
				ValidateIssuerSigningKey = true,
				ValidIssuer = configuration["JWT:Issuer"]!,
				ValidAudience = configuration["JWT:Audience"]!,
				IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["JWT:Secret"]!)),
				ClockSkew = TimeSpan.Zero
			};

			options.Events = new JwtBearerEvents
			{
				OnMessageReceived = context =>
				{
					if (context.Request.Cookies.TryGetValue(Globals.AccessToken, out string? accessToken))
					{
						IDataProtector dataProtector = context.HttpContext.RequestServices.GetRequiredService<IDataProtectionProvider>().CreateProtector(Globals.AuthProtector);

						context.Token = dataProtector.Unprotect(accessToken);
					}

					return Task.CompletedTask;
				}
			};
		});
	}

    public static void AddPortfolioRepositories(this IServiceCollection services)
    {
        services.AddScoped<IAuthStateRepository, AuthStateRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
    }

    public static void AddPortfolioServices(this IServiceCollection services)
    {
        services.AddSingleton<ITokenProvider, TokenProvider>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
	}
}