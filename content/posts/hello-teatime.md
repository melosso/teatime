---
title: Hello, Teatime
date: 2026-07-01
tags: [meta, dotnet]
summary: The first post on a blog engine that is just a folder of Markdown files.
cover: /assets/tea.webp
---

Welcome to **Teatime**, a personal blog engine that serves Markdown straight from a folder. There is no database, no build step, and no JavaScript framework to keep happy.

## Why

Writing should feel as easy as dropping a `.md` file into `content/posts/`. That really is the whole idea.

```csharp
Console.WriteLine("Hello, Teatime");
```

When you save the file, your post appears. The engine reads it once, holds it in memory, and serves it right away.
