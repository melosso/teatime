---
title: Guide
description: A quick start guide to writing and publishing with Teatime.
page-next: /deploy/
---

Teatime is built to be simple: you write in Markdown, and the server handles the rest. There is no database to configure and no build step to manage.

Work through [Installation](/deploy/install/) or [Docker](/deploy/containers/) first, then follow these steps from your first post to your first deployment.

## 1. Create a Post

To start writing, create a new `.md` file inside your `content/posts/` folder.

Every file needs a block of settings at the top, called [front matter](/examples/frontmatter/). This tells the server how to display your post. Use this template to begin:

```md [content/posts/hello.md]
---
title: A Quiet Introduction
date: 2026-07-15
tags: [meta, writing]
summary: The first note on a blog that is just a folder of Markdown.
cover: /assets/hello.webp
---

Your words begin here.
```

Only `title` and `date` are needed. Everything else is optional, and sensible defaults fill in the rest. The [front matter reference](/examples/frontmatter/) covers every field, including the ones that tune metadata and standalone pages.

Keep images under `content/assets/`, and leave `draft: true` in place until you are happy with the post.

Teatime supports more than basic Markdown. Callout containers, folded asides, code block titles and line highlights, image widths, galleries, and maps all have their own syntax, gathered in the [Markdown reference](/examples/markdown/).

Every page's meta tags, social cards, and search structured data are built for you, straight from this front matter. The [search and social previews](/examples/metadata/) page covers what it generates and how to steer it.

## 2. Preview Locally

Teatime watches your content folder and rebuilds in memory, so a saved file shows up on the next refresh. Nothing to compile.

::: code-group

```bash [Docker]
docker compose up -d
```

```powershell [Windows]
# From the folder you extracted the release into
.\Teatime.exe
```

```bash [Linux]
# From the folder you extracted the release into
./Teatime
```

```bash [From source]
cd Teatime && dotnet watch --project src/Teatime
```

:::

Docker serves on `http://localhost:8080`, the rest on `http://localhost:5000`. Either way, edits under `content/` refresh on their own. To change the port, see [environment variables](/deploy/environment/).

::: tip
A post marked `draft: true` still shows while you develop, yet it stays out of listings, feeds, and the sitemap once you publish. That gives you a private preview with no extra tooling.
:::

## 3. Publish Your Site

Running Teatime as a live service is enough on its own, and you can stop here. Expose the site through your preferred reverse proxy (such as Caddy, Nginx, or Traefik), set up your domain's DNS records, and you are ready to go.

If you would rather hand a folder of plain HTML to a static host, then expand the section below.

::::details Export as static site

With one command, the application will crawl every page, post, tag, archive, and feed and save the files as self-contained, static pages.

  ::: code-group

  ```bash [Docker]
  # A throwaway container, with the output folder mounted so the files land next to you
  docker run --rm -v "$PWD/content:/app/content" -v "$PWD/_site:/app/_site" \
    ghcr.io/melosso/teatime:latest --export /app/_site --base-url https://example.com
  ```

  ```bash [Windows]
  Teatime.exe --export .\_site --base-url https://example.com
  ```

  ```bash [Linux]
  ./Teatime --export ./_site --base-url https://example.com
  ```

  ```bash [From source]
  cd Teatime && dotnet run --project src/Teatime -- --export ./_site --base-url https://example.com
  ```

  :::

  The `--base-url` value is woven into your feed, sitemap, and social tags, so it is worth setting to your real domain. If your blog lives in a subdirectory, add a base path so every internal link resolves correctly:

  ```bash {2}
  ./Teatime --export ./_site \
    --base-url https://example.com/blog --base-path /blog
  ```

  The static `_site` folder is generated from the `content/` directory using `Teatime --export` and can be deployed directly to any static host.

  Check out our [deployment options](/deploy/), or [run Teatime with Docker](/deploy/containers/) if you prefer a live service.

::::

## 4. Translate the Interface

The interface text, like "Keep reading" and the 404 page, reads from `content/locale/`. English is the default. Point `locale.code` at a locale file to switch:

```json [content/config.json]
{
  "locale": { "code": "nl" }
}
```

For your own, copy `en.json` to `{code}.json` and translate the values. Missing keys fall back to English, so a partial file is fine.

## 5. Customize the Look

Teatime ships seven built-in themes. Naming one in `config.json` is the whole opt-in:

```json [content/config.json]
{
  "theme": "ocean"
}
```

| Name | Look |
|---|---|
| `default` | Warm paper and forest green. Used when you set nothing. |
| `casper` | White paper, near-black type, violet accent. |
| `ocean` | Cool blue-grey paper and a deep harbour accent. |
| `deep-space` | Near-black navy and a periwinkle accent. |
| `solarized` | Solarized base tones: warm paper by day, deep teal by night. |
| `laserwave` | Synthwave violet with a hot magenta accent. |
| `signal-dark` | Deep charcoal and an amber accent. |
| `limelight` | Pale off-white, a sage-lime accent with a cyan counterpart. |

Every theme adapts to a full light and dark palette, so the light/dark toggle behaves the same whichever you pick. The dark-sounding names are not dark-only: `signal-dark` has a paper-toned light mode that swaps its amber for bronze. An unrecognized name logs a warning and falls back to `default`, so a typo will never take your site down. The value hot-reloads with the rest of `config.json`, and [environment variables](/deploy/environment/) can override it per deployment.

`theme` picks the palette; page *structure* is a separate setting, so any theme can pair with any structure:

```json [content/config.json]
{
  "theme": "ocean",
  "structure": "editorial"
}
```

| Name | Shape |
|---|---|
| `default` | Teatime's own layout. Used when you set nothing. |
| `editorial` | Magazine shape: a kicker tag, square bleed-width covers, a drop cap, and a single-row topbar with Subscribe. |

To pin one mode instead of following the reader's system, add `dark` or `light` to the same value:

```json [content/config.json]
{
  "theme": "ocean dark"
}
```

A pinned mode ships only that palette and drops the toggle, so readers cannot switch. Written on its own, `"dark"` or `"light"` pins the mode and keeps the `default` theme. Palette and mode resolve separately, so `Docs:Themes:Name` can pin the palette per deployment while `config.json` keeps the mode.

Custom styles live in `wwwroot/theme/custom.css`, picked up at startup. The whole theme runs on CSS variables, so overriding a handful on `:root` restyles the entire site and stays correct in dark mode:

```css [wwwroot/theme/custom.css]
:root {
  --accent: #b4513a;
  --font-sans: "Iowan Old Style", Georgia, serif;
}
```

The common variables are `--accent`, `--text-color`, `--text-muted`, `--bg-color`, `--border`, and the fonts (`--font-sans`, `--font-display`, `--font-mono`). Put dark-mode tweaks behind `:root[data-theme="dark"]`.

When you want a rule to reach only one surface, every page uses a `data-page` hook that follows its URL: `home`, `archive`, `tags`, `authors`, a page slug like `about`, or `posts-{slug}` for a post.

```css [wwwroot/theme/custom.css]
[data-page="archive"] { --accent: #4a7c59; }
```

## 6. Connect a Newsletter or Analytics

Newsletter sign-up and analytics are handled by built-in extensions, configured in an optional `content/extensions.json` next to your `config.json`. Each one is disabled until its settings check out, and the Content Security Policy is widened for you:

```json [content/extensions.json]
{
  "extensions": {
    "plausible": { "enabled": true, "domain": "blog.example.com" }
  }
}
```

The [extensions](/extensions/) page will guide you through Beacon for newsletters, the three analytics providers, and where to keep your API key.

That's all. Happy publishing!
