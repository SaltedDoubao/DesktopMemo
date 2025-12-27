import { defineConfig } from "vitepress";

const base = "/DesktopMemo/";

export default defineConfig({
  lang: "zh-CN",
  title: "DesktopMemo 文档",
  description: "DesktopMemo（桌面便签）项目文档：用户手册、开发者指南、架构与 API。",
  base,

  locales: {
    root: { label: "简体中文", lang: "zh-CN" },
    en: { label: "English", lang: "en-US", link: "/en/" },
  },

  lastUpdated: true,
  cleanUrls: true,

  // 将静态构建输出到仓库根目录的 docs/，方便 GitHub Pages 选择 “/docs” 目录部署
  outDir: "docs",
  srcExclude: ["docs/**", "legacy-site-*/**", "**/node_modules/**", "_*.md"],

  markdown: {
    // Shiki 语法高亮：为不内置的语言补充加载/别名，避免构建时告警
    languageAlias: {
      xaml: "xml",
    },
    config(md) {
      const originalFence = md.renderer.rules.fence;

      md.renderer.rules.fence = (tokens, idx, options, env, self) => {
        const token = tokens[idx];
        const info = (token.info || "").trim();

        if (info === "mermaid") {
          // 将 mermaid 代码进行 Base64 编码，存储在 data-src 属性中
          // 这样可以完全避免 Vue 编译器解析 mermaid 代码中的 <Memo> 等语法
          // 使用 btoa 进行编码，处理 UTF-8 字符
          const base64Code = btoa(unescape(encodeURIComponent(token.content)));
          return `<div class="mermaid" data-src="${base64Code}"></div>`;
        }

        if (originalFence) return originalFence(tokens, idx, options, env, self);
        return self.renderToken(tokens, idx, options);
      };
    },
  },

  head: [
    ["link", { rel: "icon", href: `${base}logo.svg` }],
    ["meta", { name: "theme-color", content: "#2563eb" }],
  ],

  themeConfig: {
    logo: "/logo.svg",
    siteTitle: "DesktopMemo",

    nav: [
      { text: "用户手册", link: "/Guide/README" },
      { text: "开发者指南", link: "/Dev/README" },
      { text: "架构", link: "/project_structure/README" },
      { text: "API", link: "/api/README" },
      {
        text: "更多",
        items: [
          { text: "贡献指南", link: "/CONTRIBUTING" },
          { text: "MySQL 集成规范", link: "/MySQL-集成规范" },
        ],
      },
      { text: "Releases", link: "https://github.com/SaltedDoubao/DesktopMemo/releases" },
    ],

    sidebar: {
      "/Guide/": [
        {
          text: "用户手册",
          items: [
            { text: "导读", link: "/Guide/README" },
            { text: "安装与更新", link: "/Guide/01_安装与更新" },
            { text: "基础使用", link: "/Guide/02_基础使用" },
            { text: "快捷键", link: "/Guide/03_快捷键" },
            { text: "数据与备份", link: "/Guide/04_数据与备份" },
            { text: "常见问题", link: "/Guide/05_常见问题" },
            { text: "隐私与安全", link: "/Guide/06_隐私与安全" },
          ],
        },
      ],
      "/Dev/": [
        {
          text: "开发者指南",
          items: [
            { text: "导读", link: "/Dev/README" },
            { text: "开发环境", link: "/Dev/01_开发环境" },
            { text: "构建与发布", link: "/Dev/02_构建与发布" },
            { text: "调试与日志", link: "/Dev/03_调试与日志" },
            { text: "数据迁移", link: "/Dev/04_数据迁移" },
          ],
        },
        {
          text: "协作与规范",
          items: [
            { text: "贡献指南", link: "/CONTRIBUTING" },
          ],
        },
      ],
      "/project_structure/": [
        {
          text: "项目架构",
          items: [
            { text: "索引", link: "/project_structure/README" },
            { text: "01 架构图", link: "/project_structure/01_架构图" },
            { text: "02 模块划分", link: "/project_structure/02_模块划分" },
            { text: "03 技术栈和依赖", link: "/project_structure/03_技术栈和依赖" },
            { text: "04 数据流和通信", link: "/project_structure/04_数据流和通信" },
          ],
        },
      ],
      "/api/": [
        {
          text: "API 文档",
          items: [
            { text: "索引", link: "/api/README" },
            { text: "Repositories", link: "/api/Repositories" },
            { text: "Services", link: "/api/Services" },
            { text: "Models", link: "/api/Models" },
            { text: "Enums", link: "/api/Enums" },
          ],
        },
      ],
    },

    search: {
      provider: "local",
      options: {
        translations: {
          button: {
            buttonText: "搜索",
            buttonAriaLabel: "搜索",
          },
          modal: {
            displayDetails: "显示详细列表",
            resetButtonTitle: "清空搜索",
            backButtonTitle: "关闭搜索",
            noResultsText: "未找到结果",
            footer: {
              selectText: "选择",
              selectKeyAriaLabel: "回车",
              navigateText: "切换",
              navigateUpKeyAriaLabel: "上箭头",
              navigateDownKeyAriaLabel: "下箭头",
              closeText: "关闭",
              closeKeyAriaLabel: "Esc",
            },
          },
        },
      },
    },

    socialLinks: [{ icon: "github", link: "https://github.com/SaltedDoubao/DesktopMemo" }],

    editLink: {
      pattern: "https://github.com/SaltedDoubao/DesktopMemo/edit/docs/:path",
      text: "在 GitHub 上编辑此页",
    },

    footer: {
      message: "Released under the MIT License.",
      copyright: "Copyright © SaltedDoubao",
    },

    locales: {
      en: {
        nav: [
          { text: "Home", link: "/en/" },
          { text: "User Guide", link: "/en/Guide/README" },
          { text: "Dev Guide", link: "/en/Dev/README" },
          { text: "Architecture", link: "/en/project_structure/README" },
          { text: "API", link: "/en/api/README" },
          { text: "中文", link: "/" },
          { text: "GitHub", link: "https://github.com/SaltedDoubao/DesktopMemo" },
          { text: "Releases", link: "https://github.com/SaltedDoubao/DesktopMemo/releases" },
        ],
        sidebar: {
          "/en/Guide/": [
            {
              text: "User Guide",
              items: [
                { text: "Overview", link: "/en/Guide/README" },
                { text: "Install & Update", link: "/en/Guide/01_安装与更新" },
                { text: "Basics", link: "/en/Guide/02_基础使用" },
                { text: "Shortcuts", link: "/en/Guide/03_快捷键" },
                { text: "Data & Backup", link: "/en/Guide/04_数据与备份" },
                { text: "FAQ", link: "/en/Guide/05_常见问题" },
                { text: "Privacy", link: "/en/Guide/06_隐私与安全" },
              ],
            },
          ],
          "/en/Dev/": [
            {
              text: "Dev Guide",
              items: [
                { text: "Overview", link: "/en/Dev/README" },
                { text: "Environment", link: "/en/Dev/01_开发环境" },
                { text: "Build & Release", link: "/en/Dev/02_构建与发布" },
                { text: "Debug & Logs", link: "/en/Dev/03_调试与日志" },
                { text: "Migrations", link: "/en/Dev/04_数据迁移" },
              ],
            },
            {
              text: "Contribution",
              items: [{ text: "CONTRIBUTING", link: "/en/CONTRIBUTING" }],
            },
          ],
          "/en/project_structure/": [
            {
              text: "Architecture",
              items: [
                { text: "Index", link: "/en/project_structure/README" },
                { text: "01 Diagram", link: "/en/project_structure/01_架构图" },
                { text: "02 Modules", link: "/en/project_structure/02_模块划分" },
                { text: "03 Tech Stack", link: "/en/project_structure/03_技术栈和依赖" },
                { text: "04 Data Flow", link: "/en/project_structure/04_数据流和通信" },
              ],
            },
          ],
          "/en/api/": [
            {
              text: "API",
              items: [
                { text: "Index", link: "/en/api/README" },
                { text: "Repositories", link: "/en/api/Repositories" },
                { text: "Services", link: "/en/api/Services" },
                { text: "Models", link: "/en/api/Models" },
                { text: "Enums", link: "/en/api/Enums" },
              ],
            },
          ],
        },
        search: {
          provider: "local",
          options: {
            translations: {
              button: { buttonText: "Search", buttonAriaLabel: "Search" },
              modal: {
                displayDetails: "Show details",
                resetButtonTitle: "Clear search",
                backButtonTitle: "Close search",
                noResultsText: "No results",
                footer: {
                  selectText: "Select",
                  selectKeyAriaLabel: "Enter",
                  navigateText: "Navigate",
                  navigateUpKeyAriaLabel: "ArrowUp",
                  navigateDownKeyAriaLabel: "ArrowDown",
                  closeText: "Close",
                  closeKeyAriaLabel: "Esc",
                },
              },
            },
          },
        },
      },
    },
  },
});

function escapeHtml(raw: string): string {
  return raw
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;")
    .replaceAll("'", "&#39;");
}
