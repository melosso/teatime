---
title: Environment Variables
description: Every setting Teatime reads from the environment.
page-prev: /deploy/
page-next: /examples/
---

Teatime runs fine with none of these set. They earn their keep at deployment time: the port, whether drafts show, and any key that should stay out of version control.

Environment variables win over `appsettings.json`. Nested keys use a double underscore, so `Docs:PageSize` becomes `Docs__PageSize`.

## Hosting

| Variable | Default | What it does |
| --- | --- | --- |
| `ASPNETCORE_URLS` | `http://localhost:5000` | Address and port. The Docker image sets `http://+:8080`. |
| `ASPNETCORE_ENVIRONMENT` | `Production` | `Development` shows drafts and logs more. |

| `Proxy__Trusted__0` | none | A proxy IP or CIDR network allowed to set `X-Forwarded-For`. |
| `Proxy__TrustAny` | `false` | Honours the forwarded header from any caller. |

Worth double checking on a live server: drafts are visible in `Development` and hidden everywhere else.

### Behind a reverse proxy

Rate limits are counted per reader IP, and behind nginx, Caddy, or a container ingress every request arrives from the proxy instead. Left unconfigured, the sign-up budget of five per ten minutes ends up shared by everyone, and a single bot can close the form for the whole site.

Listing your proxy fixes it. Loopback is trusted already, so a proxy on the same host needs nothing:

```bash
Proxy__Trusted__0=10.0.0.0/8
Proxy__Trusted__1=172.18.0.5
```

`Proxy__TrustAny=true` skips the list entirely, which is convenient when your ingress has no fixed address. It does mean any caller who can reach the port may claim any IP, so it suits a container that only its proxy can talk to, and little else. Teatime notes the choice in the startup log.

## Content

| Variable | Default | What it does |
| --- | --- | --- |
| `Docs__RootPath` | `content` | Path to your content folder. |
| `Docs__PageSize` | `10` | Posts per page on the home feed. |
| `Docs__EnableHotReload` | `true` | Rebuilds when a file changes. |
| `Docs__DefaultPage` | `index` | Filename used as a folder's own page. |
| `Docs__BasePath` | none | Subdirectory prefix, such as `/blog`. |
| `Docs__ContentSecurityPolicy` | built in | Replaces the default policy. |

`Docs__BasePath` prefixes every internal link. Pass the same value to a static export with `--base-path` so the two agree.

## Health checks

`GET /health` answers `200` with the current build version, page count, and uptime in seconds:

```json
{ "status": "ok", "buildVersion": 17, "pages": 42, "uptimeSeconds": 3600 }
```

It answers `503` with `"status": "empty"` when no content has been built, which is what an uptime monitor such as [Uptime Kuma](https://github.com/louislam/uptime-kuma) should watch for. The route carries no rate limit, so polling it every few seconds is fine.

## Logs

Warnings and errors are written to `logs/teatime-<date>.log` beside the binary, rolling daily and keeping a fortnight. Everything at `Information` continues to go to the console only, which keeps the file small enough to read.

The `Serilog` section of `appsettings.json` holds the settings, so pointing `path` at a mounted volume or lowering `restrictedToMinimumLevel` are both single-line changes. Setting `Serilog__WriteTo__1__Args__path` in the environment works too.

## Secrets

No fixed names. An extension setting reading `${SOME_NAME}` looks up exactly that variable:

```json [content/extensions.json]
{
  "extensions": {
    "beacon": {
      "enabled": true,
      "url": "https://beacon.example.com",
      "bucket": "newsletter_en",
      "apiKey": "${BEACON_API_KEY}"
    }
  }
}
```

Unset means the extension stays disabled, with the reason in the startup log rather than in a reader's browser. See [Extensions](/extensions/) for the rest.

## In a container

```yaml [docker-compose.yml]
services:
  teatime:
    image: teatime
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - Docs__PageSize=12
      - BEACON_API_KEY=${BEACON_API_KEY}
    volumes:
      - ./content:/app/content
```

Leaving the secret's value out, as above, lets Docker read it from your shell or a `.env` file, so it never lands in the file you commit.

::: Warning
 If a setting seems ignored, check for a single underscore where a double belongs. `Docs_PageSize` is nothing at all, `Docs__PageSize` is the setting you meant.
:::