---
title: Search and Social Previews
description: How Teatime builds the meta tags, social cards, and structured data for every page.
page-prev: /examples/frontmatter/ 
---

You do not have to hand-write any meta tags. Teatime builds the canonical link, the full Open Graph and Twitter Card set, and a JSON-LD block into every page's `<head>`, all from the live request and your front matter.

For most sites this is the whole story: set a `title` and a `date`, add a `cover` when you have one, and the previews take care of themselves. The sections below open up if you want to see exactly what is generated and how to steer it.

::: details The preview image

The image on a post card comes from its `cover`. For pages without one, Teatime falls back to a site-wide `image` in your `config.json`, and then to your `brandImage`.

```json [content/config.json]
{
  "image": "/assets/social-card.png"
}
```

A relative path such as `/assets/cover.png` is turned into an absolute URL for you, so the card works when it is shared off-site. When an image is present the Twitter card upgrades to `summary_large_image`; with no image it stays a plain `summary`.

:::

::: details Open Graph and Twitter tags

Every page carries an Open Graph set (`og:type`, `og:title`, `og:url`, `og:description`, plus `og:image` when there is one) and the matching Twitter fields (`twitter:title`, `twitter:description`, `twitter:card`, `twitter:image`).

The `og:type` is `website` on index pages and `article` on posts. On a post, `article:modified_time` is added when the page has an update date. The `og:site_name` and `og:locale` values are read from your `brand` (or `title`) and `lang`.

:::

::: details Structured data for search engines

Teatime writes a JSON-LD block so search engines can read the page as data, not just markup.

Posts are described as an `Article` with a headline, description, image, publisher, and published and modified dates. Index pages are described as a `WebSite` with a name, URL, and description. Both are namespaced against `schema.org`, and the values track the same front matter your visible previews use.

:::

::: details Adding your own tags

The automatic tags cover the common cases, so most sites never touch this. If you ever need something beyond them, the `head` array in `config.json` still lets you add your own tags, and they sit alongside the generated ones rather than replacing them.

:::

Unsure how a page looks when shared? Set a `cover`, save, and check the `<head>` in your browser. 

Teatime will apply your changes instantly.
