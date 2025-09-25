# 数据迁移计划

## 目标

将旧版 DesktopMemo 数据（`notes.json`、`memos.json`、`memos_metadata.json`、`content/*.md` 等）迁移至重构版使用的统一结构：

- 所有备忘录均为 `content/{id}.md`，以 UUID 为文件名，包含 YAML Front Matter。
- 列表顺序记录在 `content/index.json` 中。
- 设置数据保存在 `settings.json`。

## 迁移原则

1. **可逆性**：在迁移前备份原始目录，确保用户可以恢复旧版数据。
2. **幂等性**：若迁移中断，可在修复问题后重新运行，不会重复生成记录。
3. **一致性**：迁移完成后，索引文件与 Markdown 文件保持同步，避免孤立记录。

## 实施步骤

1. **扫描目录**：识别历史格式：
   - `notes.json`
   - `memos.json` / `memos_metadata.json`
   - 旧 `content/*.md`
   - `settings.json`
2. **解析与映射**：
   - 生成备忘录 ID（优先使用历史记录中的 `Id` / `CreatedTime`，否则生成新 UUID）。
   - 填充标题、标签、描述等字段，缺失时采用默认值。
3. **生成新文件**：
   - 按 `{id}.md` 写出带 Front Matter 的 Markdown。
   - 生成新的 `content/index.json` 顺序列表。
   - 将窗口设置转换为 `WindowSettings` 对象写入 `settings.json`。
4. **记录日志**：输出迁移报告（成功条数、跳过条数、错误详情）。
5. **备份清理**：迁移成功后，可保留旧文件于 `backups/{timestamp}` 中供回滚。

## 伪代码示例

```csharp
var source = LegacyDataScanner.Scan(oldPath);
var targetDir = NewStructure.Prepare(appDataPath);

foreach (var legacyMemo in source.Memos)
{
    var memo = MemoMapper.Map(legacyMemo);
    NewStructure.WriteMemo(targetDir, memo);
    report.Success++;
}

NewStructure.WriteIndex(targetDir, report.MemoOrder);
NewStructure.WriteSettings(targetDir, settings);
ReportWriter.Save(report, backupsPath);
```

## 风险与缓解

- **重复或损坏文件**：通过哈希校验或按 ID 重建文件名，并在报告中列出异常。
- **编码问题**：统一使用 UTF-8 保存内容。
- **大文件性能**：必要时可分批处理 Markdown 内容，避免一次性加载全部。

