import { defineConfig } from "vitepress";
import { zhConfig } from "./locales/zh";
import { enConfig } from "./locales/en";

const base = "/DesktopMemo/";

export default defineConfig({
  base,
  lastUpdated: true,
  cleanUrls: true,

  // 将静态构建输出到仓库根目录的 docs/，方便 GitHub Pages 选择 "/docs" 目录部署
  outDir: "docs",
  srcExclude: ["docs/**", "legacy-site-*/**", "**/node_modules/**", "_*.md"],

  // 多语言配置
  locales: {
    root: {
      label: "简体中文",
      lang: "zh-CN",
      ...zhConfig,
    },
    en: {
      label: "English",
      lang: "en-US",
      link: "/en/",
      ...enConfig,
    },
  },

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

    socialLinks: [{ icon: "github", link: "https://github.com/SaltedDoubao/DesktopMemo" }],

    footer: {
      message: "Released under the MIT License.",
      copyright: "Copyright © SaltedDoubao",
    },
  },
});

