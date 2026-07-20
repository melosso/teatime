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

Worth double checking on a live server: drafts are visible in `Development` and hidden everywhere else.

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