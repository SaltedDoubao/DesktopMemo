/**
 * DesktopMemo Documentation - Interactive Features
 */

(function () {
    'use strict';

    // ========== DOM Ready ==========
    document.addEventListener('DOMContentLoaded', init);

    function init() {
        initHeader();
        initTheme();
        initLanguage();
        initBackToTop();
        initSmoothScrolling();
        initCodeCopyButtons();
        initActiveNavHighlight();
        initTableOfContents();
        initSearchHighlight();
        initProgressBar();
        initSyntaxHighlighting();
        initMermaidViewer();
    }

    // ========== Header Bar ==========
    function initHeader() {
        // Create header element
        const header = document.createElement('header');
        header.className = 'header';
        header.innerHTML = `
            <div class="header-left">
                <a href="/" class="header-logo">DesktopMemo Docs</a>
            </div>
            <div class="header-right">
                <button class="header-btn" id="theme-toggle" title="切换主题">
                    <svg id="theme-icon-light" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                        <circle cx="12" cy="12" r="5"></circle>
                        <line x1="12" y1="1" x2="12" y2="3"></line>
                        <line x1="12" y1="21" x2="12" y2="23"></line>
                        <line x1="4.22" y1="4.22" x2="5.64" y2="5.64"></line>
                        <line x1="18.36" y1="18.36" x2="19.78" y2="19.78"></line>
                        <line x1="1" y1="12" x2="3" y2="12"></line>
                        <line x1="21" y1="12" x2="23" y2="12"></line>
                        <line x1="4.22" y1="19.78" x2="5.64" y2="18.36"></line>
                        <line x1="18.36" y1="5.64" x2="19.78" y2="4.22"></line>
                    </svg>
                    <svg id="theme-icon-dark" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="display:none;">
                        <path d="M21 12.79A9 9 0 1 1 11.21 3 7 7 0 0 0 21 12.79z"></path>
                    </svg>
                    <span class="btn-text" id="theme-text">浅色</span>
                </button>
                <div class="lang-dropdown" id="lang-dropdown">
                    <button class="header-btn" id="lang-toggle" title="切换语言">
                        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                            <circle cx="12" cy="12" r="10"></circle>
                            <line x1="2" y1="12" x2="22" y2="12"></line>
                            <path d="M12 2a15.3 15.3 0 0 1 4 10 15.3 15.3 0 0 1-4 10 15.3 15.3 0 0 1-4-10 15.3 15.3 0 0 1 4-10z"></path>
                        </svg>
                        <span class="btn-text" id="lang-text">中文</span>
                    </button>
                    <div class="lang-menu">
                        <button class="lang-option active" data-lang="zh-CN">简体中文</button>
                        <button class="lang-option" data-lang="en">English</button>
                    </div>
                </div>
            </div>
        `;
        document.body.insertBefore(header, document.body.firstChild);

        // Language dropdown toggle
        const langDropdown = document.getElementById('lang-dropdown');
        const langToggle = document.getElementById('lang-toggle');

        langToggle.addEventListener('click', (e) => {
            e.stopPropagation();
            langDropdown.classList.toggle('open');
        });

        document.addEventListener('click', () => {
            langDropdown.classList.remove('open');
        });
    }

    // ========== Theme Toggle ==========
    function initTheme() {
        // Detect system preference
        const prefersDark = window.matchMedia('(prefers-color-scheme: dark)');
        const savedTheme = localStorage.getItem('theme');

        // Apply initial theme
        if (savedTheme) {
            setTheme(savedTheme);
        } else {
            // Follow system theme by default
            setTheme(prefersDark.matches ? 'dark' : 'light');
        }

        // Listen for system theme changes
        prefersDark.addEventListener('change', (e) => {
            if (!localStorage.getItem('theme')) {
                setTheme(e.matches ? 'dark' : 'light');
            }
        });

        // Theme toggle button
        document.getElementById('theme-toggle').addEventListener('click', () => {
            const current = document.documentElement.getAttribute('data-theme');
            const newTheme = current === 'dark' ? 'light' : 'dark';
            setTheme(newTheme);
            localStorage.setItem('theme', newTheme);
        });
    }

    function setTheme(theme) {
        document.documentElement.setAttribute('data-theme', theme);

        const iconLight = document.getElementById('theme-icon-light');
        const iconDark = document.getElementById('theme-icon-dark');
        const themeText = document.getElementById('theme-text');

        if (iconLight && iconDark) {
            if (theme === 'dark') {
                iconLight.style.display = 'none';
                iconDark.style.display = 'block';
                if (themeText) themeText.textContent = '深色';
            } else {
                iconLight.style.display = 'block';
                iconDark.style.display = 'none';
                if (themeText) themeText.textContent = '浅色';
            }
        }
    }

    // ========== Language Toggle ==========
    function initLanguage() {
        // Detect system language
        const systemLang = navigator.language || navigator.userLanguage;
        const savedLang = localStorage.getItem('lang');

        // Apply initial language
        const lang = savedLang || (systemLang.startsWith('zh') ? 'zh-CN' : 'en');
        setLanguage(lang);

        // Language option buttons
        document.querySelectorAll('.lang-option').forEach(btn => {
            btn.addEventListener('click', () => {
                const selectedLang = btn.dataset.lang;
                setLanguage(selectedLang);
                localStorage.setItem('lang', selectedLang);
                document.getElementById('lang-dropdown').classList.remove('open');
            });
        });
    }

    function setLanguage(lang) {
        const langText = document.getElementById('lang-text');
        const langOptions = document.querySelectorAll('.lang-option');

        langOptions.forEach(opt => {
            opt.classList.toggle('active', opt.dataset.lang === lang);
        });

        if (langText) {
            langText.textContent = lang.startsWith('zh') ? '中文' : 'EN';
        }

        // Store for potential future i18n use
        document.documentElement.setAttribute('data-lang', lang);
    }

    // ========== Syntax Highlighting with Prism.js ==========
    function initSyntaxHighlighting() {
        // Load Prism.js from CDN
        const prismCore = document.createElement('script');
        prismCore.src = 'https://cdn.jsdelivr.net/npm/prismjs@1.29.0/prism.min.js';
        prismCore.async = true;

        prismCore.onload = () => {
            // Load C# language support
            const prismCsharp = document.createElement('script');
            prismCsharp.src = 'https://cdn.jsdelivr.net/npm/prismjs@1.29.0/components/prism-csharp.min.js';
            prismCsharp.async = true;

            prismCsharp.onload = () => {
                // Load additional languages
                loadPrismLanguage('bash');
                loadPrismLanguage('powershell');
                loadPrismLanguage('json');
                loadPrismLanguage('sql');
                loadPrismLanguage('xml-doc');

                // Highlight all code blocks after a short delay
                setTimeout(() => {
                    if (window.Prism) {
                        Prism.highlightAll();
                    }
                }, 100);
            };

            document.head.appendChild(prismCsharp);
        };

        document.head.appendChild(prismCore);
    }

    function loadPrismLanguage(lang) {
        const script = document.createElement('script');
        script.src = `https://cdn.jsdelivr.net/npm/prismjs@1.29.0/components/prism-${lang}.min.js`;
        script.async = true;
        document.head.appendChild(script);
    }

    // ========== Back to Top Button ==========
    function initBackToTop() {
        const btn = document.createElement('button');
        btn.className = 'back-to-top';
        btn.innerHTML = '↑';
        btn.setAttribute('aria-label', 'Back to top');
        btn.title = '返回顶部';
        document.body.appendChild(btn);

        window.addEventListener('scroll', () => {
            if (window.scrollY > 400) {
                btn.classList.add('visible');
            } else {
                btn.classList.remove('visible');
            }
        });

        btn.addEventListener('click', () => {
            window.scrollTo({ top: 0, behavior: 'smooth' });
        });
    }

    // ========== Smooth Scrolling for Anchors ==========
    function initSmoothScrolling() {
        document.querySelectorAll('a[href^="#"]').forEach(anchor => {
            anchor.addEventListener('click', function (e) {
                const targetId = this.getAttribute('href');
                if (targetId === '#') return;

                const target = document.querySelector(targetId);
                if (target) {
                    e.preventDefault();
                    target.scrollIntoView({
                        behavior: 'smooth',
                        block: 'start'
                    });
                }
            });
        });
    }

    // ========== Code Copy Buttons ==========
    function initCodeCopyButtons() {
        document.querySelectorAll('pre').forEach(pre => {
            // Create wrapper for positioning
            pre.style.position = 'relative';

            const btn = document.createElement('button');
            btn.className = 'copy-button';
            btn.textContent = '复制';

            btn.addEventListener('click', async () => {
                const code = pre.querySelector('code');
                const text = code ? code.textContent : pre.textContent;

                try {
                    await navigator.clipboard.writeText(text);
                    btn.textContent = '已复制 ✓';
                    btn.style.background = 'rgba(40, 200, 64, 0.3)';

                    setTimeout(() => {
                        btn.textContent = '复制';
                        btn.style.background = '';
                    }, 2000);
                } catch (err) {
                    btn.textContent = '失败';
                    setTimeout(() => {
                        btn.textContent = '复制';
                    }, 2000);
                }
            });

            pre.appendChild(btn);
        });
    }

    // ========== Active Navigation Highlight ==========
    function initActiveNavHighlight() {
        const currentPath = window.location.pathname;
        const currentFile = currentPath.split('/').pop() || 'index.html';
        const currentDir = currentPath.substring(0, currentPath.lastIndexOf('/') + 1);
        const links = document.querySelectorAll('.sidebar a');

        // Remove all existing active classes first
        links.forEach(link => link.classList.remove('active'));

        let bestMatch = null;
        let bestMatchScore = -1;

        links.forEach(link => {
            const href = link.getAttribute('href');
            if (!href || href.startsWith('#')) return;

            // Resolve the href relative to current page
            const linkUrl = new URL(href, window.location.href);
            const linkPath = linkUrl.pathname;
            const linkFile = linkPath.split('/').pop() || 'index.html';
            const linkDir = linkPath.substring(0, linkPath.lastIndexOf('/') + 1);

            // Calculate match score
            let score = 0;

            // Exact path match - highest priority
            if (linkPath === currentPath) {
                score = 100;
            }
            // Same directory and same filename
            else if (linkDir === currentDir && linkFile === currentFile) {
                score = 90;
            }
            // For index.html, ensure directory matches exactly
            else if (currentFile === 'index.html' && linkFile === 'index.html') {
                if (linkDir === currentDir) {
                    score = 80;
                }
                // Don't match parent/child index.html files
            }
            // Same filename in same directory (non-index files)
            else if (linkFile === currentFile && linkDir === currentDir) {
                score = 70;
            }

            if (score > bestMatchScore) {
                bestMatchScore = score;
                bestMatch = link;
            }
        });

        if (bestMatch) {
            bestMatch.classList.add('active');
        }
    }

    // ========== Table of Contents (On-page TOC) ==========
    function initTableOfContents() {
        const headings = document.querySelectorAll('.content-inner h2, .content-inner h3');
        if (headings.length < 3) return; // Don't show TOC for short pages

        // Add IDs to headings if they don't have them
        headings.forEach((heading, index) => {
            if (!heading.id) {
                heading.id = 'section-' + index;
            }
        });

        // Highlight current section on scroll
        let ticking = false;
        window.addEventListener('scroll', () => {
            if (!ticking) {
                requestAnimationFrame(() => {
                    updateActiveSection(headings);
                    ticking = false;
                });
                ticking = true;
            }
        });
    }

    function updateActiveSection(headings) {
        const scrollPos = window.scrollY + 100;

        headings.forEach(heading => {
            const section = heading;
            const sectionTop = section.offsetTop;
            const sectionHeight = section.offsetHeight;

            if (scrollPos >= sectionTop && scrollPos < sectionTop + sectionHeight + 200) {
                // Could highlight sidebar link or TOC here
            }
        });
    }

    // ========== Search Term Highlight ==========
    function initSearchHighlight() {
        const urlParams = new URLSearchParams(window.location.search);
        const searchTerm = urlParams.get('highlight') || urlParams.get('search');

        if (searchTerm) {
            highlightText(searchTerm);
        }
    }

    function highlightText(term) {
        const content = document.querySelector('.content-inner');
        if (!content) return;

        const walker = document.createTreeWalker(
            content,
            NodeFilter.SHOW_TEXT,
            null,
            false
        );

        const regex = new RegExp(`(${escapeRegex(term)})`, 'gi');
        const nodesToReplace = [];

        while (walker.nextNode()) {
            const node = walker.currentNode;
            if (regex.test(node.textContent)) {
                nodesToReplace.push(node);
            }
        }

        nodesToReplace.forEach(node => {
            const span = document.createElement('span');
            span.innerHTML = node.textContent.replace(regex, '<mark>$1</mark>');
            node.parentNode.replaceChild(span, node);
        });
    }

    function escapeRegex(string) {
        return string.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
    }

    // ========== Reading Progress Bar ==========
    function initProgressBar() {
        const progressBar = document.createElement('div');
        progressBar.className = 'reading-progress';
        progressBar.style.cssText = `
            position: fixed;
            top: 0;
            left: 0;
            height: 3px;
            background: linear-gradient(90deg, #2563eb, #dc4a2a);
            z-index: 9999;
            transition: width 0.1s ease;
            width: 0%;
        `;
        document.body.appendChild(progressBar);

        window.addEventListener('scroll', () => {
            const scrollTop = window.scrollY;
            const docHeight = document.documentElement.scrollHeight - window.innerHeight;
            const progress = (scrollTop / docHeight) * 100;
            progressBar.style.width = Math.min(progress, 100) + '%';
        });
    }

    // ========== Keyboard Shortcuts ==========
    document.addEventListener('keydown', (e) => {
        // Ctrl/Cmd + K for search (future feature)
        if ((e.ctrlKey || e.metaKey) && e.key === 'k') {
            e.preventDefault();
            // Could open search modal here
        }

        // Press 't' to scroll to top
        if (e.key === 't' && !e.ctrlKey && !e.metaKey &&
            !['INPUT', 'TEXTAREA'].includes(document.activeElement.tagName)) {
            window.scrollTo({ top: 0, behavior: 'smooth' });
        }
    });

    // ========== Mermaid Diagram Viewer ==========
    function initMermaidViewer() {
        // Create modal element
        const modal = document.createElement('div');
        modal.className = 'mermaid-modal';
        modal.innerHTML = `
            <button class="mermaid-modal-close" title="关闭 (Esc)">✕</button>
            <div class="mermaid-modal-content"></div>
            <div class="mermaid-modal-hint">按 Esc 键或点击背景关闭 · 滚动可缩放查看</div>
        `;
        document.body.appendChild(modal);

        const modalContent = modal.querySelector('.mermaid-modal-content');
        const closeBtn = modal.querySelector('.mermaid-modal-close');

        // Close modal function
        function closeModal() {
            modal.classList.remove('active');
            document.body.style.overflow = '';
        }

        // Open modal function
        function openModal(mermaidEl) {
            // Clone the SVG content
            const svg = mermaidEl.querySelector('svg');
            if (svg) {
                const clonedSvg = svg.cloneNode(true);
                // Scale up for better viewing
                clonedSvg.style.minWidth = '800px';
                clonedSvg.style.width = 'auto';
                clonedSvg.style.height = 'auto';
                clonedSvg.style.maxWidth = 'none';
                modalContent.innerHTML = '';
                modalContent.appendChild(clonedSvg);
            } else {
                // Fallback: clone the whole mermaid content
                modalContent.innerHTML = mermaidEl.innerHTML;
            }

            modal.classList.add('active');
            document.body.style.overflow = 'hidden';
        }

        // Add click handlers to all mermaid diagrams
        document.querySelectorAll('.mermaid').forEach(mermaidEl => {
            mermaidEl.addEventListener('click', () => {
                openModal(mermaidEl);
            });
        });

        // Close on button click
        closeBtn.addEventListener('click', closeModal);

        // Close on background click
        modal.addEventListener('click', (e) => {
            if (e.target === modal) {
                closeModal();
            }
        });

        // Close on Escape key
        document.addEventListener('keydown', (e) => {
            if (e.key === 'Escape' && modal.classList.contains('active')) {
                closeModal();
            }
        });
    }

})();
