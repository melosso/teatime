# 🍵 Teatime

Meet Teatime: a personal blog engine that is really just a folder of Markdown files. If you can write a `.md` file, you already have a fast, self hosted blog with tags, an archive, and an RSS feed ready to go.

Teatime is built on the modern .NET stack. It grew out of [Bark](https://github.com/melosso/bark), a Markdown documentation server, so it keeps that same instant, in memory rendering approach and reshapes it for writing posts rather than documentation. There is no database, and no separate build step to wait on.

## How it works

Your writing lives in a `content/` folder:

```
content/
  posts/        your blog posts, one .md file each
  pages/        standalone pages like About, served at /about
  config.json   optional site settings
```

A post is a Markdown file with a little front matter at the top:

```markdown
---
title: Hello, Teatime
date: 2026-07-01
tags: [meta, dotnet]
summary: A short line for the index and the feed.
---

Welcome to your new blog. You can write anything you like here.
```

To get going, you really only need a `title` and a `date`. The `date` sets the order on your home page (newest first) and in the feed. You are welcome to add `tags`, a `summary`, a custom `slug`, or to mark a post as `draft: true` so it stays out of your listings until you are ready. Drafts remain visible while you run locally in Development, and are quietly held back everywhere else.

Once a post is saved, it appears right away. Teatime watches your files and rebuilds in memory, so there is nothing to recompile.

## Installation

The quickest way to run Teatime is the published container image, which has everything bundled and ready.

### Docker

Create a `docker-compose.yml` next to your writing:

```yaml
services:
  teatime:
    image: ghcr.io/melosso/teatime:latest
    container_name: teatime
    ports:
      - "8080:8080"
    volumes:
      - ./content:/app/content
```

Mount your own `content/` folder holding your `.md` files and an optional `config.json`. When that is in place, bring it up:

```bash
docker compose up -d
```

Your blog is then waiting at `http://localhost:8080`.

### Windows and IIS

If you would rather host on Windows, each release ships a ready to run build:

1. Download the latest `*-Windows_x64.zip` from the [Releases](https://github.com/melosso/teatime/releases) page.
2. Extract it into your site folder, for example `C:\inetpub\teatime`.
3. Create an IIS site pointed at that folder, with the CLR version set to "No Managed Code".
4. Make sure the [.NET 11 Hosting Bundle](https://dotnet.microsoft.com/download/dotnet/11.0) is installed.
5. Start the site and browse to it.

The zip already includes a `web.config` wired for in process hosting, so no manual edits are needed. A `*-Linux_x64.zip` build is attached to each release as well.

## Writing

Teatime renders your content through the same Markdig pipeline [Bark](https://github.com/melosso/bark) uses. If you would like a refresher on the syntax itself, the [Markdown Guide](https://www.markdownguide.org/) is a friendly and thorough place to start. You also get:

- A chronological home page with pagination
- Individual post pages, each with previous and next links
- Tag pages (`/tags` and `/tags/your-tag`) and a year by year `/archive`
- An RSS feed at `/feed.xml`, plus `/sitemap.xml` and `/robots.txt`
- Full text search
- Light and dark themes

And some more unmentioned features.

## Configuring your site

A `content/config.json` file is entirely optional. It lets you set details like your site title, description, and social links:

```json
{
  "title": "Teatime",
  "description": "A personal blog published with Teatime.",
  "socialLinks": [
    { "icon": "github", "url": "https://github.com/you" }
  ]
}
```

Both your posts and this config are hot reloaded, so you can adjust them while the server is running. Theme details, such as CSS variables and dark mode, are handled through `appsettings.json` or environment variables, which keeps your content folder focused purely on writing.

## License

Please see [LICENSE](LICENSE) for the full terms.
