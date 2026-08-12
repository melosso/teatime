---
title: Extensions
page-prev: /deploy/
page-next: /examples/
---

You can easily enable (native) extensions, allowing you to quickly add newsletter subscription forms or analytics tools. 

These can be configured in one optional file, `content/extensions.json`, next to your `config.json`.

### What's supported?

There are three types:

- [Newsletters](#newsletters)
- [Comments](#comments)
- [Analytics](#analytics)

The following extensions are supported:

| Extension | Type | Required keys |
| --- | --- | --- |
| [Beacon](#beacon) | Newsletter, self-hosted | `url`, `bucket`, `apiKey` |
| [Listmonk](#listmonk) | Newsletter, self-hosted | `url`, `listUuid` |
| [Mailchimp](#mailchimp) | Newsletter, hosted | `listId`, `apiKey` |
| [Remark42](#comments) | Comments, self-hosted | `url` |
| [Matomo](#analytics) | Analytics, self-hosted | `url`, `site_id` |
| [Plausible](#analytics) | Analytics, hosted or self-hosted | `domain` |
| [Medama](#analytics) | Analytics, self-hosted | `url` |
| [GoatCounter](#analytics) | Analytics, hosted or self-hosted | `url` |
| [Liwan](#analytics) | Analytics, self-hosted | `url`, `entity` |

Extensions are disabled by default. To enable an extension, set `enabled` to true and make sure its settings pass validation.

If validation fails:

* The extension stays disabled.
* A warning is recorded in the log.

Validation runs automatically every time content rebuilds, so you can safely edit settings while the dev server is running.

Any key can be written as `${ENV_VAR}` and is read from the environment. An unclosed reference like `${MY_KEY` reads as a typo, so the extension stays disabled rather than sending that text upstream as a key. Values are picked up during a rebuild, so after rotating a secret it helps to touch `extensions.json` or restart.

Only Mailchimp insists on `${ENV_VAR}`, since its key opens the whole account. Its in general bad practice to keey your keys in `extensions.json`. 

::: tip 
Keeping your secrets in an `.env` file is generally safer!.
:::

On start, the log reports what came through:

```
Active extensions: matomo, beacon. Invalid: listmonk
```

Something worth knowing before you pick: several of these want a database of their own. Matomo runs on MySQL, Plausible on PostgreSQL together with ClickHouse, and Listmonk on PostgreSQL. Medama, GoatCounter and Remark42 each keep their data in a single file next to their own binary, which is usually the gentler choice when Teatime is the only thing on the box.

Only one newsletter extension can be active. Enabling two disables both and names them, because quietly picking a winner would mean mailing the wrong list. Analytics have no such limit.

## Newsletters

### Beacon

[Beacon](https://github.com/melosso/beacon) is a small consent service. It records who agreed to what and gives each subscriber a link to manage their own preferences. This site talks to the demo instance:

```json [content/extensions.json]
{
  "extensions": {
    "beacon": {
      "enabled": true,
      "url": "https://beacon-demo-api.melosso.com",
      "bucket": "newsletter_en",
      "permission": "newsletter",
      "apiKey": "Beacon-Api-Key",
      "language": "en"
    }
  }
}
```

The `bucket` holds the consent records. The `permission` is the switch inside it that a reader can turn off later, and it defaults to `newsletter`.

Beacon's demo publishes its access token, which is why it sits here in plain sight. For your own deployment, point `apiKey` at an environment variable instead. Teatime expands it when the file is read:

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

| Setting | Default | Notes |
| --- | --- | --- |
| `url` | none | Base URL of your Beacon instance |
| `bucket` | none | Where consent records land |
| `permission` | `newsletter` | The switch inside the bucket |
| `apiKey` | none | A literal key, or `${ENV_VAR}` |
| `apiKeyHeader` | `X-Api-Key` | Rarely worth changing |
| `language` | `en` | One of `en`, `de`, `fr`, `nl`, `pl`, `es` |
| `expiryDays` | Beacon's own | Lifetime of the preference link |
| `collectName` | `false` | Adds an optional name field |
| `customFields` | none | Extra values stored with each record |
| `skipPermissionUpdate` | `true` | Leaves existing records alone |

Custom fields ride along with every sign-up, which is handy for recording where someone subscribed:

```json [content/extensions.json]
{
  "extensions": {
    "beacon": {
      "enabled": true,
      "url": "https://beacon.example.com",
      "bucket": "newsletter_en",
      "apiKey": "${BEACON_API_KEY}",
      "customFields": { "source": "blog" }
    }
  }
}
```

### Listmonk

[Listmonk](https://listmonk.app) is a self-hosted newsletter manager. Sign-ups go to its public subscription endpoint, which takes no credential at all, so there is nothing here to keep secret:

```json [content/extensions.json]
{
  "extensions": {
    "listmonk": {
      "enabled": true,
      "url": "https://lists.example.com",
      "listUuid": "eb420c55-4cfb-4972-92ba-c93c34ba475d"
    }
  }
}
```

The UUID comes from the list's page in the Listmonk admin. Use `listUuids` with an array to subscribe to several at once. Double opt-in follows whatever the list itself is set to, and the form reads that from the reply.

One thing to check: the public endpoint is only open when "Enable public subscription page" is on in Listmonk's settings. With it off, every sign-up comes back as a rejection.

### Mailchimp

```json [content/extensions.json]
{
  "extensions": {
    "mailchimp": {
      "enabled": true,
      "listId": "a1b2c3d4e5",
      "apiKey": "${MAILCHIMP_API_KEY}",
      "status": "pending"
    }
  }
}
```

The `listId` is your audience id, found under Audience settings. The data centre is read from the key's own `-us21` suffix, so there is no separate host to configure.

`status` decides who sends the confirmation. Leave it at `pending` and Mailchimp emails the reader to confirm. Set it to `subscribed` and the address is added straight away, which is worth doing only when you have another basis for consent.

A Mailchimp key carries full account access and has no scoped variant. Teatime therefore refuses a literal key in `extensions.json` and accepts only a `${ENV_VAR}` reference, since your content folder is likely in version control.

### How It Stays Safe

Your reader's browser never sees your secrets. The form sends data to `/api/subscribe`, the server will pass your secrets in the backend. 

::: details How we protect extensions from abuse
Four things guard the form, and none of them need configuring:

* Settings security: Temporary access tokens returned during sign-up are thrown away instantly. This stops random people from accessing or changing someone else's account settings.
* Rate limits: Sign-ups are capped at 5 attempts per 10 minutes per address to block spam emails and prevent server abuse.
* Proof of work: Every sign-up carries a small puzzle the browser solves first, which takes a reader a moment and costs a bot the same on every single attempt. It follows the [ALTCHA](https://altcha.org) scheme and runs entirely on your own server. A reader with JavaScript turned off cannot sign up, which is the same position the form was already in.
* Protected opt-outs: Resubmitting an address won't overwrite existing settings or undo an opt-out (unless you explicitly set `"skipPermissionUpdate": false`).

The puzzles are signed with a key generated when Teatime starts, and each solution is accepted once. Restarting invalidates any puzzle still in flight, so a reader who left the form open across a restart may need to submit twice.
:::

## Placing the form

Drop a `newsletter` block wherever you would like the form, in a post or on a page:

````markdown
```newsletter
heading: Subscribe
intro: New posts, straight to your inbox. Unsubscribe whenever you like.
button: Sign me up
consent: true
```
````

Every field is optional. An empty block uses the interface text from `content/locale/`, so it translates with the rest of your site.

The form only appears when a newsletter extension is active, since a form with no back end would fail on submit. The `name` field follows the same rule and is left out unless your provider has `collectName` on, which would otherwise discard it.

| Field | Effect |
| --- | --- |
| `heading` | Replaces the default heading |
| `intro` | Replaces the line under it |
| `button` | Replaces the button label |
| `placeholder` | Replaces the email placeholder |
| `consent` | Shows a checkbox that has to be ticked |
| `name` | Shows an optional name field, if the provider has `collectName` on |

### Translating the form

The built-in text follows `content/locale/`, so an empty block is already translated into every locale Teatime ships with. Whatever you write in the block replaces it, and you can write it per locale:

````markdown
```newsletter
heading:
  en: Subscribe
  nl: Aanmelden
  de: Abonnieren
intro:
  en: New posts, straight to your inbox.
  nl: Nieuwe berichten, rechtstreeks in je inbox.
consent:
  en: Yes, send me new posts by email.
  nl: Ja, stuur mij nieuwe berichten per e-mail.
```
````

A plain string still applies everywhere, so mixing the two forms is fine. The block reads the site's active locale, falls back from `nl-BE` to `nl`, then to `en`, and finally to the built-in string, so a partial set of translations never leaves a field blank.

Both `consent` and `name` take text as well as `true`. Giving either one a string turns the field on and replaces its label in one go.

Here is that example, live:

```newsletter
heading: Subscribe
intro: New posts, straight to your inbox. Unsubscribe whenever you like.
consent: true
```

If your provider uses double opt-in, the reader is asked to confirm by email and the form will let the user know.

## Comments

[Remark42](https://github.com/umputun/remark42) is a small self-hosted comment server. It keeps threads in a single Bolt file beside its own binary, so nothing else has to be installed, and readers can sign in anonymously or through whichever provider you enable on the Remark42 side.

```json [content/extensions.json]
{
  "extensions": {
    "remark42": {
      "enabled": true,
      "url": "https://comments.example.com",
      "siteId": "remark",
      "theme": "auto",
      "maxShownComments": 15
    }
  }
}
```

Only `url` is needed. The `siteId` should match the `SITE` value your Remark42 runs with, and `remark` is its default on both sides.

| Field | Default | Effect |
| --- | --- | --- |
| `url` | none | Base URL of your Remark42 install |
| `siteId` | `remark` | Site id configured in Remark42 |
| `theme` | `auto` | `light`, `dark`, or `auto` to follow your site's toggle |
| `locale` | site language | Language for Remark42's own interface |
| `maxShownComments` | `15` | Comments loaded before the reader asks for more |

The thread is mounted under each post, below the previous and next links, and nowhere else. Pages, tag listings and the archive stay quiet. Each thread is keyed on the post's canonical URL, so moving a post to a new slug starts a fresh thread, which is worth knowing before you rename one that already has replies.

Leaving `theme` on `auto` lets Remark42 follow your reader's dark mode toggle: Teatime watches its own theme switch and passes the change along.

::: Note
Remark42's embed runs in an iframe, so its origin is added to `frame-src` alongside the usual fetch directives. Setting `ALLOWED_HOSTS` on the Remark42 side to your blog's origin is a good idea, since that is the check on which sites may embed your threads.
:::

## Analytics

Five providers are available, and enabling more than one at a time works fine:

```json [content/extensions.json]
{
  "extensions": {
    "matomo": {
      "enabled": true,
      "url": "https://analytics.example.com",
      "site_id": "1"
    },
    "plausible": {
      "enabled": true,
      "domain": "blog.example.com"
    },
    "medama": {
      "enabled": true,
      "url": "https://medama.example.com"
    },
    "goatcounter": {
      "enabled": true,
      "url": "https://you.goatcounter.com"
    },
    "liwan": {
      "enabled": true,
      "url": "https://liwan.example.com",
      "entity": "my-website"
    }
  }
}
```

::: details Matomo
`site_id` and `siteId` are both accepted for the same value, so a config copied from another generator usually works unchanged. Cookies are switched off through `disableCookies` unless you turn them back on.
:::

::: details Plausible
`url` defaults to the hosted service at `https://plausible.io` and `script` to the standard `script.js`. Setting either is worth it when you self-host, or when you want a variant such as `script.outbound-links.js`.
:::

::: details Medama
`url` is all it takes, pointing at your own install. The script and its endpoint are both read from it.
:::

::: details GoatCounter
`url` points at your own site, whether that is `https://you.goatcounter.com` or an install of your own. Both the counting script and the endpoint it reports to are read from it, so there is nothing further to configure.
:::

::: details Liwan
`url` points at your own Liwan instance, and `entity` names the site inside it. The tracker is served from your docs origin, so its reporting endpoint is set to `url` + `/api/event` for you.
:::

Cookies are worth a thought before you publish. Plausible, Medama and GoatCounter are cookieless by design, and Matomo is loaded with `disableCookies` on for the same reason. That usually keeps you clear of a consent banner. Set `"disableCookies": false` if your setup needs them, and adding a consent flow then becomes your call.

::: Warning
Each verified origin is folded into the page's `script-src`, `connect-src`, and `img-src` automatically, and the tracker tags carry the same nonce as the rest of the page. If you override `Docs:ContentSecurityPolicy` in `appsettings.json`, your policy is widened the same way.
:::