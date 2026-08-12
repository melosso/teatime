---
title: Markdown Reference
description: Every container, code block, image, and map option Teatime adds on top of plain Markdown.
page-prev: /examples/
---

Your posts are Markdown, and Teatime gives you more than the basics. This page is the syntax reference. For a page that renders all of it at once, see the [style test](/examples/style-test/).

## Callout containers

Callouts read a little softer than a plain quote, and they come in several tones:

::: note
A note container suits context you would like readers to notice without alarm.
:::

::: warning
A warning container fits the rare moment when a detail could really trip someone up.
:::

::: danger
A danger container is reserved for the few cases where a mistake is costly.
:::

You can fold a longer aside behind a summary to keep the page scannable:

::: details A longer aside, folded away
Content inside a `details` container stays hidden until a reader opens it, which keeps supporting detail close by without crowding the main thread.
:::

Small inline labels help with versions or status <Badge type="tip">v1.0+</Badge>. Always pair it with a closing tag, since `<Badge .../>` would swallow the rest of the paragraph.

## Code blocks

A code block can carry a title, highlight a line, and number its lines:

````md
```csharp:line-numbers {3} [Program.cs]
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
app.MapGet("/", () => "Hello from Teatime"); // this line is highlighted
app.Run();
```
````

## Images

The alt text becomes a caption below the image, and a few attributes set the size:

```md
![A quiet caption for your image.](/assets/example.webp)
```

- `{.natural}` keeps the original size, and `{.plain}` drops the frame
- `{.left}` and `{.right}` float the image beside your text
- `{.wide}` reaches a little past the reading column
- `{.full}` spans the whole viewport

The post cover set in [front matter](/examples/frontmatter/) accepts the same width attributes, plus two that tune its height.

For a row of images side by side, wrap them in a gallery. The [gallery](/examples/gallery/) page shows one in place:

```md
::: gallery
![](/assets/one.webp)
![](/assets/two.webp)
![](/assets/three.webp)
:::
```

## Maps

To place points on a map, use a `map` block. Each pin needs `coords` and can carry a name, a phone number, a contact address, a website, and a line of text. The pins show as markers, and a click opens their details:

```map
zoom: 8
pins:
  - name: The Reading Room
    coords: 52.0907, 5.1214
    phone: "030 123 4567"
    contact: hello@example.nl
    url: https://example.nl
    text: Open on Tuesday afternoons.
  - name: The Quiet Corner
    coords: 52.3676, 4.9041
    text: A weekly gathering on Thursdays.
```

Leave `zoom` and `center` out and the map frames every pin for you. Maps use [OpenStreetMap](https://www.openstreetmap.org/copyright) tiles, which load from OpenStreetMap when a reader opens the page.

## Definition lists, footnotes, and links

Definition lists keep paired ideas tidy:

Static export
:   A folder of plain HTML you can host anywhere.

Live server
:   The running app that reads your Markdown and renders it on request.

And when a thought needs a source, a footnote tucks it out of the way.[^1]

External links can open in a new tab with a small attribute, as standardized in [the Markdig reference](https://github.com/xoofx/markdig){target="_blank" rel="noopener"}, written as `{target="_blank" rel="noopener"}`.

[^1]: Footnotes render at the bottom of the page with a link back to where you were reading.
