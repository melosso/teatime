---
title: Front Matter
description: Every front matter field Teatime understands, gathered in one place.
cover: /assets/frontmatter.webp {.wide}
---

Front matter is the small block of settings at the top of a Markdown file, wrapped in a pair of `---` lines. It tells Teatime how to treat the page. Every field is optional unless noted, and sensible defaults fill in whatever you leave out.

Here is a post that leans on most of them at once:

```md [content/posts/example.md]
---
title: A Complete Example
date: 2026-07-16
updated: 2026-07-20
tags: [meta, writing]
summary: A short line for cards, feeds, and social previews.
cover: /assets/example.webp {.wide}
author: murdock
slug: a-complete-example
draft: false
---

Your writing begins here.
```

## The essentials

Two fields carry most of the weight. Everything else is a refinement.

| Field         | What it does                                                        |
| ------------- | ------------------------------------------------------------------ |
| `title`       | The page heading and the browser tab text                          |
| `description` | Sets the meta description and social preview text. It is not shown on the page, and when a post leaves it out, the summary steps in |

## Posts

These shape how a post appears in your listings, feed, and archive.

| Field     | What it does                                                            |
| --------- | ---------------------------------------------------------------------- |
| `date`    | The primary sort key, so newer posts rise to the top of the home page and feed |
| `updated` | A later revision date, shown as a "Last updated" note when it is newer than `date` |
| `tags`    | A list like `[meta, writing]` that drives the tag pages and the tag index |
| `summary` | The excerpt on cards. It also stands in for the description on feeds and social previews, and falls back to the first paragraph when unset |
| `cover`   | A feature image. It accepts the same width attributes as inline images, so `cover: /assets/hero.webp {.full}` gives you a full width hero |
| `author`  | An id pointing at a file in `content/authors/`. When no match is found, the value is shown as written, and otherwise it falls back to the site author |
| `slug`    | A friendlier URL, replacing the file name in the address              |
| `draft`   | Set to `true` to hold a post out of listings, feeds, and the sitemap. Drafts stay visible while you run locally |

## Pages

Standalone pages under `content/pages/` understand a few extras of their own.

| Field                        | What it does                                                         |
| ---------------------------- | ------------------------------------------------------------------- |
| `redirect`                   | Sends visitors on to another URL instead of rendering the page      |
| `page-next`                  | Adds a "Next" link at the foot of the page, resolved to the target page's title |
| `page-prev`, `page-previous` | The matching "Previous" link. Either spelling works                 |
| `pagination`                 | Set to `false` to hide those previous and next links entirely       |
| `enabled`                    | Set to `false` on a custom `tags` or `archive` page to turn that surface off |

## Search and metadata

A couple of quieter fields help search and the "Last updated" stamp behave the way you would like.

| Field         | What it does                                                        |
| ------------- | ------------------------------------------------------------------ |
| `keywords`    | A list that nudges search weighting and fills the meta keywords    |
| `lastUpdated` | Set to `false` to hide the "Last updated" stamp on a single page   |

If a field ever feels unclear, the friendliest way to learn it is to add it, save, and watch the page refresh. Teatime reads your changes the moment you write them, so there is no wrong turn you cannot simply undo.
