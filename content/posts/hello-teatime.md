---
title: Hello, Teatime
author: Melosso
date: 2026-07-01
tags: [meta, dotnet]
summary: Hello world! The first post on a lightweight blog engine that is nothing more than just a folder of Markdown files.
cover: /assets/hello.webp
---

Welcome to **Teatime**, a personal blog engine that serves Markdown straight from a folder. There is no database, no build step, and no JavaScript framework to keep happy.

## Why

Writing should feel as easy as dropping a `.md` file into `content/posts/`. That really is the whole idea.

```csharp
Console.WriteLine("Hello, Teatime");
```

When you save the file, your post appears. The engine reads it once, holds it in memory, and serves it right away.
