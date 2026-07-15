namespace Teatime.Services.Layout;

public static partial class LayoutProvider
{
    private static string GetScripts(bool enableLiveReload, bool enableDarkMode, long buildVersion, string basePath, string? nonce = null, bool staticSearch = false)
    {
        // Non-toggle theme swaps (bfcache/other tab/OS flip) hit an already-painted page: suppress transitions or every element cross-fades.
        var themeSyncScript = enableDarkMode
            ? @"var themeRoot = document.documentElement;
        var prefersDark = window.matchMedia('(prefers-color-scheme: dark)');
        function syncThemeToggle() {
            var toggle = document.getElementById('theme-toggle');
            if (!toggle) return;
            var current = themeRoot.getAttribute('data-theme');
            toggle.setAttribute('aria-checked', String(current ? current === 'dark' : prefersDark.matches));
        }
        function applyStoredTheme() {
            var t = null;
            try { t = localStorage.getItem('teatime-theme'); } catch (_) {}
            themeRoot.classList.add('no-theme-transition');
            if (t === 'dark' || t === 'light') {
                themeRoot.setAttribute('data-theme', t);
                themeRoot.style.colorScheme = t;
            } else {
                themeRoot.removeAttribute('data-theme');
                themeRoot.style.colorScheme = '';
            }
            requestAnimationFrame(function() {
                requestAnimationFrame(function() {
                    themeRoot.classList.remove('no-theme-transition');
                });
            });
            syncThemeToggle();
        }
        window.addEventListener('pageshow', function(e) {
            if (e.persisted) applyStoredTheme();
        });
        window.addEventListener('storage', function(e) {
            if (e.key === 'teatime-theme') applyStoredTheme();
        });
        prefersDark.addEventListener('change', syncThemeToggle);"
            : "";

        return $@"    <script{GetNonceAttr(nonce)}>
        document.addEventListener('error', function(e) {{
            if (e.target && e.target.classList && e.target.classList.contains('tab-icon')) e.target.remove();
        }}, true);
        {themeSyncScript}
        document.addEventListener('DOMContentLoaded', function() {{
            var headings = document.querySelectorAll('.content h1, .content h2, .content h3, .content h4');
            var tocItems = document.querySelectorAll('.toc-item');
            var tocIndicator = document.querySelector('.toc-indicator');
            var tocListWrapper = document.querySelector('.toc-list-wrapper');
            var scrollIndicator = document.getElementById('scroll-indicator');
            var themeToggle = document.getElementById('theme-toggle');

            if ({(enableLiveReload ? "true" : "false")}) {{
                var currentBuildVersion = {buildVersion};
                setInterval(function() {{
                    fetch('{basePath}/api/build-version')
                        .then(function(r) {{ return r.json(); }})
                        .then(function(data) {{
                            if (data.version !== currentBuildVersion) {{
                                location.reload();
                            }}
                        }})
                        ['catch'](function() {{}});
                }}, 5000);
            }}

            if (themeToggle) {{
                syncThemeToggle();

                themeToggle.addEventListener('click', function() {{
                    var current = themeRoot.getAttribute('data-theme');
                    var isDark = current ? current === 'dark' : prefersDark.matches;
                    var next = isDark ? 'light' : 'dark';
                    themeRoot.setAttribute('data-theme', next);
                    themeRoot.style.colorScheme = next;
                    themeToggle.setAttribute('aria-checked', String(next === 'dark'));
                    try {{ localStorage.setItem('teatime-theme', next); }} catch (e) {{}}

                    // Mermaid bakes theme colors into its SVG at render time; reload to re-render.
                    if (document.querySelector('.mermaid')) {{
                        location.reload();
                    }}
                }});
            }}

            var navDropdowns = Array.prototype.slice.call(document.querySelectorAll('.site-nav .top-nav-item.has-dropdown'));
            function closeNavDropdown(item) {{
                item.classList.remove('open');
                var b = item.querySelector('.top-nav-link');
                if (b) b.setAttribute('aria-expanded', 'false');
            }}
            navDropdowns.forEach(function(item) {{
                var btn = item.querySelector('.top-nav-link');
                if (!btn) return;
                btn.addEventListener('click', function(e) {{
                    e.preventDefault();
                    var willOpen = !item.classList.contains('open');
                    navDropdowns.forEach(closeNavDropdown);
                    if (willOpen) {{
                        item.classList.add('open');
                        btn.setAttribute('aria-expanded', 'true');
                    }}
                }});
            }});
            if (navDropdowns.length) {{
                document.addEventListener('click', function(e) {{
                    navDropdowns.forEach(function(item) {{
                        if (item.classList.contains('open') && !item.contains(e.target)) closeNavDropdown(item);
                    }});
                }});
                document.addEventListener('keydown', function(e) {{
                    if (e.key === 'Escape') navDropdowns.forEach(closeNavDropdown);
                }});
            }}

            document.querySelectorAll('.load-more').forEach(function(btn) {{
                var skeletonCard = '<article class=""post-card skeleton-card"" aria-hidden=""true"">' +
                    '<div class=""skeleton skeleton-title""></div>' +
                    '<div class=""skeleton skeleton-meta""></div>' +
                    '<div class=""skeleton skeleton-line""></div>' +
                    '<div class=""skeleton skeleton-line short""></div>' +
                    '</article>';
                btn.addEventListener('click', function() {{
                    var next = btn.getAttribute('data-next');
                    if (!next || btn.disabled) return;
                    var wrap = btn.closest('.load-more-wrap');
                    btn.disabled = true;
                    var skel = document.createElement('div');
                    skel.className = 'load-more-skeletons';
                    skel.innerHTML = skeletonCard + skeletonCard + skeletonCard;
                    wrap.parentNode.insertBefore(skel, wrap);
                    fetch(next)
                        .then(function(r) {{ return r.text(); }})
                        .then(function(html) {{
                            var doc = new DOMParser().parseFromString(html, 'text/html');
                            var cards = doc.querySelectorAll('.content .post-card');
                            var nextBtn = doc.querySelector('.load-more');
                            skel.remove();
                            cards.forEach(function(c) {{ wrap.parentNode.insertBefore(c, wrap); }});
                            var moreUrl = nextBtn ? nextBtn.getAttribute('data-next') : null;
                            if (moreUrl) {{ btn.setAttribute('data-next', moreUrl); btn.disabled = false; }}
                            else {{ wrap.remove(); }}
                        }})
                        ['catch'](function() {{
                            skel.remove();
                            btn.disabled = false;
                        }});
                }});
            }});

            var topbarEl = document.querySelector('.topbar');
            if (topbarEl) {{
                var syncTopbarHeight = function() {{
                    document.documentElement.style.setProperty('--topbar-height', topbarEl.offsetHeight + 'px');
                }};
                syncTopbarHeight();
                window.addEventListener('resize', syncTopbarHeight);
                if (window.ResizeObserver) new ResizeObserver(syncTopbarHeight).observe(topbarEl);
            }}

            function updateScrollProgress() {{
                if (!scrollIndicator) return;
                var winScroll = document.documentElement.scrollTop || document.body.scrollTop;
                var height = document.documentElement.scrollHeight - document.documentElement.clientHeight;
                var scrolled = height > 0 ? (winScroll / height) * 100 : 0;
                scrollIndicator.style.width = scrolled + '%';
            }}

            window.addEventListener('scroll', updateScrollProgress);

            function updateTocIndicator() {{
                if (!tocIndicator || !tocListWrapper) return;
                var activeLink = tocListWrapper.querySelector('.toc-item.active > a');
                if (!activeLink) {{
                    tocIndicator.classList.remove('visible');
                    return;
                }}
                var wrapperTop = tocListWrapper.getBoundingClientRect().top;
                var linkRect = activeLink.getBoundingClientRect();
                tocIndicator.style.transform = 'translateY(' + (linkRect.top - wrapperTop) + 'px)';
                tocIndicator.style.height = linkRect.height + 'px';
                tocIndicator.classList.add('visible');
            }}

            // toc-item markup renders twice (tablet dropdown + desktop sidebar), so activation is keyed by heading id.
            function setActiveTocId(id) {{
                tocItems.forEach(function(item) {{
                    var link = item.querySelector('a');
                    var matches = link && link.getAttribute('href') === '#' + id;
                    item.classList.toggle('active', !!matches);
                }});
                updateTocIndicator();
            }}

            var observer = new IntersectionObserver(function(entries) {{
                entries.forEach(function(entry) {{
                    if (entry.isIntersecting) {{
                        setActiveTocId(entry.target.getAttribute('id'));
                    }}
                }});
            }}, {{ root: null, rootMargin: '-5% 0px -75% 0px', threshold: 0 }});

            headings.forEach(function(h) {{ observer.observe(h); }});

            // Native #hash scroll happens before JS; a heading flush at the top sits outside the observer rootMargin and never fires.
            if (location.hash) {{
                setActiveTocId(location.hash.slice(1));
            }}
            window.addEventListener('hashchange', function() {{
                if (location.hash) setActiveTocId(location.hash.slice(1));
            }});

            // At page bottom the last heading can sit above the observer rootMargin, leaving the wrong item active.
            function checkScrolledToBottom() {{
                var atBottom = window.innerHeight + window.scrollY >= document.documentElement.scrollHeight - 2;
                if (atBottom && headings.length > 0) {{
                    setActiveTocId(headings[headings.length - 1].getAttribute('id'));
                }}
            }}

            window.addEventListener('scroll', checkScrolledToBottom, {{ passive: true }});
            window.addEventListener('resize', updateTocIndicator);
            checkScrolledToBottom();

            tocItems.forEach(function(item) {{
                var link = item.querySelector('a');
                if (link) {{
                    link.addEventListener('click', function() {{
                        setActiveTocId(link.getAttribute('href').slice(1));
                    }});
                }}
            }});

            var searchTrigger = document.getElementById('search-trigger');
            var searchTriggerKbd = document.getElementById('search-trigger-kbd');
            var searchOverlay = document.getElementById('search-overlay');
            var searchModalInput = document.getElementById('search-modal-input');
            var searchModalResults = document.getElementById('search-modal-results');
            var searchModalClose = document.getElementById('search-modal-close');
            var searchModalStatus = document.getElementById('search-modal-status');
            var searchTimeout;
            var searchActiveIndex = -1;
            var searchLastFocused = null;
            var searchRequestId = 0;

            // Static export: search runs in-browser against a prebuilt index, mirroring SearchIndex.
            var teatimeStaticSearch = {(staticSearch ? "true" : "false")};
            var teatimeSearchIndexUrl = '{basePath}/search-index.json';
            var teatimeSearchIndexPromise = null;
            var BARK_MAX_QUERY_LENGTH = 128, BARK_MAX_QUERY_TERMS = 8, BARK_MAX_FUZZY = 3, BARK_FUZZY_THRESHOLD = 0.5;

            function teatimeLoadSearchIndex() {{
                if (!teatimeSearchIndexPromise) {{
                    teatimeSearchIndexPromise = fetch(teatimeSearchIndexUrl).then(function(r) {{
                        if (!r.ok) throw new Error('search index unavailable');
                        return r.json();
                    }});
                }}
                return teatimeSearchIndexPromise;
            }}

            function teatimeTokenize(text) {{
                if (!text) return [];
                return text.split(/\W+/)
                    .map(function(w) {{ return w.trim().toLowerCase(); }})
                    .filter(function(w) {{ return w.length > 0; }});
            }}

            function teatimeTrigrams(term) {{
                if (term.length < 3) return [];
                var out = [];
                for (var i = 0; i <= term.length - 3; i++) out.push(term.substr(i, 3));
                return out;
            }}

            function teatimeExcerpt(text, term) {{
                if (!text) return null;
                var idx = text.toLowerCase().indexOf(term.toLowerCase());
                if (idx < 0) return null;
                var start = Math.max(0, idx - 60);
                var length = Math.min(text.length - start, 160);
                var excerpt = text.slice(start, start + length).trim();
                if (start > 0) excerpt = '...' + excerpt;
                if (start + length < text.length) excerpt = excerpt + '...';
                return excerpt;
            }}

            function teatimeFindFuzzy(index, term) {{
                var tg = teatimeTrigrams(term);
                if (!tg.length) return [];
                var shared = {{}};
                tg.forEach(function(t) {{
                    var terms = index.trigrams[t];
                    if (!terms) return;
                    terms.forEach(function(it) {{ shared[it] = (shared[it] || 0) + 1; }});
                }});
                var cands = [];
                Object.keys(shared).forEach(function(it) {{
                    var countB = Math.max(it.length - 2, 0);
                    var denom = tg.length + countB;
                    var sim = denom === 0 ? 0 : (2 * shared[it]) / denom;
                    if (sim >= BARK_FUZZY_THRESHOLD) cands.push({{ term: it, sim: sim }});
                }});
                cands.sort(function(a, b) {{ return b.sim - a.sim || (a.term < b.term ? -1 : a.term > b.term ? 1 : 0); }});
                return cands.slice(0, BARK_MAX_FUZZY).map(function(c) {{ return c.term; }});
            }}

            function teatimeAccumulate(index, scores, postings, excerptTerm, divisor) {{
                postings.forEach(function(p) {{
                    var doc = index.docs[p.doc];
                    if (!doc) return;
                    var eff = Math.max(1, Math.floor(p.score / divisor));
                    var ex = teatimeExcerpt(doc.text, excerptTerm);
                    if (ex == null && doc.description) ex = teatimeExcerpt(doc.description, excerptTerm);
                    var cur = scores[p.doc];
                    if (cur) {{ cur.score += eff; if (cur.excerpt == null) cur.excerpt = ex; }}
                    else scores[p.doc] = {{ score: eff, excerpt: ex }};
                }});
            }}

            function teatimeGroupNameMatches(list, query, max) {{
                var q = query.trim().toLowerCase();
                if (!q) return [];
                return (list || []).filter(function(x) {{ return x.name.toLowerCase().indexOf(q) >= 0; }})
                    .sort(function(a, b) {{
                        var ra = a.name.toLowerCase().indexOf(q) === 0 ? 0 : 1;
                        var rb = b.name.toLowerCase().indexOf(q) === 0 ? 0 : 1;
                        return ra - rb || (b.count || 0) - (a.count || 0) || (a.name < b.name ? -1 : a.name > b.name ? 1 : 0);
                    }})
                    .slice(0, max);
            }}

            function teatimeSearchStatic(index, query) {{
                var authors = teatimeGroupNameMatches(index.authors, query, 5);
                var tags = teatimeGroupNameMatches(index.tags, query, 8);
                if (query.length > BARK_MAX_QUERY_LENGTH) query = query.slice(0, BARK_MAX_QUERY_LENGTH);
                var terms = teatimeTokenize(query);
                var posts = [];
                if (terms.length) {{
                    if (terms.length > BARK_MAX_QUERY_TERMS) terms = terms.slice(0, BARK_MAX_QUERY_TERMS);
                    var scores = {{}};
                    terms.forEach(function(term) {{
                        var key = term.toLowerCase();
                        if (index.terms[key]) {{ teatimeAccumulate(index, scores, index.terms[key], term, 1); return; }}
                        teatimeFindFuzzy(index, key).forEach(function(cand) {{
                            if (index.terms[cand]) teatimeAccumulate(index, scores, index.terms[cand], cand, 2);
                        }});
                    }});
                    posts = Object.keys(scores).map(function(k) {{
                        var doc = index.docs[k];
                        return {{ path: doc.path, title: doc.title, description: doc.description, excerpt: scores[k].excerpt, score: scores[k].score }};
                    }}).filter(function(r) {{ return r.path.indexOf('authors/') !== 0; }})
                      .sort(function(a, b) {{ return b.score - a.score || (a.path < b.path ? -1 : a.path > b.path ? 1 : 0); }});
                }}
                return {{ authors: authors, tags: tags, posts: posts }};
            }}

            function teatimeRunSearch(query) {{
                if (teatimeStaticSearch) {{
                    return teatimeLoadSearchIndex().then(function(index) {{ return teatimeSearchStatic(index, query); }});
                }}
                return fetch('{basePath}/api/search?q=' + encodeURIComponent(query)).then(function(r) {{ return r.json(); }});
            }}

            if (searchTriggerKbd && /Mac|iPhone|iPad/.test(navigator.platform || '')) {{
                searchTriggerKbd.textContent = '⌘K';
            }}

            function escapeHtml(value) {{
                var div = document.createElement('div');
                div.textContent = value == null ? '' : value;
                return div.innerHTML;
            }}

            function escapeRegExp(value) {{
                return value.replace(/[.*+?^${{}}()|[\]\\]/g, '\\$&');
            }}

            // Escape first, then highlight within the escaped string so the <mark> wrap can't reopen an XSS hole.
            function highlightMatches(value, terms) {{
                var escaped = escapeHtml(value);
                if (!terms.length) return escaped;
                var pattern = new RegExp('(' + terms.map(escapeRegExp).join('|') + ')', 'ig');
                return escaped.replace(pattern, '<mark class=""search-highlight"">$1</mark>');
            }}

            function getSearchResultItems() {{
                return Array.prototype.slice.call(searchModalResults.querySelectorAll('.search-result-item'));
            }}

            function setSearchActiveIndex(index) {{
                var items = getSearchResultItems();
                if (items.length === 0) {{
                    searchActiveIndex = -1;
                    searchModalInput.removeAttribute('aria-activedescendant');
                    return;
                }}
                searchActiveIndex = (index + items.length) % items.length;
                items.forEach(function(item, i) {{
                    var isActive = i === searchActiveIndex;
                    item.classList.toggle('active', isActive);
                    item.setAttribute('aria-selected', isActive ? 'true' : 'false');
                }});
                var activeItem = items[searchActiveIndex];
                searchModalInput.setAttribute('aria-activedescendant', activeItem.id);
                activeItem.scrollIntoView({{ block: 'nearest' }});
            }}

            function openSearchModal() {{
                searchLastFocused = document.activeElement;
                searchOverlay.hidden = false;
                requestAnimationFrame(function() {{ searchOverlay.classList.add('open'); }});
                document.documentElement.style.overflow = 'hidden';
                searchModalInput.value = '';
                searchModalInput.focus();
                searchModalResults.innerHTML = '';
                searchModalInput.setAttribute('aria-expanded', 'false');
                searchModalInput.removeAttribute('aria-activedescendant');
                searchModalStatus.textContent = '';
                searchActiveIndex = -1;
            }}

            function closeSearchModal() {{
                searchOverlay.classList.remove('open');
                searchOverlay.hidden = true;
                document.documentElement.style.overflow = '';
                if (searchLastFocused && typeof searchLastFocused.focus === 'function') {{
                    searchLastFocused.focus();
                }}
            }}

            function isSearchModalOpen() {{
                return !searchOverlay.hidden;
            }}

            if (searchTrigger) {{
                searchTrigger.addEventListener('click', openSearchModal);
            }}
            searchModalClose.addEventListener('click', closeSearchModal);
            searchOverlay.addEventListener('mousedown', function(e) {{
                if (e.target === searchOverlay) closeSearchModal();
            }});

            document.addEventListener('keydown', function(e) {{
                var isCtrlK = (e.ctrlKey || e.metaKey) && e.key.toLowerCase() === 'k';
                if (isCtrlK) {{
                    e.preventDefault();
                    isSearchModalOpen() ? closeSearchModal() : openSearchModal();
                    return;
                }}
                if (!isSearchModalOpen()) return;
                if (e.key === 'Escape') {{
                    e.preventDefault();
                    closeSearchModal();
                    return;
                }}
                if (e.key === 'Tab') {{
                    var focusable = [searchModalInput, searchModalClose];
                    var currentIndex = focusable.indexOf(document.activeElement);
                    e.preventDefault();
                    var nextIndex = e.shiftKey
                        ? (currentIndex <= 0 ? focusable.length - 1 : currentIndex - 1)
                        : (currentIndex === -1 || currentIndex === focusable.length - 1 ? 0 : currentIndex + 1);
                    focusable[nextIndex].focus();
                    return;
                }}
                if (e.key === 'ArrowDown') {{
                    e.preventDefault();
                    if (getSearchResultItems().length) setSearchActiveIndex(searchActiveIndex + 1);
                    return;
                }}
                if (e.key === 'ArrowUp') {{
                    e.preventDefault();
                    if (getSearchResultItems().length) setSearchActiveIndex(searchActiveIndex - 1);
                    return;
                }}
                if (e.key === 'Enter') {{
                    var items = getSearchResultItems();
                    if (searchActiveIndex >= 0 && items[searchActiveIndex]) {{
                        e.preventDefault();
                        items[searchActiveIndex].click();
                    }}
                }}
            }});

            searchModalResults.addEventListener('mouseover', function(e) {{
                var item = e.target.closest('.search-result-item');
                if (!item) return;
                var index = getSearchResultItems().indexOf(item);
                if (index !== -1) setSearchActiveIndex(index);
            }});

            searchModalInput.addEventListener('input', function() {{
                clearTimeout(searchTimeout);
                var query = searchModalInput.value.trim();
                searchActiveIndex = -1;
                searchRequestId += 1;
                if (query.length < 2) {{
                    searchModalResults.innerHTML = '';
                    searchModalInput.setAttribute('aria-expanded', 'false');
                    searchModalStatus.textContent = '';
                    return;
                }}
                searchModalResults.innerHTML = '<div class=""search-result-empty"" role=""status"">Searching&hellip;</div>';
                var requestId = searchRequestId;
                searchTimeout = setTimeout(function() {{
                    teatimeRunSearch(query)
                        .then(function(data) {{
                            if (requestId !== searchRequestId) return; // a newer keystroke superseded this request
                            var authors = data.authors || [];
                            var tags = data.tags || [];
                            var posts = data.posts || [];
                            var total = authors.length + tags.length + posts.length;
                            if (total === 0) {{
                                searchModalResults.innerHTML = '<div class=""search-result-empty"" role=""status"">No results found.</div>';
                                searchModalStatus.textContent = 'No results found.';
                            }} else {{
                                var terms = query.split(/\s+/).filter(Boolean);
                                var html = '';
                                var i = 0;
                                if (authors.length) {{
                                    html += '<div class=""search-group""><h3 class=""search-group-label"">Authors</h3>';
                                    authors.forEach(function(a) {{
                                        var avatar = a.image
                                            ? '<img class=""search-hit-avatar"" src=""' + escapeHtml(a.image) + '"" alt="""" loading=""lazy"">'
                                            : '<span class=""search-hit-avatar avatar-initial"">' + escapeHtml((a.name || '?').slice(0, 1)) + '</span>';
                                        html += '<a href=""{basePath}/' + a.url + '/"" class=""search-result-item search-hit-media"" role=""option"" id=""search-result-' + i + '"" aria-selected=""false"" tabindex=""-1"">' +
                                            avatar + '<span class=""search-result-title"">' + highlightMatches(a.name, terms) + '</span></a>';
                                        i++;
                                    }});
                                    html += '</div>';
                                }}
                                if (tags.length) {{
                                    html += '<div class=""search-group""><h3 class=""search-group-label"">Tags</h3>';
                                    tags.forEach(function(t) {{
                                        html += '<a href=""{basePath}/' + t.url + '/"" class=""search-result-item search-hit-media"" role=""option"" id=""search-result-' + i + '"" aria-selected=""false"" tabindex=""-1"">' +
                                            '<span class=""search-hit-tag"" aria-hidden=""true"">#</span>' +
                                            '<span class=""search-result-title"">' + highlightMatches(t.name, terms) + '</span>' +
                                            '<span class=""search-hit-count"">' + t.count + '</span></a>';
                                        i++;
                                    }});
                                    html += '</div>';
                                }}
                                if (posts.length) {{
                                    html += '<div class=""search-group""><h3 class=""search-group-label"">Posts</h3>';
                                    posts.forEach(function(r) {{
                                        html += '<a href=""{basePath}/' + r.path + '/"" class=""search-result-item"" role=""option"" id=""search-result-' + i + '"" aria-selected=""false"" tabindex=""-1"">' +
                                            '<div class=""search-result-title"">' + highlightMatches(r.title, terms) + '</div>' +
                                            (r.excerpt ? '<div class=""search-result-excerpt"">' + highlightMatches(r.excerpt, terms) + '</div>' : '') +
                                            '</a>';
                                        i++;
                                    }});
                                    html += '</div>';
                                }}
                                searchModalResults.innerHTML = html;
                                searchModalStatus.textContent = total + (total === 1 ? ' result found.' : ' results found.');
                            }}
                            searchModalInput.setAttribute('aria-expanded', 'true');
                        }})
                        ['catch'](function() {{
                            if (requestId !== searchRequestId) return;
                            searchModalResults.innerHTML = '<div class=""search-result-empty"" role=""status"">Something went wrong. Try again.</div>';
                            searchModalStatus.textContent = 'Search failed.';
                        }});
                }}, 200);
            }});

            // Wrap wide tables in scroll container; overflow-x:auto on table itself doesn't reliably contain it.
            var tables = document.querySelectorAll('.content table');
            tables.forEach(function(table) {{
                var wrapper = document.createElement('div');
                wrapper.className = 'table-wrapper';
                table.parentNode.insertBefore(wrapper, table);
                wrapper.appendChild(table);
            }});


            var codeBlocks = document.querySelectorAll('.content pre');
            codeBlocks.forEach(function(pre) {{
                var wrapper = document.createElement('div');
                wrapper.className = 'code-block-wrapper';
                pre.parentNode.insertBefore(wrapper, pre);
                wrapper.appendChild(pre);

                var buttons = document.createElement('div');
                buttons.className = 'code-block-buttons';

                var iconCopy = '<svg xmlns=""http://www.w3.org/2000/svg"" width=""17"" height=""17"" viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""2"" stroke-linecap=""round"" stroke-linejoin=""round"" aria-hidden=""true""><rect width=""14"" height=""14"" x=""8"" y=""8"" rx=""2"" ry=""2""/><path d=""M4 16c-1.1 0-2-.9-2-2V4c0-1.1.9-2 2-2h10c1.1 0 2 .9 2 2""/></svg>';
                var iconCheck = '<svg xmlns=""http://www.w3.org/2000/svg"" width=""17"" height=""17"" viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""2.5"" stroke-linecap=""round"" stroke-linejoin=""round"" aria-hidden=""true""><path d=""M20 6 9 17l-5-5""/></svg>';
                var iconX = '<svg xmlns=""http://www.w3.org/2000/svg"" width=""17"" height=""17"" viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""2"" stroke-linecap=""round"" stroke-linejoin=""round"" aria-hidden=""true""><path d=""M18 6 6 18""/><path d=""m6 6 12 12""/></svg>';
                var copyBtn = document.createElement('button');
                copyBtn.innerHTML = iconCopy;
                copyBtn.setAttribute('aria-label', 'Copy code');
                copyBtn.setAttribute('title', 'Copy code');
                copyBtn.addEventListener('click', function() {{
                    var code = pre.querySelector('code');
                    var text = code ? code.textContent : pre.textContent;
                    navigator.clipboard.writeText(text).then(function() {{
                        copyBtn.innerHTML = iconCheck;
                        copyBtn.classList.add('copied');
                        setTimeout(function() {{
                            copyBtn.innerHTML = iconCopy;
                            copyBtn.classList.remove('copied');
                        }}, 2000);
                    }})['catch'](function() {{
                        copyBtn.innerHTML = iconX;
                        copyBtn.classList.add('failed');
                        setTimeout(function() {{
                            copyBtn.innerHTML = iconCopy;
                            copyBtn.classList.remove('failed');
                        }}, 2000);
                    }});
                }});
                buttons.appendChild(copyBtn);

                var downloadBtn = document.createElement('button');
                downloadBtn.innerHTML = '<svg xmlns=""http://www.w3.org/2000/svg"" width=""17"" height=""17"" viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""2"" stroke-linecap=""round"" stroke-linejoin=""round"" aria-hidden=""true""><path d=""M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4""/><polyline points=""7 10 12 15 17 10""/><line x1=""12"" x2=""12"" y1=""3"" y2=""15""/></svg>';
                downloadBtn.setAttribute('aria-label', 'Download code');
                downloadBtn.setAttribute('title', 'Download code');
                downloadBtn.addEventListener('click', function() {{
                    var code = pre.querySelector('code');
                    var text = code ? code.textContent : pre.textContent;
                    var blob = new Blob([text], {{ type: 'text/plain' }});
                    var url = URL.createObjectURL(blob);
                    var a = document.createElement('a');
                    a.href = url;
                    var langBlock = pre.closest('[class^=""language-""]');
                    a.download = (langBlock && langBlock.dataset.filename) || 'code.txt';
                    document.body.appendChild(a);
                    a.click();
                    document.body.removeChild(a);
                    URL.revokeObjectURL(url);
                }});
                buttons.appendChild(downloadBtn);

                wrapper.appendChild(buttons);
            }});

            var codeGroups = document.querySelectorAll('.teatime-code-group');
            codeGroups.forEach(function(group) {{
                var inputs = group.querySelectorAll('.tabs input');
                var labels = group.querySelectorAll('.tabs label');
                var blocks = group.querySelectorAll('.blocks > [class^=""language-""]');
                inputs.forEach(function(input, index) {{
                    input.addEventListener('change', function() {{
                        blocks.forEach(function(block, blockIndex) {{
                            block.classList.toggle('active', blockIndex === index);
                        }});
                        labels.forEach(function(label, labelIndex) {{
                            label.classList.toggle('active-tab', labelIndex === index);
                        }});
                    }});
                    if (input.checked) labels[index] && labels[index].classList.add('active-tab');
                }});
            }});

            // Mermaid ignores CSS variables; current theme must be passed in explicitly.
            var mermaidBlocks = document.querySelectorAll('.mermaid');
            if (mermaidBlocks.length && window.mermaid) {{
                var currentTheme = document.documentElement.getAttribute('data-theme');
                var prefersDarkNow = window.matchMedia('(prefers-color-scheme: dark)').matches;
                var mermaidIsDark = currentTheme ? currentTheme === 'dark' : prefersDarkNow;
                window.mermaid.initialize({{ theme: mermaidIsDark ? 'dark' : 'default' }});
                window.mermaid.run();
            }}

            var pageControlsToggle = document.querySelector('.page-controls-toggle');
            var pageControlsMenu = document.querySelector('.page-controls-menu');
            if (pageControlsToggle && pageControlsMenu) {{
                function closePageControls() {{
                    pageControlsMenu.hidden = true;
                    pageControlsToggle.setAttribute('aria-expanded', 'false');
                }}
                function openPageControls() {{
                    pageControlsMenu.hidden = false;
                    pageControlsToggle.setAttribute('aria-expanded', 'true');
                    var rect = pageControlsMenu.getBoundingClientRect();
                    if (rect.left < 8) {{
                        pageControlsMenu.style.right = 'auto';
                        pageControlsMenu.style.left = '0';
                    }} else {{
                        pageControlsMenu.style.right = '';
                        pageControlsMenu.style.left = '';
                    }}
                    var first = pageControlsMenu.querySelector('.page-controls-item');
                    if (first) first.focus();
                }}
                pageControlsToggle.addEventListener('click', function(e) {{
                    e.stopPropagation();
                    if (!pageControlsMenu.hidden) {{ closePageControls(); }} else {{ openPageControls(); }}
                }});
                document.addEventListener('click', function(e) {{
                    if (!pageControlsMenu.hidden && !pageControlsMenu.contains(e.target))
                        closePageControls();
                }});
                document.addEventListener('keydown', function(e) {{
                    if (e.key === 'Escape' && !pageControlsMenu.hidden) {{ closePageControls(); pageControlsToggle.focus(); }}
                }});
                pageControlsMenu.addEventListener('keydown', function(e) {{
                    var items = Array.prototype.slice.call(pageControlsMenu.querySelectorAll('.page-controls-item'));
                    if (e.key === 'ArrowDown' || e.key === 'ArrowUp') {{
                        e.preventDefault();
                        var idx = items.indexOf(document.activeElement);
                        idx = e.key === 'ArrowDown' ? (idx + 1) % items.length : (idx - 1 + items.length) % items.length;
                        items[idx].focus();
                    }} else if (e.key === 'Enter' || e.key === ' ') {{
                        var focused = document.activeElement;
                        if (focused && pageControlsMenu.contains(focused) && !focused.href) {{
                            e.preventDefault();
                            focused.click();
                        }}
                    }}
                }});

                var copiedHtml = '<svg viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""2"" stroke-linecap=""round"" stroke-linejoin=""round"" aria-hidden=""true"" width=""14"" height=""14""><polyline points=""20 6 9 17 4 12""/></svg>Copied!';
                var errorHtml = '<svg viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""2"" stroke-linecap=""round"" stroke-linejoin=""round"" aria-hidden=""true"" width=""14"" height=""14""><line x1=""18"" y1=""6"" x2=""6"" y2=""18""/><line x1=""6"" y1=""6"" x2=""18"" y2=""18""/></svg>Failed';
                function showCopied(btn, savedHtml) {{
                    btn.innerHTML = copiedHtml;
                    setTimeout(function() {{ closePageControls(); }}, 600);
                    setTimeout(function() {{ btn.innerHTML = savedHtml; }}, 2000);
                }}
                function showError(btn, savedHtml) {{
                    btn.innerHTML = errorHtml;
                    setTimeout(function() {{ btn.innerHTML = savedHtml; }}, 2000);
                }}

                pageControlsMenu.querySelectorAll('[data-copy-url]').forEach(function(btn) {{
                    btn.addEventListener('click', function() {{
                        if (btn.classList.contains('loading')) return;
                        var savedHtml = btn.innerHTML;
                        btn.classList.add('loading');
                        fetch(btn.getAttribute('data-copy-url'))
                            .then(function(r) {{ if (!r.ok) throw new Error(); return r.text(); }})
                            .then(function(text) {{ return navigator.clipboard.writeText(text); }})
                            .then(function() {{ btn.classList.remove('loading'); showCopied(btn, savedHtml); }})
                            ['catch'](function() {{ btn.classList.remove('loading'); showError(btn, savedHtml); }});
                    }});
                }});

                pageControlsMenu.querySelectorAll('[data-copy-value]').forEach(function(btn) {{
                    btn.addEventListener('click', function() {{
                        var savedHtml = btn.innerHTML;
                        var value = new URL(btn.getAttribute('data-copy-value'), window.location.href).href;
                        navigator.clipboard.writeText(value)
                            .then(function() {{ showCopied(btn, savedHtml); }})
                            ['catch'](function() {{ showError(btn, savedHtml); }});
                    }});
                }});
            }}

            var promoBar = document.getElementById('promo-bar');
            var promoClose = document.getElementById('promo-bar-close');
            if (promoBar && promoClose) {{
                promoClose.addEventListener('click', function() {{
                    try {{ localStorage.setItem('teatime-promo-dismissed', promoBar.getAttribute('data-promo-id') || '1'); }} catch (_) {{}}
                    var removed = false;
                    var removeBar = function() {{ if (!removed) {{ removed = true; promoBar.remove(); }} }};
                    promoBar.addEventListener('transitionend', function(e) {{
                        if (e.propertyName === 'grid-template-rows') removeBar();
                    }});
                    setTimeout(removeBar, 400);
                    promoBar.classList.add('promo-bar-hiding');
                }});
            }}

            document.querySelectorAll('.top-nav-item.has-dropdown').forEach(function(item) {{
                var btn = item.querySelector('.top-nav-link[aria-expanded]');
                if (!btn) return;
                item.addEventListener('focusin', function() {{
                    btn.setAttribute('aria-expanded', 'true');
                }});
                item.addEventListener('focusout', function() {{
                    setTimeout(function() {{
                        if (!item.matches(':focus-within')) btn.setAttribute('aria-expanded', 'false');
                    }}, 0);
                }});
                item.addEventListener('mouseenter', function() {{ btn.setAttribute('aria-expanded', 'true'); }});
                item.addEventListener('mouseleave', function() {{ btn.setAttribute('aria-expanded', 'false'); }});
            }});
        }});
    </script>
";
    }
}
