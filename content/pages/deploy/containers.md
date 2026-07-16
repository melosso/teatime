---
title: Running Teatime with Docker
description: A take on deploying Teatime as a container.
---

Teatime is an ordinary ASP.NET Core app, so the published image runs anywhere Docker does. Create a `docker-compose.yml`:

```yaml [docker-compose.yml]
services:
  teatime:
    image: ghcr.io/melosso/teatime:latest
    container_name: teatime
    ports:
      - "8080:8080"
    volumes:
      - ./content:/app/content
```

Mount your `content/` folder so your posts stay editable from the host, then bring it up:

```bash
docker compose up -d
```

Your blog is then available at `http://localhost:8080`.

::: tip
Because `content/` is a volume, dropping in a new Markdown post is enough for it to appear. There is nothing to rebuild.
:::

Behind a reverse proxy, set `Docs:BasePath` (or `--base-path` for a static export) so every internal link resolves under your chosen path. For the one time setup, the [installation guide](/deploy/install/) covers the rest.
