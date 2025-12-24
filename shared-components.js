/**
 * Shared Components Loader
 * Dynamically loads and injects shared HTML components (sidebar, header) to avoid duplication
 */
(function () {
    'use strict';

    // Configuration based on current page depth
    function getBasePath() {
        const path = window.location.pathname;
        const depth = (path.match(/\//g) || []).length;

        // For file:// protocol, calculate from path segments
        const segments = path.split('/').filter(s => s && !s.includes('.html'));

        // Check if we're in a subdirectory
        if (path.includes('/api/') || path.includes('/project_structure/')) {
            return '../';
        }
        return '';
    }

    // Sidebar navigation structure
    function getSidebarHTML(basePath) {
        return `
            <div class="sidebar-logo">DesktopMemo</div>

            <h3>概览</h3>
            <ul>
                <li><a href="${basePath}index.html">首页</a></li>
                <li><a href="${basePath}contributing.html">贡献指南</a></li>
                <li><a href="${basePath}mysql_integration.html">MySQL 集成规范</a></li>
            </ul>

            <h3>项目架构</h3>
            <ul>
                <li><a href="${basePath}project_structure/index.html">架构首页</a></li>
                <li><a href="${basePath}project_structure/architecture.html">01 架构图</a></li>
                <li><a href="${basePath}project_structure/modules.html">02 模块划分</a></li>
                <li><a href="${basePath}project_structure/tech_stack.html">03 技术栈和依赖</a></li>
                <li><a href="${basePath}project_structure/data_flow.html">04 数据流和通信</a></li>
            </ul>

            <h3>API 文档</h3>
            <ul>
                <li><a href="${basePath}api/index.html">API 概览</a></li>
                <li><a href="${basePath}api/repositories.html">仓储接口</a></li>
                <li><a href="${basePath}api/services.html">服务接口</a></li>
                <li><a href="${basePath}api/models.html">领域模型</a></li>
                <li><a href="${basePath}api/enums.html">枚举类型</a></li>
            </ul>
        `;
    }

    // Header HTML structure
    function getHeaderHTML(basePath) {
        return `
            <div class="header-left">
                <a href="${basePath}index.html" class="header-logo">DesktopMemo Docs</a>
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
    }

    // Initialize shared components
    window.initSharedComponents = function () {
        const basePath = getBasePath();

        // Inject sidebar content if sidebar exists but is empty or has placeholder
        const sidebar = document.querySelector('.sidebar');
        if (sidebar && sidebar.dataset.shared !== 'false') {
            sidebar.innerHTML = getSidebarHTML(basePath);
        }

        // Create and inject header if it doesn't exist
        let header = document.querySelector('.header');
        if (!header) {
            header = document.createElement('header');
            header.className = 'header';
            header.innerHTML = getHeaderHTML(basePath);
            document.body.insertBefore(header, document.body.firstChild);
        }
    };

    // Auto-init when DOM is ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', window.initSharedComponents);
    } else {
        window.initSharedComponents();
    }
})();
