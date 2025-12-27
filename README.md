# DesktopMemo 文档站点

本仓库用于承载 DesktopMemo 的项目文档（面向用户与开发者），内容以 Markdown 编写，并通过 VitePress 构建为可部署到 GitHub Pages 的静态站点。

<div class="dm-actions">
  <a class="dm-btn dm-btn--primary" href="https://github.com/SaltedDoubao/DesktopMemo" target="_blank" rel="noopener">GitHub 仓库</a>
  <a class="dm-btn" href="https://github.com/SaltedDoubao/DesktopMemo/releases" target="_blank" rel="noopener">Releases</a>
  <a class="dm-btn" href="https://github.com/SaltedDoubao/DesktopMemo/issues" target="_blank" rel="noopener">反馈 / Issues</a>
</div>

## 你应该从哪里开始？

- **我是普通用户**：从 [用户手册](Guide/README.md) 开始
- **我要参与开发/二次开发**：从 [开发者指南](Dev/README.md) 开始
- **我要快速理解代码结构**：看 [项目架构文档](project_structure/README.md)
- **我要查看接口与模型**：看 [API 文档](api/README.md)
- **我想了解规划中的云同步**：看 [MySQL 集成规范](MySQL-集成规范.md)

## 相关仓库

- 主项目仓库（代码与 Releases）：https://github.com/SaltedDoubao/DesktopMemo

## 快速预览（本地网页）

### 本地开发（VitePress）

```powershell
npm install
npm run dev
```

### 生成静态文件（用于 GitHub Pages）

```powershell
npm run build
```

构建产物输出到仓库根目录的 `docs/`（可在 GitHub Pages 中选择 “/docs” 目录作为部署源）。

本地预览构建产物：

```powershell
npm run preview
```

## 反馈与贡献

- 发现文档问题：欢迎直接改文档并提交 PR，或在 Issues 中反馈
- 参与开发：请阅读 [贡献指南](CONTRIBUTING.md)

---

**最后更新**：2025-12-26
