import DefaultTheme from "vitepress/theme";
import type { Theme } from "vitepress";
import { nextTick } from "vue";
import "./custom.css";

// ========== Mermaid 模态框状态 ==========
let modalOverlay: HTMLDivElement | null = null;
let currentZoom = 1;

function createModal(): HTMLDivElement {
  if (modalOverlay) return modalOverlay;

  modalOverlay = document.createElement("div");
  modalOverlay.className = "mermaid-modal-overlay";
  modalOverlay.innerHTML = `
    <button class="mermaid-modal-close" aria-label="关闭">✕</button>
    <div class="mermaid-modal-content"></div>
    <div class="mermaid-zoom-controls">
      <button class="mermaid-zoom-btn" data-action="zoom-out" aria-label="缩小">−</button>
      <span class="mermaid-zoom-level">100%</span>
      <button class="mermaid-zoom-btn" data-action="zoom-in" aria-label="放大">+</button>
      <button class="mermaid-zoom-btn" data-action="zoom-reset" aria-label="重置">↺</button>
    </div>
  `;

  document.body.appendChild(modalOverlay);

  // 关闭按钮
  const closeBtn = modalOverlay.querySelector(".mermaid-modal-close");
  closeBtn?.addEventListener("click", closeModal);

  // 点击背景关闭
  modalOverlay.addEventListener("click", (e) => {
    if (e.target === modalOverlay) closeModal();
  });

  // 缩放控制
  const zoomControls = modalOverlay.querySelector(".mermaid-zoom-controls");
  zoomControls?.addEventListener("click", (e) => {
    e.stopPropagation();
    const target = e.target as HTMLElement;
    const action = target.dataset.action || target.closest("[data-action]")?.getAttribute("data-action");
    if (action === "zoom-in") updateZoom(0.25);
    if (action === "zoom-out") updateZoom(-0.25);
    if (action === "zoom-reset") resetZoom();
  });

  // ESC 键关闭
  document.addEventListener("keydown", (e) => {
    if (e.key === "Escape" && modalOverlay?.classList.contains("active")) {
      closeModal();
    }
  });

  // 滚轮缩放（Ctrl + 滚轮 或 直接滚轮）
  const content = modalOverlay.querySelector(".mermaid-modal-content");
  content?.addEventListener("wheel", (e) => {
    e.preventDefault();
    const wheelEvent = e as WheelEvent;
    const delta = wheelEvent.deltaY > 0 ? -0.1 : 0.1;
    updateZoom(delta);
  }, { passive: false });

  return modalOverlay;
}

function openModal(svgContent: string): void {
  const modal = createModal();
  const content = modal.querySelector(".mermaid-modal-content") as HTMLDivElement;
  if (content) {
    content.innerHTML = svgContent;
    // 保存原始尺寸
    const svg = content.querySelector("svg");
    if (svg) {
      const rect = svg.getBoundingClientRect();
      svg.dataset.originalWidth = String(rect.width);
      svg.dataset.originalHeight = String(rect.height);
    }
    resetZoom();
    modal.classList.add("active");
    document.body.style.overflow = "hidden";
  }
}

function closeModal(): void {
  modalOverlay?.classList.remove("active");
  document.body.style.overflow = "";
}

function updateZoom(delta: number): void {
  currentZoom = Math.max(0.25, Math.min(5, currentZoom + delta));
  applyZoom();
}

function resetZoom(): void {
  currentZoom = 1;
  applyZoom();
}

function applyZoom(): void {
  if (!modalOverlay) return;
  const content = modalOverlay.querySelector(".mermaid-modal-content") as HTMLDivElement;
  const zoomLevel = modalOverlay.querySelector(".mermaid-zoom-level");
  if (content) {
    const svg = content.querySelector("svg");
    if (svg) {
      const originalWidth = parseFloat(svg.dataset.originalWidth || "0");
      const originalHeight = parseFloat(svg.dataset.originalHeight || "0");
      if (originalWidth && originalHeight) {
        svg.style.width = `${originalWidth * currentZoom}px`;
        svg.style.height = `${originalHeight * currentZoom}px`;
        svg.style.minWidth = `${originalWidth * currentZoom}px`;
        svg.style.minHeight = `${originalHeight * currentZoom}px`;
      }
    }
  }
  if (zoomLevel) {
    zoomLevel.textContent = `${Math.round(currentZoom * 100)}%`;
  }
}

// ========== Mermaid 渲染 ==========

async function renderMermaid(): Promise<void> {
  if (typeof window === "undefined") return;

  const nodes = Array.from(document.querySelectorAll<HTMLElement>(".mermaid"));
  if (nodes.length === 0) return;

  // 从 data-src 属性读取 Base64 编码的源码并解码
  for (const el of nodes) {
    if (!el.dataset.mermaidSrc) {
      // 优先从 data-src 属性读取 Base64 编码的代码
      const base64Src = el.getAttribute("data-src");
      if (base64Src) {
        try {
          // 解码 Base64（支持 UTF-8）
          el.dataset.mermaidSrc = decodeURIComponent(escape(atob(base64Src)));
        } catch {
          el.dataset.mermaidSrc = el.textContent || "";
        }
      } else {
        el.dataset.mermaidSrc = el.textContent || "";
      }
    }
    // Mermaid 会用 data-processed 跳过已处理节点；为了支持主题切换/路由切换后的重渲染，需要清理该标记
    el.removeAttribute("data-processed");
    el.textContent = el.dataset.mermaidSrc;
  }

  const mermaidModule = await import("mermaid");
  const mermaid = mermaidModule.default;

  const isDark = document.documentElement.classList.contains("dark");
  mermaid.initialize({
    startOnLoad: false,
    theme: isDark ? "dark" : "default",
    securityLevel: "strict",
  });

  await nextTick();

  try {
    const validNodes: HTMLElement[] = [];
    for (const el of nodes) {
      const src = (el.dataset.mermaidSrc || "").trim();
      if (!src) continue;
      try {
        await mermaid.parse(src);
        validNodes.push(el);
      } catch {
        // 保留原始文本作为回退展示，避免渲染出 Mermaid 的"Syntax error in text"错误占位图
        el.textContent = el.dataset.mermaidSrc || "";
      }
    }

    if (validNodes.length > 0) {
      await mermaid.run({ nodes: validNodes });
      // 添加点击事件
      addClickHandlers(validNodes);
    }
  } catch {
    // 忽略渲染失败，避免影响页面主体内容
  }
}

function addClickHandlers(nodes: HTMLElement[]): void {
  for (const el of nodes) {
    // 避免重复绑定
    if (el.dataset.clickBound) continue;
    el.dataset.clickBound = "true";

    el.addEventListener("click", () => {
      const svg = el.querySelector("svg");
      if (svg) {
        openModal(svg.outerHTML);
      }
    });
  }
}

function observeThemeChange(): void {
  if (typeof window === "undefined") return;

  const el = document.documentElement;
  const observer = new MutationObserver(() => {
    void renderMermaid();
  });
  observer.observe(el, { attributes: true, attributeFilter: ["class"] });
}

export default {
  extends: DefaultTheme,
  enhanceApp({ router }) {
    const existing = (router as any).onAfterRouteChanged;
    (router as any).onAfterRouteChanged = async () => {
      if (typeof existing === "function") await existing();
      // 等待 DOM 更新
      setTimeout(() => {
        void renderMermaid();
      }, 0);
    };

    if (typeof window !== "undefined") {
      observeThemeChange();
      setTimeout(() => void renderMermaid(), 0);
    }
  },
} satisfies Theme;
