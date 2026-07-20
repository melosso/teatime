namespace Teatime.Services.Layout;

public static partial class LayoutProvider
{
    /// <summary>
    /// Styles and submit handler for the newsletter form, emitted only on pages that carry one.
    /// The form posts to Teatime's own /api/subscribe, so connect-src stays 'self'.
    /// </summary>
    private static string GetNewsletterAssets(string basePath, string? nonce)
    {
        var nonceAttr = GetNonceAttr(nonce);

        var css = $@"<style{nonceAttr}>
        .teatime-newsletter {{
            display: grid;
            gap: 0.75rem;
            margin: 2.5rem 0;
            padding: 1.75rem;
            border: 1px solid var(--border);
            border-radius: 12px;
            background: var(--accent-light, var(--sidebar-bg));
        }}
        /* Element + class keeps these ahead of the prose rules (.content.reading h2) the form sits inside. */
        .teatime-newsletter h2.newsletter-heading {{ margin: 0; font-size: 1.25rem; line-height: 1.3; }}
        .teatime-newsletter p.newsletter-intro {{ margin: 0; color: var(--text-muted); font-size: 0.95rem; }}
        .teatime-newsletter p.newsletter-status {{ margin: 0; font-size: 0.9rem; }}
        .teatime-newsletter label.newsletter-field {{ display: grid; gap: 0.35rem; margin: 0; }}
        .teatime-newsletter .newsletter-field span {{ font-size: 0.85rem; color: var(--text-muted); }}
        .teatime-newsletter input[type=""email""],
        .teatime-newsletter input[type=""text""] {{
            width: 100%;
            padding: 0.6rem 0.75rem;
            font: inherit;
            color: var(--text-color);
            background: var(--bg-color);
            border: 1px solid var(--border);
            border-radius: 8px;
        }}
        .teatime-newsletter input:focus-visible {{ outline: 2px solid var(--accent); outline-offset: 1px; }}
        .teatime-newsletter .newsletter-consent {{
            display: flex;
            gap: 0.5rem;
            align-items: flex-start;
            font-size: 0.9rem;
            color: var(--text-muted);
        }}
        .teatime-newsletter .newsletter-submit {{
            justify-self: start;
            padding: 0.6rem 1.25rem;
            font: inherit;
            color: var(--bg-color);
            background: var(--accent);
            border: 0;
            border-radius: 8px;
            cursor: pointer;
        }}
        .teatime-newsletter .newsletter-submit[disabled] {{ opacity: 0.6; cursor: progress; }}
        /* Settled after a successful sign-up: the fields grey out, the message stays readable. */
        .teatime-newsletter[data-done] .newsletter-field,
        .teatime-newsletter[data-done] .newsletter-consent,
        .teatime-newsletter[data-done] .newsletter-submit {{
            opacity: 0.55;
        }}
        .teatime-newsletter[data-done] .newsletter-submit {{ cursor: default; }}
        .teatime-newsletter[data-done] input {{ cursor: not-allowed; }}
        .teatime-newsletter p.newsletter-status:empty {{ display: none; }}
        .teatime-newsletter p.newsletter-status[data-state=""error""] {{ color: var(--alert-caution, #b3261e); }}
        .teatime-newsletter .newsletter-honeypot {{
            position: absolute;
            width: 1px;
            height: 1px;
            overflow: hidden;
            clip-path: inset(50%);
            white-space: nowrap;
        }}
        .teatime-newsletter .visually-hidden {{
            position: absolute;
            width: 1px;
            height: 1px;
            overflow: hidden;
            clip-path: inset(50%);
            white-space: nowrap;
        }}
    </style>";

        // Errors are reported through the status line, never through an alert or the console.
        var js = $@"<script{nonceAttr}>
        (function () {{
            var endpoint = '{basePath}/api/subscribe';
            var challengeUrl = '{basePath}/api/altcha';
            // ALTCHA proof of work: fetch a signed target, then walk the numbers until the hash matches.
            async function solve() {{
                var response = await fetch(challengeUrl, {{ headers: {{ 'Accept': 'application/json' }} }});
                var c = await response.json();
                var encoder = new TextEncoder();
                for (var n = 0; n <= c.maxnumber; n++) {{
                    var digest = await crypto.subtle.digest('SHA-256', encoder.encode(c.salt + n));
                    var hex = Array.from(new Uint8Array(digest)).map(function (b) {{
                        return b.toString(16).padStart(2, '0');
                    }}).join('');
                    if (hex === c.challenge) {{
                        return btoa(JSON.stringify({{
                            algorithm: c.algorithm, challenge: c.challenge,
                            number: n, salt: c.salt, signature: c.signature
                        }}));
                    }}
                }}
                throw new Error('no solution');
            }}
            document.querySelectorAll('form[data-newsletter]').forEach(function (form) {{
                var status = form.querySelector('.newsletter-status');
                var button = form.querySelector('.newsletter-submit');
                var consent = form.querySelector('input[name=""consent""]');
                form.addEventListener('submit', function (event) {{
                    event.preventDefault();
                    if (button.disabled) return;
                    var email = form.querySelector('input[name=""email""]');
                    if (!email.value.trim() || !email.checkValidity()) {{
                        show(form.dataset.invalid, 'error');
                        email.focus();
                        return;
                    }}
                    if (consent && !consent.checked) {{
                        show(form.dataset.consentRequired, 'error');
                        consent.focus();
                        return;
                    }}
                    var name = form.querySelector('input[name=""name""]');
                    var website = form.querySelector('input[name=""website""]');
                    button.disabled = true;
                    button.textContent = button.dataset.sending;
                    show('', null);
                    solve().then(function (altcha) {{
                    return fetch(endpoint, {{
                        method: 'POST',
                        headers: {{ 'Content-Type': 'application/json', 'Accept': 'application/json' }},
                        body: JSON.stringify({{
                            email: email.value.trim(),
                            name: name ? name.value.trim() : null,
                            website: website ? website.value : '',
                            consent: consent ? consent.checked : true,
                            altcha: altcha
                        }})
                    }}); }}).then(function (response) {{
                        if (response.status === 429) return {{ ok: false, message: form.dataset.throttled }};
                        return response.json().catch(function () {{ return {{ ok: false, message: form.dataset.error }}; }});
                    }}).then(function (result) {{
                        if (result && result.ok) {{
                            form.reset();
                            show(result.message, 'ok');
                            settle();
                        }} else {{
                            show((result && result.message) || form.dataset.error, 'error');
                        }}
                    }}).catch(function () {{
                        show(form.dataset.error, 'error');
                    }}).finally(function () {{
                        if (form.hasAttribute('data-done')) return;
                        button.disabled = false;
                        button.textContent = button.dataset.label;
                    }});
                }});
                // Closes the form for this page view only. Nothing is stored, because a browser
                // having submitted once is not the same as this reader being subscribed.
                function settle() {{
                    form.setAttribute('data-done', '');
                    form.querySelectorAll('input, button').forEach(function (field) {{
                        field.disabled = true;
                    }});
                    button.textContent = button.dataset.label;
                }}
                function show(message, state) {{
                    status.textContent = message || '';
                    if (state) {{ status.setAttribute('data-state', state); }}
                    else {{ status.removeAttribute('data-state'); }}
                }}
            }});
        }})();
    </script>";

        return css + js;
    }
}
