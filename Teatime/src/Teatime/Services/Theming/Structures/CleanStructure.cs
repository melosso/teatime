namespace Teatime.Services.Theming.Structures;

/// <summary>
/// Minimal text-first page shape, with a single-row topbar and no cover images; 
/// </summary>
public sealed class CleanStructure : ITeatimeStructure
{
    public string Name => "clean";

    public string Label => "Clean";

    public string ComponentCss => """
                :root {
                    font-size: 18.5px;
                    --measure: 700px;
                    --measure-wide: 760px;
                }
                @media (min-width: 769px) {
                    .topbar {
                        flex-direction: row;
                        align-items: center;
                        justify-content: space-between;
                        flex-wrap: wrap;
                        gap: 1.5rem 2rem;
                        padding: 1rem var(--topbar-pad);
                    }
                    .brand {
                        order: 1;
                        font-size: 1.25rem;
                        font-weight: 700;
                        letter-spacing: -0.02em;
                        margin-left: calc((100% - var(--measure-wide)) / 2);
                    }
                    .site-nav-wrap {
                        order: 2;
                        position: static;
                        width: auto;
                        flex: 1 1 auto;
                        justify-content: center;
                    }
                    .site-nav {
                        justify-content: center;
                        gap: 1.75rem;
                    }
                    .masthead-actions {
                        order: 3;
                        position: static;
                        margin-right: calc((100% - var(--measure-wide)) / 2);
                    }
                }
                .lead,
                .post-card {
                    display: block;
                }
                .lead-cover,
                .card-cover {
                    display: none;
                }
                .lead {
                    padding: 2rem 0 2.5rem;
                }
                .post-card {
                    padding: 1.75rem 0;
                }
                .lead-title {
                    font-size: var(--lead-title-size);
                }
                .post-title,
                .lead-title,
                .page-title {
                    font-weight: 700;
                    letter-spacing: -0.03em;
                    line-height: 1.06;
                }
                .content.reading h2 {
                    font-weight: 700;
                    letter-spacing: -0.025em;
                }
                .content.reading h3 {
                    font-weight: 700;
                }
                .content.reading {
                    font-size: 1.1875rem;
                    line-height: 1.75;
                }
                .prose .custom-block {
                    font-size: 1.1rem;
                }
                .content.reading > p:first-of-type {
                    color: var(--text-muted);
                    font-size: 1.25rem;
                    line-height: 1.6;
                }
                @media (prefers-color-scheme: dark) {
                    .content.reading > p:first-of-type {
                        color: color-mix(in srgb, var(--text-muted) 75%, var(--text-color));
                    }
                }
                :root[data-theme="dark"] .content.reading > p:first-of-type {
                    color: color-mix(in srgb, var(--text-muted) 75%, var(--text-color));
                }
                .search-trigger {
                    border: none;
                    background: none;
                    padding: 0.35rem;
                    margin-left: 0;
                }
                .subscribe-trigger {
                    display: inline-flex;
                    align-items: center;
                    justify-content: center;
                    width: 36px;
                    height: 36px;
                    border-radius: 6px;
                    border: none;
                    background: transparent;
                    color: var(--text-muted);
                    cursor: pointer;
                    flex-shrink: 0;
                    padding: 0;
                    font-size: 0;
                    transition: color 0.15s ease, background-color 0.15s ease;
                }
                .subscribe-trigger::before {
                    content: "";
                    width: 18px;
                    height: 18px;
                    flex-shrink: 0;
                    background-color: currentColor;
                    -webkit-mask: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24' fill='none' stroke='black' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'%3E%3Crect x='3' y='5' width='18' height='14' rx='2'/%3E%3Cpath d='m3 7 9 6 9-6'/%3E%3C/svg%3E") center / contain no-repeat;
                    mask: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24' fill='none' stroke='black' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'%3E%3Crect x='3' y='5' width='18' height='14' rx='2'/%3E%3Cpath d='m3 7 9 6 9-6'/%3E%3C/svg%3E") center / contain no-repeat;
                }
                .subscribe-trigger:hover {
                    color: var(--accent);
                    background-color: var(--code-bg);
                }
                .tag-chip {
                    background: var(--code-bg);
                    border: 1px solid var(--border);
                    color: var(--text-muted);
                    font-weight: 500;
                }
                .tag-chip:hover {
                    background: var(--accent-light);
                    color: var(--accent);
                }
                .post-header .post-meta {
                    border-bottom: 0;
                }
                code {
                    font-size: 0.925rem;
                }
                .prose table {
                    font-size: 0.925rem;
                }
        """;
}
