# Aquari One .NET SDK

Official Aquari One packages for .NET applications and trusted server integrations.

## Packages

- `Aquari.One` — OAuth/OpenID Connect, Passport, trust, transport, and structured errors.
- `Aquari.One.Server` — trusted-server machine, issuer, and webhook APIs.
- `Aquari.One.AspNetCore` — ASP.NET Core application-session integration.

## Requirements

- .NET 10 (`net10.0`)
- nullable reference types enabled
- warnings treated as errors

## Security boundary

Use `Aquari.One` and `Aquari.One.AspNetCore` for user authentication, Passport reads, and trust evaluation.

Use `Aquari.One.Server` only in trusted backend processes that can protect machine API keys, issuer credentials, webhook secrets, and confidential OAuth client credentials.

Aquari authenticates the person and exposes portable Passport and trust data. The consuming application decides its own local roles and permissions.

## Documentation

See `https://docs.aquari.one/sdks/dotnet` for the integration guide and API reference.

## License

Apache-2.0. See `LICENSE` and `NOTICE`.

Source repository (GitLab, authoritative): https://gitlab.com/nousen-group/aquari/aquari-dotnet
GitHub read-only mirror: https://github.com/AquariOne/aquari-dotnet

The public SDK source is exported from Aquari One’s private monorepo. Contributions and release decisions are managed on GitLab; do not push directly to the GitHub mirror.
