using Aquari.One.AspNetCore.Concurrency;
using Microsoft.Extensions.DependencyInjection;

namespace Aquari.One.AspNetCore;

/// <summary>
/// Provides ASP.NET Core dependency-injection registration for Aquari One.
/// </summary>
public static class AquariServiceCollectionExtensions
{
    /// <summary>
    /// Registers Aquari One runtime authentication services.
    /// </summary>
    public static IServiceCollection AddAquariOne(
        this IServiceCollection services,
        AquariAspNetCoreOptions options)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);

        options.Validate();

        services.AddSingleton(options);

        if (options.RefreshLock is null)
        {
            services.AddSingleton<IAquariRefreshLock, InMemoryAquariRefreshLock>();
        }
        else
        {
            services.AddSingleton<IAquariRefreshLock>(options.RefreshLock);
        }

        services.AddSingleton(
            static provider =>
            {
                var resolved =
                    provider.GetRequiredService<AquariAspNetCoreOptions>();

                return new AquariClient(
                    resolved.ToClientOptions());
            });

        services.AddSingleton(
            provider =>
            {
                var resolved =
                    provider.GetRequiredService<AquariAspNetCoreOptions>();

                var client =
                    provider.GetRequiredService<AquariClient>();

                var refreshLock =
                    provider.GetRequiredService<IAquariRefreshLock>();

                return new AquariAspNetCoreAuth(
                    new AquariAspNetCoreOptions
                    {
                        Issuer = resolved.Issuer,
                        ClientId = resolved.ClientId,
                        ClientSecret = resolved.ClientSecret,
                        RedirectUri = resolved.RedirectUri,
                        Scopes = resolved.Scopes,
                        SessionStore = resolved.SessionStore,
                        RefreshLock = refreshLock,
                        SessionCookieName = resolved.SessionCookieName,
                        SessionCookieDomain = resolved.SessionCookieDomain,
                        TransactionCookieName = resolved.TransactionCookieName,
                        SecureCookies = resolved.SecureCookies,
                        SessionTtl = resolved.SessionTtl,
                        Timeout = resolved.Timeout,
                        MaxRetries = resolved.MaxRetries,
                        MaxRetryDelay = resolved.MaxRetryDelay,
                    },
                    client);
            });

        return services;
    }
}
