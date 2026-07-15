namespace Teatime.Services.Layout;

public static partial class LayoutProvider
{
    private static string GetStyles(string darkModeMediaQuery, string basePath, string? nonce = null) => $@"    <style{GetNonceAttr(nonce)}>
        @font-face {{
            font-family: ""Inter"";
            font-style: normal;
            font-weight: 100 900;
            font-display: swap;
            src: url(""{basePath}/fonts/Inter.woff2"") format(""woff2"");
        }}
        :root {{
            color-scheme: light;
            --bg-color: #FBFAF7;
            --sidebar-bg: #F3F1EA;
            --text-color: #1B1D1A;
            --text-muted: #5A5F58;
            --accent: #2E4A36;
            --accent-light: #E7ECE7;
            --border: rgba(20, 24, 20, 0.09);
            --code-bg: #F3F1EA;
            --alert-note: #0969da;
            --alert-tip: #1a7f37;
            --alert-important: #8250df;
            --alert-warning: #9a6700;
            --alert-caution: #cf222e;
            --font-sans: ""Inter"", system-ui, -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, sans-serif;
            --font-display: ""Inter Display"", ""Inter"", system-ui, -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, sans-serif;
            --font-mono: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace;
            --measure: 680px;
            --measure-wide: 760px;

            --search-bg: var(--sidebar-bg);
            --search-border: var(--border);
            --search-hover-border: var(--accent);
            --nav-hover-bg: var(--code-bg);
            --nav-active-bg: var(--accent-light);
            --overlay-bg: rgba(0, 0, 0, 0.5);
            --code-button-bg: var(--bg-color);
            --code-button-border: var(--border);
            --code-button-hover: var(--accent);
            --shadow-md: 0 8px 24px rgba(0, 0, 0, 0.12);
            --shadow-lg: 0 24px 64px rgba(0, 0, 0, 0.3);
        }}
        {darkModeMediaQuery}
        * {{
            box-sizing: border-box;
            margin: 0;
            padding: 0;
        }}
        html, body {{
            /* `clip` not `hidden` -- `hidden` forces overflow-y to `auto` too, turning body
               into a scroll container and breaking position: sticky on the sidebars. */
            overflow-x: clip;
        }}
        html {{
            /* Reserve the scrollbar gutter so centered content does not shift between
               pages that scroll and pages that do not. */
            scrollbar-gutter: stable;
        }}
        body {{
            font-family: var(--font-sans);
            background-color: var(--bg-color);
            color: var(--text-color);
            line-height: 1.6;
            -webkit-font-smoothing: antialiased;
            transition: background-color 0.15s ease, color 0.15s ease;
        }}
        #scroll-indicator {{
            position: fixed; top: 0; left: 0; height: 3px;
            background-color: var(--accent); width: 0%; z-index: 1101;
            transition: width 0.15s ease;
        }}
        :focus-visible {{
            outline: 2px solid var(--accent);
            outline-offset: 2px;
        }}
        .skip-link {{
            position: absolute; top: 0; left: 0; z-index: 1100;
            width: 1px; height: 1px; overflow: hidden;
            clip-path: inset(50%); white-space: nowrap;
            background: var(--accent); color: #fff; padding: 0.75rem 1.25rem;
            border-radius: 0 0 6px 0; text-decoration: none; font-size: 0.9rem;
        }}
        .skip-link:focus {{
            width: auto; height: auto; overflow: visible;
            clip-path: none; white-space: normal;
        }}
        .no-theme-transition, .no-theme-transition * {{
            transition: none !important;
        }}
        :root {{
            --topbar-height: 57px;
            --promo-bg: var(--accent-light);
            --promo-text: var(--accent);
        }}
        /* z-index scale: sidebar-overlay 1001 < topbar 1002 < mobile drawer 1003 < skip-link 1100
           < scroll-indicator 1101, so the indicator stays visible above the opaque topbar. */
        .icon-btn {{
            display: inline-flex; align-items: center; justify-content: center;
            width: 36px; height: 36px; border-radius: 6px; border: none;
            background: transparent; color: var(--text-muted); cursor: pointer;
            flex-shrink: 0; text-decoration: none;
            transition: color 0.15s ease, background-color 0.15s ease;
        }}
        .icon-btn:hover {{
            color: var(--accent);
            background-color: var(--code-bg);
        }}
        .icon-btn svg {{
            width: 18px;
            height: 18px;
        }}
        .topbar {{
            display: flex; align-items: center; justify-content: space-between;
            height: var(--topbar-height); padding: 0 1.5rem;
            background-color: var(--bg-color); border-bottom: 1px solid var(--border);
            position: sticky; top: 0; z-index: 1002;
        }}
        .topbar-left {{
            display: flex; align-items: center; gap: 0.75rem;
        }}
        .topbar-right {{
            display: flex; align-items: center; gap: 1rem;
        }}
        .top-nav {{
            position: absolute; left: 50%; transform: translateX(-50%);
            display: flex; align-items: center; gap: 1.5rem; height: 100%;
        }}
        .top-nav-item {{
            display: flex;
            align-items: center;
            height: 100%;
            position: relative;
        }}
        .top-nav-link {{
            display: inline-flex; align-items: center; gap: 0.3rem;
            font-size: 0.9rem; font-weight: 500; color: var(--text-muted);
            text-decoration: none; background: none; border: none; cursor: pointer;
            padding: 0; font-family: inherit;
        }}
        .top-nav-link:hover, .top-nav-link.active {{
            color: var(--accent);
        }}
        .top-nav-chevron {{
            width: 14px;
            height: 14px;
            transition: transform 0.15s ease;
        }}
        .top-nav-item.has-dropdown:hover .top-nav-chevron,
        .top-nav-item.has-dropdown:focus-within .top-nav-chevron {{
            transform: rotate(180deg);
        }}
        .top-nav-dropdown-menu {{
            display: none; position: absolute; top: 100%; left: 0; min-width: 180px;
            background-color: var(--bg-color); border: 1px solid var(--border); border-radius: 8px;
            padding: 0.4rem; box-shadow: var(--shadow-md); z-index: 1003;
        }}
        .top-nav-item.has-dropdown:hover .top-nav-dropdown-menu,
        .top-nav-item.has-dropdown:focus-within .top-nav-dropdown-menu {{
            display: block;
        }}
        .top-nav-dropdown-link {{
            display: flex; align-items: center; justify-content: space-between; gap: 0.5rem;
            padding: 0.45rem 0.6rem; border-radius: 6px;
            font-size: 0.875rem; color: var(--text-color); text-decoration: none;
        }}
        .top-nav-dropdown-link:hover {{
            background-color: var(--code-bg); color: var(--accent);
        }}
        .layout {{
            display: grid;
            grid-template-columns: 270px 1fr 270px;
            min-height: calc(100vh - var(--topbar-height));
        }}
        .layout.no-left-sidebar {{
            grid-template-columns: 1fr 270px;
        }}
        @media (min-width: 769px) {{
            .layout.no-left-sidebar > .sidebar-left {{
                display: none;
            }}
        }}
        .sidebar-left {{
            background-color: var(--sidebar-bg);
            border-right: 1px solid var(--border);
            padding: 2.75rem 1.75rem;
            position: sticky; top: var(--topbar-height); align-self: start;
            height: calc(100vh - var(--topbar-height)); overflow-y: auto;
        }}
        .brand a {{
            font-size: 1.1rem; font-weight: 600; letter-spacing: -0.02em;
            color: var(--text-color); text-decoration: none;
        }}
        .brand a:hover {{
            color: var(--accent);
        }}
        .brand img {{
            height: 22px; width: auto; vertical-align: middle; margin-right: 0.75rem;
        }}
        .theme-toggle {{
            position: relative; flex-shrink: 0; width: 48px; height: 28px;
            border: 1px solid var(--border); border-radius: 999px; padding: 0;
            background-color: var(--code-bg); cursor: pointer;
            transition: background-color 0.15s ease, border-color 0.15s ease;
        }}
        .theme-toggle:hover {{
            border-color: var(--accent);
        }}
        .theme-toggle-thumb {{
            position: absolute; top: 3px; left: 3px; width: 20px; height: 20px;
            border-radius: 50%; background-color: var(--bg-color);
            box-shadow: 0 1px 2px rgba(0, 0, 0, 0.25);
            display: flex; align-items: center; justify-content: center;
            transition: transform 0.2s cubic-bezier(0.16, 1, 0.3, 1);
        }}
        .theme-toggle-thumb svg {{
            width: 13px;
            height: 13px;
            color: var(--accent);
        }}
        .theme-toggle-thumb .icon-moon {{
            display: none;
        }}
        :root[data-theme=""dark""] .theme-toggle-thumb {{
            transform: translateX(20px);
        }}
        :root[data-theme=""dark""] .theme-toggle-thumb .icon-sun {{
            display: none;
        }}
        :root[data-theme=""dark""] .theme-toggle-thumb .icon-moon {{
            display: block;
        }}
        @media (prefers-color-scheme: dark) {{
            :root:not([data-theme=""light""]) .theme-toggle-thumb {{
                transform: translateX(20px);
            }}
            :root:not([data-theme=""light""]) .theme-toggle-thumb .icon-sun {{
                display: none;
            }}
            :root:not([data-theme=""light""]) .theme-toggle-thumb .icon-moon {{
                display: block;
            }}
        }}
        .sr-only {{
            position: absolute; width: 1px; height: 1px; padding: 0; margin: -1px;
            overflow: hidden; clip: rect(0, 0, 0, 0); white-space: nowrap; border: 0;
        }}
        .search-trigger {{
            display: flex; align-items: center; gap: 0.55rem;
            margin-left: 1rem; padding: 0.4rem 0.65rem;
            border: 1px solid var(--search-border); border-radius: 8px;
            background-color: var(--search-bg); color: var(--text-muted);
            font-family: inherit; font-size: 0.85rem; cursor: pointer;
            transition: border-color 0.15s ease, color 0.15s ease;
        }}
        .search-trigger:hover {{
            border-color: var(--search-hover-border);
            color: var(--text-color);
        }}
        .search-trigger svg {{
            width: 16px;
            height: 16px;
            flex-shrink: 0;
        }}
        .search-trigger-kbd {{
            font-family: var(--font-sans); font-size: 0.7rem;
            font-weight: 400; letter-spacing: 0.02em;
            border: 1px solid var(--border); border-radius: 4px;
            padding: 0.1rem 0.35rem; background-color: var(--bg-color); color: var(--text-muted);
            pointer-events: none;
            user-select: none;
        }}
        .search-overlay {{
            position: fixed; inset: 0; z-index: 1200;
            background-color: var(--overlay-bg);
            display: flex; align-items: center; justify-content: center;
            padding: 1.5rem; opacity: 0; transition: opacity 0.15s ease;
        }}
        .search-overlay[hidden] {{
            display: none;
        }}
        .search-overlay.open {{
            opacity: 1;
        }}
        .search-modal {{
            width: 100%; max-width: 720px; max-height: 80vh;
            background-color: var(--bg-color); border: 1px solid var(--border); border-radius: 12px;
            box-shadow: var(--shadow-lg);
            display: flex; flex-direction: column; overflow: hidden;
            transform: translateY(-12px) scale(0.98);
            transition: transform 0.15s ease;
        }}
        .search-overlay.open .search-modal {{
            transform: translateY(0) scale(1);
        }}
        .search-modal-header {{
            display: flex; align-items: center; gap: 0.75rem;
            padding: 1rem 1.25rem; border-bottom: 1px solid var(--border); flex-shrink: 0;
        }}
        .search-modal-header > svg {{
            width: 20px;
            height: 20px;
            color: var(--text-muted);
            flex-shrink: 0;
        }}
        .search-modal-input {{
            flex: 1; min-width: 0; border: none; outline: none; background: transparent;
            color: var(--text-color); font-size: 1.05rem; font-family: var(--font-sans);
        }}
        .search-modal-close {{
            flex-shrink: 0;
        }}
        .search-modal-results {{
            flex: 1;
            overflow-y: auto;
            padding: 0.5rem;
        }}
        .search-result-item {{
            display: block; padding: 0.7rem 0.9rem; border-radius: 8px;
            text-decoration: none; transition: background-color 0.1s ease;
        }}
        .search-result-item.active, .search-result-item:hover {{
            background-color: var(--accent-light);
        }}
        .search-result-title {{
            font-weight: 500;
            color: var(--text-color);
            font-size: 0.9rem;
        }}
        .search-result-excerpt {{
            font-size: 0.8rem;
            color: var(--text-muted);
            margin-top: 0.2rem;
        }}
        .search-highlight {{
            background-color: var(--accent-light); color: var(--accent);
            border-radius: 3px; padding: 0 0.15em; font-weight: 600;
        }}
        .search-result-empty {{
            color: var(--text-muted);
            font-size: 0.85rem;
            padding: 1rem;
            text-align: center;
        }}
        .search-group + .search-group {{
            border-top: 1px solid var(--border); margin-top: 0.25rem; padding-top: 0.25rem;
        }}
        .search-group-label {{
            font-size: 0.7rem; text-transform: uppercase; letter-spacing: 0.06em;
            color: var(--text-muted); font-weight: 600; padding: 0.5rem 0.9rem 0.25rem;
        }}
        .search-hit-media {{
            display: flex; align-items: center; gap: 0.65rem;
        }}
        .search-hit-media .search-result-title {{
            flex: 1; min-width: 0;
        }}
        .search-hit-avatar {{
            width: 30px; height: 30px; border-radius: 50%; object-fit: cover;
            flex-shrink: 0; font-size: 0.8rem;
        }}
        .search-hit-tag {{
            display: inline-grid; place-items: center; width: 30px; height: 30px; flex-shrink: 0;
            border-radius: 8px; background: var(--accent-light); color: var(--accent); font-weight: 700;
        }}
        .search-hit-count {{
            flex-shrink: 0; font-size: 0.78rem; color: var(--text-muted); font-variant-numeric: tabular-nums;
        }}
        .DocSearch-Commands {{
            display: flex; gap: 1.25rem; padding: 0.6rem 1.25rem; margin: 0; list-style: none;
            border-top: 1px solid var(--border); font-size: 0.75rem; color: var(--text-muted);
            flex-shrink: 0;
        }}
        .DocSearch-Commands li {{
            display: flex;
            align-items: center;
            gap: 0.4rem;
        }}
        .DocSearch-Commands-Key {{
            display: inline-flex; align-items: center; justify-content: center;
            font-family: var(--font-mono); border: 1px solid var(--border); border-radius: 4px;
            padding: 0.1rem 0.3rem; background-color: var(--code-bg); min-width: 1.4rem; height: 1.4rem;
        }}
        .DocSearch-Commands-Key svg {{
            width: 14px;
            height: 14px;
        }}
        .DocSearch-Escape-Key {{
            font-size: 0.7rem;
            line-height: 1;
        }}
        @media (max-width: 768px) {{
            .search-modal-close {{
                width: 44px;
                height: 44px;
            }}
            .search-overlay {{
                padding: 0;
            }}
            .search-modal {{
                max-width: 100%;
                max-height: 100%;
                height: 100%;
                height: 100dvh;
                border-radius: 0;
            }}
            .DocSearch-Commands {{
                flex-wrap: wrap;
                row-gap: 0.4rem;
            }}
        }}
        .nav-group {{
            margin-bottom: 2.25rem;
        }}
        .nav-group-title {{
            font-size: 0.75rem; text-transform: uppercase; letter-spacing: 0.05em;
            color: var(--text-muted); margin-bottom: 1rem; font-weight: 600;
        }}
        .nav-list {{
            list-style: none;
        }}
        .nav-item a {{
            display: block; padding: 0.55rem 0.8rem; line-height: 1.4;
            color: var(--text-muted); text-decoration: none; font-size: 0.9rem;
            border-radius: 6px; margin-left: -0.8rem;
            transition: color 0.15s ease, background-color 0.15s ease;
        }}
        .nav-item a:hover {{
            color: var(--text-color); background-color: var(--nav-hover-bg);
        }}
        .nav-item.active a {{
            color: var(--accent); background-color: var(--nav-active-bg); font-weight: 500;
        }}
        /* .sidebar-group-title stays a plain <div>; <summary> can't be fully de-styled across
           engines, so summary.sidebar-group-summary just wraps it as a click target. */
        /* Each .sidebar-group-items adds 0.9rem left padding; depth compounds via nesting,
           no per-level overrides needed. Root list gets no padding. */
        .sidebar-tree {{
            font-size: 0.9rem;
        }}
        .sidebar-group {{
            margin-bottom: 0.25rem;
        }}
        .sidebar-group-summary {{
            display: block; list-style: none; cursor: pointer;
        }}
        .sidebar-group-summary::-webkit-details-marker {{
            display: none;
        }}
        .sidebar-group-summary::marker {{
            content: """";
        }}
        .sidebar-group.no-caret > .sidebar-group-title {{
            cursor: default;
        }}
        .sidebar-group-title {{
            display: flex; align-items: center; gap: 0.4rem;
            padding: 0.5rem 0.8rem; border-radius: 6px;
            user-select: none; transition: background-color 0.15s ease;
        }}
        .sidebar-group-summary:hover .sidebar-group-title {{
            background-color: var(--code-bg);
        }}
        /* Only the caret should distinguish colapsible from static groups, not typography */
        .sidebar-group-title h2, .sidebar-group-title h3 {{
            font-size: 0.75rem; text-transform: uppercase; letter-spacing: 0.05em;
            color: var(--text-muted); font-weight: 600; flex: 1; margin: 0;
        }}
        /* Ancestors get a text-color cue only; the highlighted background is reserved for the
           one active leaf link, so a nested active page doesn't stack backgrounds at every level. */
        .sidebar-group-title.has-active h2, .sidebar-group-title.has-active h3 {{
            color: var(--accent);
        }}
        .caret-icon {{
            display: inline-flex; flex-shrink: 0; width: 16px; height: 16px;
            color: var(--text-muted); transition: transform 0.2s ease;
        }}
        .caret-icon svg {{
            width: 100%;
            height: 100%;
        }}
        details[open] > .sidebar-group-summary .caret-icon {{
            transform: rotate(90deg);
        }}
        .sidebar-group-items {{
            padding-left: 0.9rem;
            margin-bottom: 0.5rem;
        }}
        .sidebar-tree > .sidebar-group > .sidebar-group-items {{
            padding-left: 0;
        }}
        .sidebar-link {{
            margin-bottom: 0.1rem;
        }}
        /* Top-level entries get a divider between sections; scoped to direct children of
           .sidebar-tree so items inside a group stay tightly packed. */
        .sidebar-tree > .sidebar-group + .sidebar-group,
        .sidebar-tree > .sidebar-group + .sidebar-link,
        .sidebar-tree > .sidebar-link + .sidebar-group,
        .sidebar-tree > .sidebar-link + .sidebar-link {{
            border-top: 1px solid var(--border);
            padding-top: 0.75rem;
            margin-top: 0.75rem;
        }}
        .sidebar-link a {{
            display: block; 
            padding: 0.45rem 0.8rem; 
            line-height: 1.4;
            color: var(--text-muted); 
            text-decoration: none; font-size: 0.875rem;
            border-radius: 6px; 
            transition: color 0.15s ease, background-color 0.15s ease;
        }}
        .sidebar-link a:hover {{
            color: var(--text-color);
            background-color: var(--nav-hover-bg);
        }}
        .sidebar-link.is-active a {{
            color: var(--accent); background-color: var(--nav-active-bg); font-weight: 500;
        }}
        .main-container {{
            padding: 3rem 4rem;
            max-width: 800px; justify-self: center; width: 100%;
            min-width: 0;
        }}
        .breadcrumb {{
            display: flex; align-items: center; gap: 0.4rem;
            margin-bottom: 1.5rem; font-size: 0.8rem; flex-wrap: wrap;
        }}
        .breadcrumb a {{
            color: var(--text-muted); text-decoration: none;
            transition: color 0.15s ease;
        }}
        .breadcrumb a:hover {{
            color: var(--accent);
        }}
        .breadcrumb .separator {{
            color: var(--text-muted);
        }}
        .breadcrumb .crumb-text {{
            color: var(--text-muted);
        }}
        .breadcrumb .current {{
            color: var(--text-color);
            font-weight: 500;
        }}
        .content h1 {{
            font-size: 2.2rem; font-weight: 600; letter-spacing: -0.03em;
            margin-bottom: 1rem; scroll-margin-top: calc(var(--topbar-height) + 1rem);
        }}
        .content h2, .content h3, .content h4, .content h5, .content h6 {{
            position: relative;
        }}
        /* Jumping to a heading or footnote via URL hash (TOC links, footnote refs/back-refs)
           should visibly show where you landed, not just scroll there silently. */
        .content h1:target, .content h2:target, .content h3:target,
        .content h4:target, .content h5:target, .content h6:target {{
            animation: teatime-target-flash 2s ease-out;
        }}
        .content a.footnote-ref:target,
        .content a.footnote-back-ref:target {{
            background-color: var(--accent-light); outline: 2px solid var(--accent);
            border-radius: 4px; padding: 0 0.2em; scroll-margin-top: calc(var(--topbar-height) + 1rem);
        }}
        .content .footnotes li:target {{
            background-color: var(--accent-light); outline: 2px solid var(--accent);
            border-radius: 6px; padding: 0.25rem 0.6rem; margin-left: -0.6rem;
            scroll-margin-top: calc(var(--topbar-height) + 1rem);
        }}
        @keyframes teatime-target-flash {{
            0%, 40% {{
                background-color: var(--accent-light);
            }}
            100% {{
                background-color: transparent;
            }}
        }}
        @media (prefers-reduced-motion: reduce) {{
            .content h1:target, .content h2:target, .content h3:target,
            .content h4:target, .content h5:target, .content h6:target {{
                animation: none; background-color: var(--accent-light);
            }}
        }}
        .header-anchor {{
            position: absolute; left: -1.2rem; top: 0; bottom: 0;
            display: inline-flex; align-items: center;
            opacity: 0; text-decoration: none; font-weight: 400;
            color: var(--text-muted);
            transition: opacity 0.15s ease, color 0.15s ease;
        }}
        .header-anchor::before {{
            content: ""#"";
        }}
        .header-anchor:hover {{
            color: var(--accent);
        }}
        .content h2:hover .header-anchor, .content h3:hover .header-anchor,
        .content h4:hover .header-anchor, .content h5:hover .header-anchor,
        .content h6:hover .header-anchor, .header-anchor:focus {{
            opacity: 1;
        }}
        .content h2 {{
            font-size: 1.4rem; font-weight: 500; letter-spacing: -0.02em;
            margin-top: 2.5rem; margin-bottom: 1rem; padding-bottom: 0.3rem;
            border-bottom: 1px solid var(--border); scroll-margin-top: calc(var(--topbar-height) + 1rem);
        }}
        .content p {{
            color: var(--text-color); margin-bottom: 1.25rem;
            text-decoration-color: var(--border); text-underline-offset: 2px;
        }}
        .content a {{
            color: var(--accent); text-decoration: underline;
            text-decoration-color: var(--border); text-underline-offset: 2px;
            transition: text-decoration-color 0.15s ease;
        }}
        .content a:hover {{
            text-decoration-color: var(--accent);
        }}
        .content ul, .content ol {{
            padding-left: 1.5rem; margin-bottom: 1.25rem;
        }}
        .content li {{
            margin-bottom: 0.4rem;
        }}
        .content li > ul, .content li > ol {{
            margin-top: 0.4rem; margin-bottom: 0;
        }}
        .content hr {{
            border: none; border-top: 1px solid var(--border); margin: 2.5rem 0;
        }}
        .content h3 {{
            font-size: 1.15rem; font-weight: 500; letter-spacing: -0.01em;
            margin-top: 2rem; margin-bottom: 0.75rem; scroll-margin-top: calc(var(--topbar-height) + 1rem);
        }}
        .content h4 {{
            font-size: 1rem; font-weight: 500;
            margin-top: 1.5rem; margin-bottom: 0.5rem; scroll-margin-top: calc(var(--topbar-height) + 1rem);
        }}
        .content h5, .content h6 {{
            font-size: 0.9rem; font-weight: 600;
            margin-top: 1.25rem; margin-bottom: 0.5rem; scroll-margin-top: calc(var(--topbar-height) + 1rem);
        }}
        pre {{
            background-color: var(--code-bg);
            border: 1px solid var(--border);
            border-radius: 8px;
            padding: 1.25rem;
            overflow-x: auto;
            font-family: var(--font-mono);
            font-size: 0.85rem;
            margin: 1.5rem 0;
        }}
        code {{
            font-family: var(--font-mono);
            background-color: var(--code-bg);
            padding: 0.2rem 0.4rem;
            border-radius: 4px;
            font-size: 0.85rem;
        }}
        pre code {{
            padding: 0; background-color: transparent; border-radius: 0;
        }}
        dt {{
            font-weight: 700;
        }}
        dd {{
            margin-bottom: .5rem;
            margin-left: 0;
        }}
        .content h1 code, .content h2 code, .content h3 code,
        .content h4 code, .content h5 code, .content h6 code {{
            background: none; padding: 0; border-radius: 0; font-size: inherit;
        }}
        /* Fenced code block chrome */
        .content div[class^=""language-""] {{
            position: relative;
            margin: 1.5rem 0;
            background-color: var(--code-bg);
            border: 1px solid var(--border);
            border-radius: 8px;
        }}
        .content div[class^=""language-""] pre {{
            margin: 0; border: none; border-radius: 0; padding-top: 2rem;
        }}
        /* Lang badge top-left; Copy/Download buttons (injected by JS) occupy top-right. */
        .content div[class^=""language-""] .lang {{
            position: absolute; top: 0.6rem; left: 1rem; right: auto;
            font-size: 0.7rem; color: var(--text-muted);
            font-family: var(--font-sans); text-transform: lowercase;
            user-select: none; z-index: 1;
        }}
        .content div[class^=""language-""] button.copy {{
            display: none;
        }}
        .content div[class^=""language-""] .code-title {{
            padding: 0.6rem 1rem; font-size: 0.8rem; font-family: var(--font-mono);
            color: var(--text-muted); border-bottom: 1px solid var(--border);
        }}
        .content div[class^=""language-""].has-title .lang {{
            display: none;
        }}
        .content div[class^=""language-""].has-title pre {{
            padding-top: 0.75rem;
        }}
        /* Resolves the --shiki-light/dark vars TextMateSyntaxHighlighter writes per token,
           same prefers-color-scheme + [data-theme] override pattern as the rest of the theme. */
        .shiki, .shiki span {{
            color: var(--shiki-light);
        }}
        .shiki {{
            background-color: var(--shiki-light-bg);
        }}
        @media (prefers-color-scheme: dark) {{
            :root:not([data-theme=""light""]) .shiki, :root:not([data-theme=""light""]) .shiki span {{
                color: var(--shiki-dark);
            }}
            :root:not([data-theme=""light""]) .shiki {{
                background-color: var(--shiki-dark-bg);
            }}
        }}
        :root[data-theme=""dark""] .shiki, :root[data-theme=""dark""] .shiki span {{
            color: var(--shiki-dark);
        }}
        :root[data-theme=""dark""] .shiki {{
            background-color: var(--shiki-dark-bg);
        }}
        @media (prefers-color-scheme: dark) {{
            :root:not([data-theme=""light""]) .tab-icon {{ filter: brightness(0) invert(1); }}
        }}
        :root[data-theme=""dark""] .tab-icon {{ filter: brightness(0) invert(1); }}
        .content .line {{
            display: inline-block;
            width: 100%;
            min-height: 1.4em;
        }}
        .content .line.highlighted {{
            background-color: var(--accent-light);
            margin: 0 -1.25rem; padding: 0 1.25rem;
            box-shadow: 2px 0 0 var(--accent) inset;
        }}
        .content .line.highlighted.error {{
            box-shadow: 2px 0 0 var(--alert-caution) inset;
        }}
        .content .line.highlighted.warning {{
            box-shadow: 2px 0 0 var(--alert-warning) inset;
        }}
        .content .line.diff {{
            margin: 0 -1.25rem;
            padding: 0 1.25rem;
        }}
        .content .line.diff.add {{
            background-color: color-mix(in srgb, var(--alert-tip) 15%, transparent);
        }}
        .content .line.diff.remove {{
            background-color: color-mix(in srgb, var(--alert-caution) 15%, transparent);
            opacity: 0.7;
        }}
        .content div[class^=""language-""].has-focused-lines .line {{
            opacity: 0.5;
            filter: blur(0.06rem);
            transition: opacity 0.2s, filter 0.2s;
        }}
        .content div[class^=""language-""].has-focused-lines .line.has-focus {{
            opacity: 1;
            filter: none;
        }}
        .content .line-numbers-mode pre {{
            padding-left: 2.5rem;
        }}
        .content .line-numbers-wrapper {{
            position: absolute; top: 2rem; left: 0; width: 2rem;
            text-align: right; color: var(--text-muted); font-family: var(--font-mono);
            font-size: 0.85rem; line-height: 1.6; user-select: none;
        }}
        /* Custom containers: ::: tip / warning / danger / info / details */
        .content .custom-block {{
            margin: 1rem 0; padding: 1rem; border-radius: 8px;
            line-height: 1.5; font-size: 0.9rem; color: var(--text-muted);
            background-color: var(--accent-light);
        }}
        .content .custom-block p:not(.custom-block-title) {{
            margin: 0;
        }}
        .content .custom-block.tip {{
            color: var(--alert-tip);
            background-color: color-mix(in srgb, var(--alert-tip) 10%, var(--bg-color));
        }}
        .content .custom-block.info {{
            color: var(--alert-note);
            background-color: color-mix(in srgb, var(--alert-note) 10%, var(--bg-color));
        }}
        .content .custom-block.warning {{
            color: var(--alert-warning);
            background-color: color-mix(in srgb, var(--alert-warning) 10%, var(--bg-color));
        }}
        .content .custom-block.danger {{
            color: var(--alert-caution);
            background-color: color-mix(in srgb, var(--alert-caution) 10%, var(--bg-color));
        }}
        .content .custom-block-title {{
            font-weight: 700;
            margin: 0 0 0.5rem;
        }}
        .content .custom-block a {{
            color: inherit; font-weight: 600; text-decoration: underline;
            text-decoration-color: currentColor; text-underline-offset: 2px;
        }}
        .content .custom-block a:hover {{
            opacity: 0.75;
        }}
        .content details.custom-block summary {{
            font-weight: 700;
            cursor: pointer;
            margin: 0 0 0.5rem;
        }}
        .content details.custom-block:not([open]) {{
            padding-bottom: 0;
        }}
        .content details.custom-block:not([open]) summary {{
            margin-bottom: 0;
        }}
        /* code-group tabs */
        .content .teatime-code-group {{
            margin: 1.5rem 0;
        }}
        .content .teatime-code-group .tabs {{
            display: flex; gap: 0.25rem; border-bottom: 1px solid var(--border);
        }}
        .content .teatime-code-group .tabs input {{
            display: none;
        }}
        .content .teatime-code-group .tabs label {{
            display: inline-flex; align-items: center; gap: 0.35rem;
            padding: 0.5rem 0.9rem; font-size: 0.85rem; color: var(--text-muted);
            cursor: pointer; border-bottom: 2px solid transparent; margin-bottom: -1px;
        }}
        .content .teatime-code-group .tabs .tab-icon {{
            width: 14px;
            height: 14px;
            flex-shrink: 0;
        }}
        .content .teatime-code-group .blocks > div[class^=""language-""] {{
            display: none;
            margin-top: 0;
            border-top-left-radius: 0;
            border-top-right-radius: 0;
        }}
        .content .teatime-code-group .blocks > div[class^=""language-""].active {{
            display: block;
        }}
        .content .teatime-code-group .tabs label.active-tab {{
            color: var(--text-color);
            border-bottom-color: var(--accent);
        }}
        .table-wrapper {{
            overflow-x: auto; -webkit-overflow-scrolling: touch;
            margin: 1.5rem 0; border-radius: 6px;
        }}
        .task-list-item input[type=""checkbox""] {{
            width: 1em; height: 1em; margin: 0 0.4em 0 0;
            vertical-align: middle;
        }}
        .content table {{
            width: 100%; border-collapse: collapse;
            font-size: 0.875rem;
        }}
        .content th, .content td {{
            padding: 0.6rem 1rem; border: 1px solid var(--border);
            text-align: left; vertical-align: top;
        }}
        .content th {{
            background-color: var(--accent-light); font-weight: 600;
            color: var(--text-color);
        }}
        .content tr:nth-child(even) {{
            background-color: var(--code-bg);
        }}
        .content tr:nth-child(even) code {{
            background-color: color-mix(in srgb, var(--accent) 8%, var(--code-bg));
        }}
        .code-block-wrapper {{
            position: relative;
        }}
        .code-block-buttons {{
            position: absolute; top: 0.5rem; right: 0.5rem;
            display: flex; gap: 0.25rem; opacity: 0;
            transition: opacity 0.15s ease;
        }}
        .code-block-wrapper:hover .code-block-buttons,
        .code-block-wrapper:focus-within .code-block-buttons {{
            opacity: 1;
        }}
        .code-block-buttons button {{
            background: var(--code-button-bg); border: 1px solid var(--code-button-border);
            border-radius: 6px; width: 32px; height: 32px;
            display: flex; align-items: center; justify-content: center;
            color: var(--text-muted); cursor: pointer; flex-shrink: 0;
            transition: color 0.15s ease, border-color 0.15s ease;
        }}
        .code-block-buttons button svg {{
            display: block; pointer-events: none;
        }}
        .code-block-buttons button:hover {{
            color: var(--code-button-hover); border-color: var(--code-button-hover);
        }}
        .code-block-buttons button.copied {{
            color: var(--code-button-hover); border-color: var(--code-button-hover);
        }}
        .code-block-buttons button.failed {{
            opacity: 0.5;
        }}
        .markdown-alert {{
            padding: 0.75rem 1rem; margin: 1.5rem 0;
            border-left: 4px solid var(--accent);
            border-radius: 0 8px 8px 0;
            background-color: var(--accent-light);
        }}
        .markdown-alert-title {{
            display: flex; align-items: center; gap: 0.5rem;
            font-weight: 600; margin-bottom: 0.25rem;
        }}
        .markdown-alert-title svg {{
            width: 18px; height: 18px; flex-shrink: 0;
            fill: currentColor;
        }}
        .markdown-alert-note {{
            border-left-color: var(--alert-note);
            background-color: color-mix(in srgb, var(--alert-note) 10%, var(--bg-color));
        }}
        .markdown-alert-tip {{
            border-left-color: var(--alert-tip);
            background-color: color-mix(in srgb, var(--alert-tip) 10%, var(--bg-color));
        }}
        .markdown-alert-important {{
            border-left-color: var(--alert-important);
            background-color: color-mix(in srgb, var(--alert-important) 10%, var(--bg-color));
        }}
        .markdown-alert-warning {{
            border-left-color: var(--alert-warning);
            background-color: color-mix(in srgb, var(--alert-warning) 10%, var(--bg-color));
        }}
        .markdown-alert-caution {{
            border-left-color: var(--alert-caution);
            background-color: color-mix(in srgb, var(--alert-caution) 10%, var(--bg-color));
        }}
        .markdown-alert-note .markdown-alert-title svg {{
            color: var(--alert-note);
        }}
        .markdown-alert-tip .markdown-alert-title svg {{
            color: var(--alert-tip);
        }}
        .markdown-alert-important .markdown-alert-title svg {{
            color: var(--alert-important);
        }}
        .markdown-alert-warning .markdown-alert-title svg {{
            color: var(--alert-warning);
        }}
        .markdown-alert-caution .markdown-alert-title svg {{
            color: var(--alert-caution);
        }}
        .markdown-alert > :last-child {{
            margin-bottom: 0;
        }}
        /* Inline badge: <Badge type=""tip"">text</Badge> in raw Markdown. Markdig passes unrecognized
           tags through as raw HTML and lowercases them, so plain CSS on <badge> is enough -- no
           extension needed. Self-closing `<Badge .../>` is NOT supported: HTML has no XML-style
           self-close for unknown elements, so it'd swallow the rest of the paragraph. Always pair
           with a closing tag. */
        badge {{
            display: inline-flex; align-items: center; vertical-align: middle;
            margin: 0 0.3rem; padding: 0.15rem 0.55rem; border-radius: 6px;
            background-color: color-mix(in srgb, var(--alert-tip) 16%, var(--code-bg));
            color: var(--alert-tip); font-family: var(--font-sans);
            font-size: 0.7rem; font-weight: 600; letter-spacing: 0.03em;
            text-transform: uppercase; line-height: 1.5;
        }}
        badge[type=""info""] {{
            background-color: color-mix(in srgb, var(--alert-note) 16%, var(--code-bg));
            color: var(--alert-note);
        }}
        badge[type=""tip""] {{
            background-color: color-mix(in srgb, var(--alert-tip) 16%, var(--code-bg));
            color: var(--alert-tip);
        }}
        badge[type=""warning""] {{
            background-color: color-mix(in srgb, var(--alert-warning) 16%, var(--code-bg));
            color: var(--alert-warning);
        }}
        badge[type=""danger""] {{
            background-color: color-mix(in srgb, var(--alert-caution) 16%, var(--code-bg));
            color: var(--alert-caution);
        }}
        h1 badge, h2 badge, h3 badge, h4 badge {{
            font-size: 0.55em;
            margin-left: 0.5rem;
            vertical-align: middle;
        }}
        .pagination {{
            display: flex; justify-content: space-between;
            margin-top: 5rem; padding-top: 2rem;
            border-top: 1px solid var(--border);
        }}
        .pagination-link {{
            text-decoration: none; color: var(--text-muted);
            display: flex; flex-direction: column; gap: 0.25rem;
            transition: color 0.2s ease;
        }}
        .pagination-link:hover {{
            color: var(--accent);
        }}
        .pagination-link .label {{
            font-size: 0.75rem; text-transform: uppercase; letter-spacing: 0.05em;
        }}
        .pagination-link .title {{
            font-size: 1rem; font-weight: 500; color: var(--text-color);
        }}
        .pagination-link:hover .title {{
            color: var(--accent);
        }}
        .pagination-link.next {{
            text-align: right; margin-left: auto;
        }}
        .sidebar-right {{
            padding: 3.5rem 2rem;
            position: sticky; top: var(--topbar-height); align-self: start;
            height: calc(100vh - var(--topbar-height)); overflow-y: auto;
        }}
        .toc-title {{
            font-size: 0.75rem; text-transform: uppercase; letter-spacing: 0.05em;
            color: var(--text-muted); margin-bottom: 1rem; font-weight: 600;
        }}
        /* Containing block for .toc-indicator, which JS positions absolutely regardless of
           .toc-list/.toc-sublist nesting depth. */
        .toc-list-wrapper {{
            position: relative;
        }}
        /* Faint always-visible track; .toc-indicator overlays it and slides to the active item. */
        .toc-list-wrapper::before {{
            content: """"; position: absolute; left: 0; top: 0; bottom: 0; width: 2px;
            border-radius: 2px; background-color: var(--accent-light);
        }}
        .toc-indicator {{
            position: absolute; left: 0; top: 0; width: 2px; border-radius: 2px;
            background-color: var(--accent); opacity: 0; height: 0;
            transition: transform 0.25s cubic-bezier(0.16, 1, 0.3, 1), opacity 0.2s ease, height 0.2s ease;
            will-change: transform;
        }}
        .toc-indicator.visible {{
            opacity: 1;
        }}
        .toc-list {{
            list-style: none; font-size: 0.875rem; padding-left: 0.9rem;
        }}
        .toc-sublist {{
            list-style: none; padding-left: 0.9rem;
        }}
        .toc-item {{
            margin-bottom: 0.1rem;
        }}
        /* Levels differ by indentation and weight/size, not color -- the accent bar is the only color cue. */
        .toc-list > .toc-item > a {{
            font-weight: 500;
        }}
        .toc-list > .toc-item > .toc-sublist > .toc-item > a {{
            font-weight: 400;
        }}
        .toc-list > .toc-item > .toc-sublist > .toc-item > .toc-sublist > .toc-item > a {{
            font-weight: 400; font-size: 0.8rem;
        }}
        .toc-item a {{
            display: block; color: var(--text-muted); line-height: 1.5;
            text-decoration: none; padding: 0.3rem 0.8rem;
            transition: color 0.15s ease;
            overflow: hidden; text-overflow: ellipsis; white-space: nowrap;
        }}
        .toc-item a:hover {{
            color: var(--text-color);
        }}
        .toc-item.active > a {{
            color: var(--accent);
        }}
        .social-links {{
            display: flex; align-items: center; gap: 0.25rem;
        }}
        .social-icon-text {{
            font-size: 0.9rem;
        }}
        .sidebar-social-links {{
            display: none;
        }}
        .content-footer {{
            margin-top: 3rem; padding-top: 1.5rem;
            border-top: 1px solid var(--border);
            font-size: 0.8rem; color: var(--text-muted);
        }}
        .content-footer a {{
            color: var(--accent); text-decoration: none;
        }}
        .content-footer a:hover {{
            text-decoration: underline;
        }}
        .menu-toggle {{
            display: none;
        }}
        .sidebar-overlay {{
            display: none;
        }}
        .toc-inline {{
            display: none;
        }}
        /* Bump touch targets to 44px on coarse-pointer devices, not just by viewport width. */
        @media (hover: none) and (pointer: coarse) {{
            .icon-btn {{
                width: 40px;
                height: 40px;
            }}
            .toc-item a {{
                min-height: 44px; display: flex; align-items: center;
            }}
            .theme-toggle::after {{
                content: ""; position: absolute; left: 0; right: 0;
                top: 50%; transform: translateY(-50%); height: 44px;
            }}
            .code-block-buttons {{
                opacity: 1;
            }}
        }}
        @media (max-width: 1024px) {{
            .layout {{
                grid-template-columns: 240px 1fr;
            }}
            .sidebar-right {{
                display: none;
            }}
            .main-container {{
                padding: 2rem 1.5rem;
            }}
        }}
        @media (min-width: 769px) and (max-width: 1024px) {{
            .toc-inline {{
                display: block; margin-bottom: 2rem;
                border: 1px solid var(--border); border-radius: 8px; padding: 0.5rem 1rem;
            }}
            .toc-inline summary {{
                cursor: pointer; font-size: 0.8rem; font-weight: 600;
                text-transform: uppercase; letter-spacing: 0.05em; color: var(--text-muted);
                padding: 0.5rem 0; list-style: none;
                display: flex; align-items: center; justify-content: space-between;
            }}
            .toc-inline summary::-webkit-details-marker {{ display: none; }}
            .toc-inline summary::after {{
                content: ""; display: inline-block; width: 6px; height: 6px; flex-shrink: 0;
                border-right: 2px solid var(--text-muted); border-bottom: 2px solid var(--text-muted);
                transform: rotate(-45deg); transition: transform 0.2s ease;
            }}
            .toc-inline[open] summary::after {{ transform: rotate(45deg); }}
            .toc-inline .toc-list {{
                padding-bottom: 0.5rem;
            }}
            .toc-inline .toc-item a {{
                padding-left: 0.5rem; border-left: none;
            }}
        }}
        @media (max-width: 768px) {{
            .search-result-title {{
                font-size: 0.95rem;
            }}
            .search-result-excerpt {{
                font-size: 0.8rem;
            }}
        }}
        @media (prefers-reduced-motion: reduce) {{
            *, *::before, *::after {{
                animation-duration: 0.01ms !important;
                animation-iteration-count: 1 !important;
                transition-duration: 0.01ms !important;
                scroll-behavior: auto !important;
            }}
        }}

        h1, h2, h3, h4, h5, h6 {{ font-family: var(--font-display); letter-spacing: -0.015em; }}

        .topbar {{
            position: sticky; top: 0; z-index: 1002;
            display: flex; flex-direction: column; align-items: center; justify-content: flex-start; gap: 0.8rem;
            height: auto; min-height: 0;
            padding: 1.5rem clamp(1.25rem, 5vw, 2.75rem) 1.25rem;
            background: color-mix(in srgb, var(--bg-color) 86%, transparent);
            -webkit-backdrop-filter: blur(8px); backdrop-filter: blur(8px);
            border-bottom: 1px solid var(--border);
            -webkit-user-select: none; user-select: none;
        }}
        .masthead-actions {{
            position: absolute; top: 1.35rem; right: clamp(1.25rem, 5vw, 2.75rem);
            display: inline-flex; align-items: center; gap: 0.5rem;
        }}
        .brand {{
            font-family: var(--font-display); font-size: 1.45rem; font-weight: 600;
            letter-spacing: -0.015em; color: var(--text-color); text-decoration: none;
            display: inline-flex; align-items: center; gap: 0.5rem;
        }}
        .brand img {{ height: 1.4rem; width: auto; }}
        .site-nav {{
            display: flex; flex-wrap: wrap; justify-content: center;
            gap: 1.75rem; font-size: 0.9rem;
        }}
        .site-nav a {{ color: var(--text-muted); text-decoration: none; padding: 0.35rem 0; box-shadow: inset 0 -2px 0 transparent; transition: color 0.15s ease, box-shadow 0.15s ease; }}
        .site-nav a:hover {{ color: var(--text-color); }}
        .site-nav a.here {{ color: var(--text-color); }}
        .site-nav .top-nav-item {{ position: relative; display: inline-flex; align-items: center; height: auto; }}
        .site-nav .top-nav-link {{ padding: 0.35rem 0; font-size: 0.9rem; font-weight: 400; color: var(--text-muted); }}
        .site-nav .top-nav-link:hover {{ color: var(--text-color); }}
        .site-nav .top-nav-link.active {{ color: var(--text-color); box-shadow: inset 0 -2px 0 var(--accent); }}
        .site-nav .top-nav-chevron {{ width: 13px; height: 13px; }}
        .site-nav .top-nav-dropdown-menu {{
            display: block; opacity: 0; visibility: hidden;
            top: 100%; left: 50%; transform: translateX(-50%) translateY(6px);
            margin-top: 0.5rem; min-width: 170px; padding: 0.3rem;
            background: var(--bg-color); border: 1px solid var(--border);
            border-radius: 10px; box-shadow: var(--shadow-md);
            transition: opacity 0.15s ease, transform 0.15s ease, visibility 0.15s;
        }}
        .site-nav .top-nav-dropdown-menu::before {{
            content: ""; position: absolute; top: -0.9rem; left: 0; right: 0; height: 0.9rem;
        }}
        .site-nav .top-nav-item.has-dropdown:hover .top-nav-dropdown-menu,
        .site-nav .top-nav-item.has-dropdown:focus-within .top-nav-dropdown-menu {{
            opacity: 1; visibility: visible;
            transform: translateX(-50%) translateY(0);
        }}
        .site-nav .top-nav-dropdown-link {{
            justify-content: flex-start; padding: 0.5rem 0.7rem; border-radius: 7px;
            font-size: 0.875rem; color: var(--text-color); white-space: nowrap;
        }}
        .site-nav .top-nav-dropdown-link:hover, .site-nav .top-nav-dropdown-link.here {{
            background: var(--accent-light); color: var(--accent);
        }}

        .sidebar-left, .sidebar-right, .sidebar-overlay {{ display: none !important; }}
        .layout, .layout.no-left-sidebar {{ display: block; min-height: 0; }}
        .main-container {{ max-width: var(--measure-wide); margin: 0 auto; padding: 2.5rem clamp(1.25rem, 5vw, 2.75rem) 0; min-height: 0; }}
        .content {{ max-width: none; margin: 0; padding: 0; }}
        .content > :first-child, .content.reading > :first-child {{ margin-top: 0; }}
        .list-heading {{ margin-top: 0; }}
        .content.reading {{ max-width: var(--measure); margin: 0 auto; font-size: 1.125rem; line-height: 1.7; }}
        .content.reading p {{ margin: 0 0 1.6rem; }}
        .content.reading h1, .content.reading h2, .content.reading h3, .content.reading h4 {{ font-family: var(--font-display); font-weight: 600; border-bottom: none; padding-bottom: 0; }}
        .content.reading h2 {{ font-size: 1.6rem; line-height: 1.3; letter-spacing: -0.015em; margin: 2.75rem 0 1rem; }}
        .content.reading h3 {{ font-size: 1.3rem; line-height: 1.35; margin: 2.25rem 0 0.75rem; }}
        .content.reading a {{ text-decoration-color: color-mix(in srgb, var(--accent) 45%, transparent); }}

        #scroll-indicator {{
            height: 2px;
            background: linear-gradient(90deg, color-mix(in srgb, var(--accent) 55%, transparent), var(--accent));
        }}

        .list-heading {{ font-family: var(--font-display); font-size: clamp(1.7rem, 1.2rem + 1.5vw, 2.1rem); font-weight: 600; letter-spacing: -0.02em; margin: 2.5rem 0 0.4rem; }}
        .list-intro {{ color: var(--text-muted); margin: 0 0 2rem; font-size: 1.05rem; max-width: 60ch; }}
        .list-empty {{ color: var(--text-muted); padding: 2rem 0; }}
        .post-card {{ display: block; padding: 2.15rem 0; border-bottom: 1px solid var(--border); text-decoration: none; }}
        .post-card-title {{ font-family: var(--font-display); font-size: 1.4rem; font-weight: 600; line-height: 1.2; letter-spacing: -0.015em; margin: 0.45rem 0 0.4rem; }}
        .post-card-title a {{ color: var(--text-color); text-decoration: none; transition: color 0.15s ease; }}
        .post-card-title a:hover {{ color: var(--accent); }}
        .post-excerpt {{ color: var(--text-muted); margin: 0.35rem 0 0; max-width: 62ch; }}
        .post-meta {{
            display: flex; flex-wrap: wrap; align-items: center; gap: 0.5rem;
            font-size: 0.85rem; color: var(--text-muted); letter-spacing: 0.01em;
            font-variant-numeric: tabular-nums;
        }}
        .post-tags {{ display: inline-flex; flex-wrap: wrap; gap: 0.4rem; margin-left: 0.15rem; }}
        .tag-chip {{
            display: inline-block; text-decoration: none;
            color: var(--accent); background: var(--accent-light);
            padding: 0.12rem 0.55rem; border-radius: 999px; font-size: 0.72rem; letter-spacing: 0.01em;
        }}
        .tag-chip:hover {{ background: color-mix(in srgb, var(--accent) 18%, var(--accent-light)); }}

        .pager {{ display: flex; align-items: center; justify-content: space-between; gap: 1rem; margin-top: 2.5rem; padding-top: 1.5rem; border-top: 1px solid var(--border); font-size: 0.9rem; }}
        .pager a {{ color: var(--accent); text-decoration: none; }}
        .pager-status {{ color: var(--text-muted); font-variant-numeric: tabular-nums; }}

        .post-header {{ margin-bottom: 0.5rem; }}
        .post-title {{ font-family: var(--font-display); font-size: clamp(1.9rem, 1.2rem + 3vw, 3rem); font-weight: 600; line-height: 1.1; letter-spacing: -0.02em; margin: 0.3rem 0 0.9rem; text-wrap: balance; }}
        .post-header .post-meta {{ padding-bottom: 1.5rem; border-bottom: 1px solid var(--border); }}
        .content.reading > p:first-of-type {{ margin-top: 1.75rem; }}
        .content.reading blockquote {{ font-family: var(--font-display); font-weight: 500; font-style: normal; border-left: 2px solid var(--accent); padding: 0.1rem 0 0.1rem 1.5rem; margin: 2.2rem 0; font-size: 1.25rem; line-height: 1.5; letter-spacing: -0.01em; color: var(--text-color); }}
        .post-nav {{ display: flex; justify-content: space-between; gap: 1rem; margin-top: 4rem; padding-top: 1.5rem; border-top: 1px solid var(--border); font-size: 0.92rem; }}
        .post-nav a {{ color: var(--accent); text-decoration: none; }}
        .post-nav-newer {{ text-align: right; margin-left: auto; }}
        .page-title {{ font-family: var(--font-display); font-size: clamp(1.8rem, 1.2rem + 2.5vw, 2.6rem); font-weight: 600; letter-spacing: -0.02em; margin: 0.3rem 0 1.5rem; }}

        .tag-cloud {{ list-style: none; padding: 0; margin: 1.5rem 0 0; display: flex; flex-wrap: wrap; gap: 0.6rem; }}
        .tag-cloud .tag-chip {{ font-size: 0.85rem; padding: 0.3rem 0.7rem; }}
        .tag-count {{ margin-left: 0.4rem; opacity: 0.7; font-variant-numeric: tabular-nums; }}
        .archive-year {{ margin-top: 2.5rem; }}
        .archive-year h2 {{ font-size: 1.2rem; color: var(--text-muted); font-variant-numeric: tabular-nums; margin-bottom: 0.75rem; }}
        .archive-list {{ list-style: none; padding: 0; margin: 0; }}
        .content .archive-list, .content .tag-cloud, .content .author-grid {{ padding-left: 0; }}
        .archive-list li {{ display: flex; gap: 1rem; align-items: baseline; padding: 0.5rem 0; border-bottom: 1px solid var(--border); }}
        .archive-list time {{ color: var(--text-muted); font-size: 0.85rem; min-width: 4.5rem; font-variant-numeric: tabular-nums; }}
        .archive-list a {{ color: var(--text-color); text-decoration: none; }}
        .archive-list a:hover {{ color: var(--accent); }}
        .post-card:last-child, .lead:last-child {{ border-bottom: none; }}
        .archive-year:last-child .archive-list li:last-child {{ border-bottom: none; }}

        .lead {{ display: grid; grid-template-columns: 1.15fr 1fr; gap: clamp(1.75rem, 4vw, 3.5rem); align-items: center; padding: 3.5rem 0 3rem; border-bottom: 1px solid var(--border); }}
        .lead-cover {{ display: block; order: 2; aspect-ratio: 4 / 3; border-radius: 12px; overflow: hidden; border: 1px solid var(--border); background: var(--accent-light); }}
        .lead-cover img {{ width: 100%; height: 100%; object-fit: cover; display: block; }}
        .lead-cover-empty {{ background: var(--sidebar-bg); }}
        .lead-body {{ order: 1; }}
        .lead-title {{ font-family: var(--font-display); font-size: clamp(1.7rem, 1rem + 2.2vw, 2.4rem); font-weight: 600; line-height: 1.14; letter-spacing: -0.02em; margin: 0.55rem 0 0.5rem; text-wrap: balance; }}
        .lead-title a {{ color: var(--text-color); text-decoration: none; }}
        .lead-title a:hover {{ color: var(--accent); }}
        .lead-excerpt {{ color: var(--text-muted); margin: 0.5rem 0 1rem; }}
        .readmore {{ display: inline-flex; align-items: center; gap: 0.35rem; color: var(--accent); text-decoration: none; font-weight: 600; font-size: 0.9rem; }}
        .readmore span {{ transition: transform 0.15s ease; }}
        .readmore:hover span {{ transform: translateX(3px); }}

        .byline {{ gap: 0.55rem; }}
        .byline-author {{ color: var(--text-color); font-weight: 500; }}
        .byline-link {{ display: inline-flex; align-items: center; gap: 0.55rem; color: inherit; text-decoration: none; }}
        .byline-link:hover .byline-author {{ color: var(--accent); }}
        .avatar {{ width: 26px; height: 26px; border-radius: 50%; object-fit: cover; display: inline-grid; place-items: center; background: radial-gradient(circle at 35% 30%, color-mix(in srgb, var(--accent) 55%, var(--sidebar-bg)), var(--accent)); color: var(--bg-color); font-size: 0.72rem; font-weight: 700; }}
        .avatar-initial {{ display: inline-grid; place-items: center; background: radial-gradient(circle at 35% 30%, color-mix(in srgb, var(--accent) 55%, var(--sidebar-bg)), var(--accent)); color: var(--bg-color); font-weight: 700; }}
        .author-header {{ text-align: center; padding: 0.5rem 0 2rem; border-bottom: 1px solid var(--border); margin-bottom: 2.5rem; }}
        .author-header-avatar {{ width: 76px; height: 76px; border-radius: 50%; object-fit: cover; margin: 0 auto 1rem; }}
        .author-header-avatar.avatar-initial {{ font-size: 1.9rem; }}
        .author-name {{ font-family: var(--font-display); font-size: clamp(1.8rem, 1.2rem + 2.5vw, 2.6rem); font-weight: 600; letter-spacing: -0.02em; margin: 0 0 0.6rem; }}
        .author-bio {{ color: var(--text-muted); max-width: 52ch; margin: 0 auto; }}
        .author-bio p {{ margin: 0; }}
        .author-grid {{ list-style: none; padding: 0; margin: 1.5rem 0 0; display: grid; grid-template-columns: repeat(auto-fill, minmax(190px, 1fr)); gap: 1rem; }}
        .author-card {{ display: flex; align-items: center; gap: 0.75rem; padding: 0.7rem 0.85rem; border: 1px solid var(--border); border-radius: 12px; text-decoration: none; color: var(--text-color); transition: border-color 0.15s ease; }}
        .author-card:hover {{ border-color: color-mix(in srgb, var(--accent) 40%, var(--border)); }}
        .author-card-avatar {{ width: 44px; height: 44px; border-radius: 50%; object-fit: cover; flex-shrink: 0; }}
        .author-card-avatar.avatar-initial {{ font-size: 1.1rem; }}
        .author-card-name {{ font-weight: 500; }}
        .post-cover {{ display: block; width: 100%; aspect-ratio: 16 / 9; object-fit: cover; border-radius: 12px; border: 1px solid var(--border); background: var(--sidebar-bg); margin: 1.75rem 0 0; }}
        .content.reading img {{ max-width: 100%; height: auto; border-radius: 10px; background: var(--sidebar-bg); }}
        .content.reading p > img:only-child {{ display: block; width: 100%; aspect-ratio: 16 / 9; object-fit: cover; border: 1px solid var(--border); border-radius: 12px; background: var(--sidebar-bg); margin: 1.75rem 0; }}
        .content.reading img.natural, .content.reading p > img.natural:only-child {{ aspect-ratio: auto; object-fit: initial; width: auto; }}
        .content.reading img.plain, .content.reading p > img.plain:only-child {{ border: none; background: none; border-radius: 0; }}
        .content.reading img.left, .content.reading p > img.left:only-child {{ float: left; width: min(45%, 20rem); margin: 0.4rem 1.4rem 1rem 0; aspect-ratio: auto; }}
        .content.reading img.right, .content.reading p > img.right:only-child {{ float: right; width: min(45%, 20rem); margin: 0.4rem 0 1rem 1.4rem; aspect-ratio: auto; }}

        .brand-mark {{ font-size: 1.15rem; line-height: 1; }}
        .site-footer {{
            max-width: var(--measure-wide); margin: 5.5rem auto 3rem;
            padding: 1.75rem clamp(1.25rem, 5vw, 2.75rem) 0;
            border-top: 1px solid var(--border);
            display: flex; flex-wrap: wrap; align-items: center; gap: 0.6rem 1.2rem;
            font-size: 0.82rem; color: var(--text-muted);
        }}
        .site-footer a {{ color: var(--accent); text-decoration: none; }}
        .site-footer a:hover {{ text-decoration: underline; }}
        .site-footer .social-links {{ display: inline-flex; gap: 0.2rem; }}
        .site-footer .social-links a {{ text-decoration: none; }}
        .site-footer .icon-btn {{ width: 32px; height: 32px; }}
        .site-footer {{ justify-content: flex-start; }}
        .site-footer-note {{ margin-right: auto; text-align: left; }}
        .DocSearch-Commands {{ display: none !important; }}
        .search-modal-results:empty {{ display: none; }}
        @media (hover: none) and (pointer: coarse) {{
            .site-nav a, .site-nav .top-nav-link {{ min-height: 44px; display: inline-flex; align-items: center; }}
            .site-footer .icon-btn {{ width: 40px; height: 40px; }}
        }}

        @media (max-width: 620px) {{
            .topbar {{ align-items: flex-start; }}
            .site-nav {{ gap: 1.1rem; font-size: 0.85rem; justify-content: flex-start; align-items: flex-start; }}
            .brand {{ font-size: 1.3rem; }}
            .site-nav .top-nav-item.has-dropdown {{ flex-direction: column; align-items: flex-start; }}
            .site-nav .top-nav-dropdown-menu {{
                position: static; display: none; opacity: 1; visibility: visible;
                transform: none; margin: 0.4rem 0 0; box-shadow: none; min-width: 150px;
            }}
            .site-nav .top-nav-item.has-dropdown:hover .top-nav-dropdown-menu,
            .site-nav .top-nav-item.has-dropdown:focus-within .top-nav-dropdown-menu {{ display: block; }}
            .post-nav {{ flex-direction: column; gap: 0.75rem; }}
            .post-nav-newer {{ text-align: left; margin-left: 0; }}
            .site-footer-note {{ margin-right: 0; flex-basis: 100%; }}
            .lead {{ grid-template-columns: 1fr; }}
            .lead-cover {{ order: 1; }}
            .lead-body {{ order: 2; }}
        }}
</style>
";
}
