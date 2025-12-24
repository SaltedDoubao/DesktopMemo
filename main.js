/**
 * DesktopMemo Documentation - Interactive Features
 */

(function () {
    'use strict';

    const SUPPORTED_LANGS = ['zh-CN', 'en', 'ja', 'ko'];

    const I18N_STRINGS = {
        'zh-CN': {
            'header.logo': 'DesktopMemo Docs',
            'header.themeTitle': '切换主题',
            'header.langTitle': '切换语言',

            'theme.light': '浅色',
            'theme.dark': '深色',

            'lang.short.zh-CN': '中文',
            'lang.short.en': 'EN',
            'lang.short.ja': '日本語',
            'lang.short.ko': '한국어',
            'lang.option.zh-CN': '简体中文',
            'lang.option.en': 'English',
            'lang.option.ja': '日本語',
            'lang.option.ko': '한국어',

            'sidebar.section.overview': '概览',
            'sidebar.section.arch': '项目架构',
            'sidebar.section.api': 'API 文档',

            'nav.home': '首页',
            'nav.contributing': '贡献指南',
            'nav.mysql': 'MySQL 集成规范',

            'nav.arch.index': '架构首页',
            'nav.arch.diagram': '架构图',
            'nav.arch.modules': '模块划分',
            'nav.arch.tech': '技术栈和依赖',
            'nav.arch.dataflow': '数据流和通信',

            'nav.api.index': 'API 概览',
            'nav.api.repositories': '仓储接口',
            'nav.api.services': '服务接口',
            'nav.api.models': '领域模型',
            'nav.api.enums': '枚举类型',

            'ui.backToTop.title': '返回顶部',
            'ui.backToTop.aria': '返回顶部',
            'ui.copy': '复制',
            'ui.copied': '已复制 ✓',
            'ui.copyFailed': '失败',
            'ui.mermaid.close': '关闭 (Esc)',
            'ui.mermaid.hint': '按 Esc 键或点击背景关闭 · 滚动可缩放查看',
        },
        en: {
            'header.logo': 'DesktopMemo Docs',
            'header.themeTitle': 'Toggle theme',
            'header.langTitle': 'Change language',

            'theme.light': 'Light',
            'theme.dark': 'Dark',

            'lang.short.zh-CN': '中文',
            'lang.short.en': 'EN',
            'lang.short.ja': '日本語',
            'lang.short.ko': '한국어',
            'lang.option.zh-CN': '简体中文',
            'lang.option.en': 'English',
            'lang.option.ja': '日本語',
            'lang.option.ko': '한국어',

            'sidebar.section.overview': 'Overview',
            'sidebar.section.arch': 'Project Structure',
            'sidebar.section.api': 'API Docs',

            'nav.home': 'Home',
            'nav.contributing': 'Contributing',
            'nav.mysql': 'MySQL Integration',

            'nav.arch.index': 'Architecture',
            'nav.arch.diagram': 'Diagram',
            'nav.arch.modules': 'Modules',
            'nav.arch.tech': 'Tech Stack',
            'nav.arch.dataflow': 'Data Flow',

            'nav.api.index': 'API Overview',
            'nav.api.repositories': 'Repositories',
            'nav.api.services': 'Services',
            'nav.api.models': 'Models',
            'nav.api.enums': 'Enums',

            'ui.backToTop.title': 'Back to top',
            'ui.backToTop.aria': 'Back to top',
            'ui.copy': 'Copy',
            'ui.copied': 'Copied ✓',
            'ui.copyFailed': 'Failed',
            'ui.mermaid.close': 'Close (Esc)',
            'ui.mermaid.hint': 'Press Esc or click the background to close · Scroll to zoom',
        },
        ja: {
            'header.logo': 'DesktopMemo Docs',
            'header.themeTitle': 'テーマを切り替え',
            'header.langTitle': '言語を切り替え',

            'theme.light': 'ライト',
            'theme.dark': 'ダーク',

            'lang.short.zh-CN': '中文',
            'lang.short.en': 'EN',
            'lang.short.ja': '日本語',
            'lang.short.ko': '한국어',
            'lang.option.zh-CN': '简体中文',
            'lang.option.en': 'English',
            'lang.option.ja': '日本語',
            'lang.option.ko': '한국어',

            'sidebar.section.overview': '概要',
            'sidebar.section.arch': 'プロジェクト構造',
            'sidebar.section.api': 'API ドキュメント',

            'nav.home': 'ホーム',
            'nav.contributing': '貢献ガイド',
            'nav.mysql': 'MySQL 統合',

            'nav.arch.index': 'アーキテクチャ',
            'nav.arch.diagram': '図',
            'nav.arch.modules': 'モジュール',
            'nav.arch.tech': '技術スタック',
            'nav.arch.dataflow': 'データフロー',

            'nav.api.index': 'API 概要',
            'nav.api.repositories': 'リポジトリ',
            'nav.api.services': 'サービス',
            'nav.api.models': 'モデル',
            'nav.api.enums': '列挙型',

            'ui.backToTop.title': 'トップへ',
            'ui.backToTop.aria': 'トップへ戻る',
            'ui.copy': 'コピー',
            'ui.copied': 'コピー済み ✓',
            'ui.copyFailed': '失敗',
            'ui.mermaid.close': '閉じる (Esc)',
            'ui.mermaid.hint': 'Esc キーまたは背景クリックで閉じる · スクロールで拡大/縮小',
        },
        ko: {
            'header.logo': 'DesktopMemo Docs',
            'header.themeTitle': '테마 전환',
            'header.langTitle': '언어 전환',

            'theme.light': '라이트',
            'theme.dark': '다크',

            'lang.short.zh-CN': '中文',
            'lang.short.en': 'EN',
            'lang.short.ja': '日本語',
            'lang.short.ko': '한국어',
            'lang.option.zh-CN': '简体中文',
            'lang.option.en': 'English',
            'lang.option.ja': '日本語',
            'lang.option.ko': '한국어',

            'sidebar.section.overview': '개요',
            'sidebar.section.arch': '프로젝트 구조',
            'sidebar.section.api': 'API 문서',

            'nav.home': '홈',
            'nav.contributing': '기여 가이드',
            'nav.mysql': 'MySQL 통합',

            'nav.arch.index': '아키텍처',
            'nav.arch.diagram': '다이어그램',
            'nav.arch.modules': '모듈',
            'nav.arch.tech': '기술 스택',
            'nav.arch.dataflow': '데이터 흐름',

            'nav.api.index': 'API 개요',
            'nav.api.repositories': '리포지토리',
            'nav.api.services': '서비스',
            'nav.api.models': '모델',
            'nav.api.enums': '열거형',

            'ui.backToTop.title': '맨 위로',
            'ui.backToTop.aria': '맨 위로 이동',
            'ui.copy': '복사',
            'ui.copied': '복사됨 ✓',
            'ui.copyFailed': '실패',
            'ui.mermaid.close': '닫기 (Esc)',
            'ui.mermaid.hint': 'Esc 키 또는 배경 클릭으로 닫기 · 스크롤로 확대/축소',
        },
    };

    const SIDEBAR_LINK_KEY_BY_SUFFIX = [
        { suffix: '/project_structure/data_flow.html', key: 'nav.arch.dataflow' },
        { suffix: '/project_structure/tech_stack.html', key: 'nav.arch.tech' },
        { suffix: '/project_structure/modules.html', key: 'nav.arch.modules' },
        { suffix: '/project_structure/architecture.html', key: 'nav.arch.diagram' },
        { suffix: '/project_structure/index.html', key: 'nav.arch.index' },

        { suffix: '/api/repositories.html', key: 'nav.api.repositories' },
        { suffix: '/api/services.html', key: 'nav.api.services' },
        { suffix: '/api/models.html', key: 'nav.api.models' },
        { suffix: '/api/enums.html', key: 'nav.api.enums' },
        { suffix: '/api/index.html', key: 'nav.api.index' },

        { suffix: '/mysql_integration.html', key: 'nav.mysql' },
        { suffix: '/contributing.html', key: 'nav.contributing' },
        { suffix: '/index.html', key: 'nav.home' },
    ];

    let currentLang = 'zh-CN';

    function normalizeLang(lang) {
        const raw = (lang || '').toString().trim();
        if (!raw) return 'zh-CN';

        const lower = raw.toLowerCase();
        if (lower === 'zh' || lower.startsWith('zh-')) return 'zh-CN';
        if (lower === 'en' || lower.startsWith('en-')) return 'en';
        if (lower === 'ja' || lower.startsWith('ja-')) return 'ja';
        if (lower === 'ko' || lower.startsWith('ko-')) return 'ko';
        return 'zh-CN';
    }

    function getSavedLang() {
        try {
            return localStorage.getItem('lang');
        } catch {
            return null;
        }
    }

    function setSavedLang(lang) {
        try {
            localStorage.setItem('lang', lang);
        } catch {
            // ignore
        }
    }

    function detectPreferredLang() {
        const urlLang = new URLSearchParams(window.location.search).get('lang');
        if (urlLang) return normalizeLang(urlLang);

        const saved = getSavedLang();
        if (saved) return normalizeLang(saved);

        const langs = Array.isArray(navigator.languages) && navigator.languages.length
            ? navigator.languages
            : [navigator.language || navigator.userLanguage].filter(Boolean);

        for (const lang of langs) {
            const normalized = normalizeLang(lang);
            if (SUPPORTED_LANGS.includes(normalized)) return normalized;
        }

        return 'zh-CN';
    }

    function t(key) {
        const langTable = I18N_STRINGS[currentLang] || I18N_STRINGS['zh-CN'];
        return (langTable && langTable[key]) || I18N_STRINGS['zh-CN'][key] || key;
    }

    function applyLangAttributes(lang) {
        document.documentElement.setAttribute('data-lang', lang);
        document.documentElement.lang = lang;
    }

    function getBasePath() {
        const path = (window.location.pathname || '').replace(/\\/g, '/');
        if (path.includes('/api/') || path.includes('/project_structure/')) return '../';
        return '';
    }

    function prepareSidebarI18nKeys() {
        const sidebar = document.querySelector('.sidebar');
        if (!sidebar) return;

        const headings = sidebar.querySelectorAll('h3');
        if (headings[0] && !headings[0].dataset.i18n) headings[0].dataset.i18n = 'sidebar.section.overview';
        if (headings[1] && !headings[1].dataset.i18n) headings[1].dataset.i18n = 'sidebar.section.arch';
        if (headings[2] && !headings[2].dataset.i18n) headings[2].dataset.i18n = 'sidebar.section.api';

        sidebar.querySelectorAll('a[href]').forEach(a => {
            if (a.dataset.i18n) return;

            const href = a.getAttribute('href');
            if (!href || href.startsWith('#')) return;

            let path;
            try {
                path = new URL(href, window.location.href).pathname || '';
            } catch {
                return;
            }

            const matched = SIDEBAR_LINK_KEY_BY_SUFFIX.find(x => path.endsWith(x.suffix));
            if (!matched) return;

            a.dataset.i18n = matched.key;

            const text = (a.textContent || '').trim();
            const prefixMatch = text.match(/^(\d+)(?:\.(?=\s)|\s)\s*/);
            if (prefixMatch) {
                const number = prefixMatch[1];
                const hasDot = text.startsWith(number + '.');
                a.dataset.i18nPrefix = hasDot ? `${number}.` : number;
            }
        });
    }

    function applyI18n() {
        document.querySelectorAll('[data-i18n]').forEach(el => {
            const key = el.dataset.i18n;
            if (key) el.textContent = t(key);
        });

        document.querySelectorAll('[data-i18n-title]').forEach(el => {
            const key = el.dataset.i18nTitle;
            if (key) el.title = t(key);
        });

        document.querySelectorAll('[data-i18n-aria-label]').forEach(el => {
            const key = el.dataset.i18nAriaLabel;
            if (key) el.setAttribute('aria-label', t(key));
        });

        document.querySelectorAll('.sidebar a[data-i18n]').forEach(a => {
            const key = a.dataset.i18n;
            const prefix = (a.dataset.i18nPrefix || '').trim();
            if (!key) return;
            a.textContent = prefix ? `${prefix} ${t(key)}` : t(key);
        });
    }

    function updateLangUI(lang) {
        const langText = document.getElementById('lang-text');
        if (langText) {
            const key = `lang.short.${lang}`;
            langText.dataset.i18n = key;
            langText.textContent = t(key);
        }

        document.querySelectorAll('.lang-option').forEach(opt => {
            opt.classList.toggle('active', opt.dataset.lang === lang);
        });
    }

    function updateThemeText() {
        const theme = document.documentElement.getAttribute('data-theme') || 'light';
        const themeText = document.getElementById('theme-text');
        if (!themeText) return;

        const key = theme === 'dark' ? 'theme.dark' : 'theme.light';
        themeText.dataset.i18n = key;
        themeText.textContent = t(key);
    }

    // ========== DOM Ready ==========
    document.addEventListener('DOMContentLoaded', init);

    function init() {
        currentLang = detectPreferredLang();
        applyLangAttributes(currentLang);
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
        if (document.querySelector('.header')) return;

        const basePath = getBasePath();
        const currentLangShortKey = `lang.short.${currentLang}`;

        const langOptionsHTML = SUPPORTED_LANGS.map(code => {
            const active = code === currentLang ? 'active' : '';
            const labelKey = `lang.option.${code}`;
            return `<button class="lang-option ${active}" data-lang="${code}" data-i18n="${labelKey}">${t(labelKey)}</button>`;
        }).join('');

        // Create header element
        const header = document.createElement('header');
        header.className = 'header';
        header.innerHTML = `
            <div class="header-left">
                <a href="${basePath}index.html" class="header-logo" data-i18n="header.logo">${t('header.logo')}</a>
            </div>
            <div class="header-right">
                <button class="header-btn" id="theme-toggle" title="${t('header.themeTitle')}" data-i18n-title="header.themeTitle">
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
                    <span class="btn-text" id="theme-text" data-i18n="theme.light">${t('theme.light')}</span>
                </button>
                <div class="lang-dropdown" id="lang-dropdown">
                    <button class="header-btn" id="lang-toggle" title="${t('header.langTitle')}" data-i18n-title="header.langTitle">
                        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                            <circle cx="12" cy="12" r="10"></circle>
                            <line x1="2" y1="12" x2="22" y2="12"></line>
                            <path d="M12 2a15.3 15.3 0 0 1 4 10 15.3 15.3 0 0 1-4 10 15.3 15.3 0 0 1-4-10 15.3 15.3 0 0 1 4-10z"></path>
                        </svg>
                        <span class="btn-text" id="lang-text" data-i18n="${currentLangShortKey}">${t(currentLangShortKey)}</span>
                    </button>
                    <div class="lang-menu">
                        ${langOptionsHTML}
                    </div>
                </div>
            </div>
        `;
        document.body.insertBefore(header, document.body.firstChild);

        // Language dropdown toggle
        const langDropdown = document.getElementById('lang-dropdown');
        const langToggle = document.getElementById('lang-toggle');

        if (!langDropdown || !langToggle) return;

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

        if (iconLight && iconDark) {
            if (theme === 'dark') {
                iconLight.style.display = 'none';
                iconDark.style.display = 'block';
            } else {
                iconLight.style.display = 'block';
                iconDark.style.display = 'none';
            }
        }

        updateThemeText();
    }

    // ========== Language Toggle ==========
    function initLanguage() {
        setLanguage(currentLang);

        // Language option buttons
        document.querySelectorAll('.lang-option').forEach(btn => {
            btn.addEventListener('click', () => {
                const selectedLang = btn.dataset.lang;
                if (!selectedLang) return;

                setLanguage(selectedLang);

                const dropdown = document.getElementById('lang-dropdown');
                if (dropdown) dropdown.classList.remove('open');
            });
        });
    }

    function setLanguage(lang) {
        currentLang = normalizeLang(lang);
        setSavedLang(currentLang);
        applyLangAttributes(currentLang);

        prepareSidebarI18nKeys();
        applyI18n();
        updateLangUI(currentLang);
        updateThemeText();
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
        btn.dataset.i18nAriaLabel = 'ui.backToTop.aria';
        btn.dataset.i18nTitle = 'ui.backToTop.title';
        btn.setAttribute('aria-label', t('ui.backToTop.aria'));
        btn.title = t('ui.backToTop.title');
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
            btn.dataset.i18n = 'ui.copy';
            btn.textContent = t('ui.copy');

            btn.addEventListener('click', async () => {
                const code = pre.querySelector('code');
                const text = code ? code.textContent : pre.textContent;

                try {
                    await navigator.clipboard.writeText(text);
                    btn.dataset.i18n = 'ui.copied';
                    btn.textContent = t('ui.copied');
                    btn.style.background = 'rgba(40, 200, 64, 0.3)';

                    setTimeout(() => {
                        btn.dataset.i18n = 'ui.copy';
                        btn.textContent = t('ui.copy');
                        btn.style.background = '';
                    }, 2000);
                } catch (err) {
                    btn.dataset.i18n = 'ui.copyFailed';
                    btn.textContent = t('ui.copyFailed');
                    setTimeout(() => {
                        btn.dataset.i18n = 'ui.copy';
                        btn.textContent = t('ui.copy');
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
            <button class="mermaid-modal-close" title="${t('ui.mermaid.close')}" data-i18n-title="ui.mermaid.close">✕</button>
            <div class="mermaid-modal-content"></div>
            <div class="mermaid-modal-hint" data-i18n="ui.mermaid.hint">${t('ui.mermaid.hint')}</div>
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
