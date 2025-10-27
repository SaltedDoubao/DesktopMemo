# MySQL 云存储集成规范

> **状态**: 计划功能（尚未实现）  
> **版本**: 1.0  
> **最后更新**: 2025-01-27

## 1. 概述

本文档定义了将 MySQL 云存储功能集成到 DesktopMemo 的技术规范和最佳实践。MySQL 集成将作为可选的云同步功能，与现有的本地 SQLite 存储并行工作。

### 1.1 设计原则

- **本地优先**: 本地 SQLite 仍然是主要数据源
- **可选同步**: MySQL 云同步是可选功能
- **冲突解决**: 明确的冲突解决策略
- **隐私保护**: 用户数据加密和隐私保护
- **性能优化**: 对本地操作的影响最小化

### 1.2 架构概览

```
┌─────────────────────────────────────────────┐
│           应用层                            │
│  (ViewModels, Commands, UI Logic)           │
└─────────────────┬───────────────────────────┘
                  │
┌─────────────────▼───────────────────────────┐
│         仓储层                              │
│  ┌──────────────┐      ┌─────────────────┐ │
│  │   本地       │      │   云端同步      │ │
│  │   SQLite     │◄────►│   管理器        │ │
│  └──────────────┘      └────────┬────────┘ │
└─────────────────────────────────┼──────────┘
                                  │
                    ┌─────────────▼──────────────┐
                    │   MySQL 云存储              │
                    │   (远程数据库)              │
                    └────────────────────────────┘
```

## 2. 数据模型设计

### 2.1 数据库架构

#### 备忘录表（Memos Table）
```sql
CREATE TABLE memos (
    id VARCHAR(36) PRIMARY KEY,
    user_id VARCHAR(36) NOT NULL,
    title VARCHAR(255) NOT NULL,
    content LONGTEXT,
    created_at DATETIME(3) NOT NULL,
    updated_at DATETIME(3) NOT NULL,
    is_pinned TINYINT(1) DEFAULT 0,
    is_deleted TINYINT(1) DEFAULT 0,
    deleted_at DATETIME(3) NULL,
    sync_status ENUM('synced', 'modified', 'conflict') DEFAULT 'synced',
    local_version INT DEFAULT 1,
    server_version INT DEFAULT 1,
    INDEX idx_user_updated (user_id, updated_at),
    INDEX idx_user_deleted (user_id, is_deleted)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
```

#### 待办事项表（Todos Table）
```sql
CREATE TABLE todos (
    id VARCHAR(36) PRIMARY KEY,
    user_id VARCHAR(36) NOT NULL,
    content VARCHAR(1000) NOT NULL,
    is_completed TINYINT(1) DEFAULT 0,
    created_at DATETIME(3) NOT NULL,
    updated_at DATETIME(3) NOT NULL,
    completed_at DATETIME(3) NULL,
    is_deleted TINYINT(1) DEFAULT 0,
    deleted_at DATETIME(3) NULL,
    sync_status ENUM('synced', 'modified', 'conflict') DEFAULT 'synced',
    local_version INT DEFAULT 1,
    server_version INT DEFAULT 1,
    INDEX idx_user_updated (user_id, updated_at),
    INDEX idx_user_completed (user_id, is_completed)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
```

#### 同步日志表（Sync Log Table）
```sql
CREATE TABLE sync_logs (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,
    user_id VARCHAR(36) NOT NULL,
    entity_type ENUM('memo', 'todo') NOT NULL,
    entity_id VARCHAR(36) NOT NULL,
    action ENUM('create', 'update', 'delete') NOT NULL,
    sync_direction ENUM('upload', 'download') NOT NULL,
    status ENUM('success', 'failed', 'conflict') NOT NULL,
    error_message TEXT NULL,
    synced_at DATETIME(3) NOT NULL,
    INDEX idx_user_synced (user_id, synced_at),
    INDEX idx_entity (entity_type, entity_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
```

### 2.2 本地 SQLite 架构扩展

为现有表添加同步相关字段：

```sql
-- 添加到 memos 表
ALTER TABLE memos ADD COLUMN sync_status TEXT DEFAULT 'local_only';
ALTER TABLE memos ADD COLUMN local_version INTEGER DEFAULT 1;
ALTER TABLE memos ADD COLUMN server_version INTEGER DEFAULT 0;
ALTER TABLE memos ADD COLUMN last_synced_at TEXT NULL;

-- 添加到 todos 表
ALTER TABLE todos ADD COLUMN sync_status TEXT DEFAULT 'local_only';
ALTER TABLE todos ADD COLUMN local_version INTEGER DEFAULT 1;
ALTER TABLE todos ADD COLUMN server_version INTEGER DEFAULT 0;
ALTER TABLE todos ADD COLUMN last_synced_at TEXT NULL;
```

## 3. 同步策略

### 3.1 同步模式

1. **手动同步**: 用户通过 UI 按钮触发同步
2. **自动同步**: 定期后台同步（可配置间隔）
3. **启动同步**: 应用启动时同步

### 3.2 冲突解决

**最后写入优先（LWW）策略**:
- 比较 `updated_at` 时间戳
- 保留时间戳较新的版本
- 将冲突版本存储在本地历史中

**用户提示解决**:
- 对于重大冲突，提示用户选择：
  - 保留本地版本
  - 使用服务器版本
  - 手动合并

### 3.3 同步流程

```
1. 检测本地更改
   ├─ 查询 sync_status != 'synced' 的项目
   └─ 收集已更改的项目

2. 拉取远程更改
   ├─ 请求自 last_synced_at 以来的更新
   ├─ 检测冲突 (local_version != server_version)
   └─ 应用更改到本地数据库

3. 解决冲突
   ├─ 应用解决策略
   └─ 标记冲突项目

4. 推送本地更改
   ├─ 上传新增/修改的项目
   ├─ 更新 server_version
   └─ 标记为 'synced'

5. 更新同步状态
   ├─ 更新 last_synced_at
   └─ 记录同步结果
```

## 4. API 设计

### 4.1 RESTful API 端点

#### 身份认证
```
POST   /api/auth/login
POST   /api/auth/register
POST   /api/auth/refresh
POST   /api/auth/logout
```

#### 备忘录
```
GET    /api/memos?since={timestamp}&limit={n}
GET    /api/memos/{id}
POST   /api/memos
PUT    /api/memos/{id}
DELETE /api/memos/{id}
POST   /api/memos/batch-sync
```

#### 待办事项
```
GET    /api/todos?since={timestamp}&limit={n}
GET    /api/todos/{id}
POST   /api/todos
PUT    /api/todos/{id}
DELETE /api/todos/{id}
POST   /api/todos/batch-sync
```

#### 同步
```
GET    /api/sync/status
POST   /api/sync/pull
POST   /api/sync/push
POST   /api/sync/full
```

### 4.2 请求/响应格式

**批量同步请求**:
```json
{
  "user_id": "uuid",
  "last_synced_at": "2025-01-27T12:00:00.000Z",
  "changes": [
    {
      "id": "memo-uuid",
      "action": "update",
      "data": { /* memo 对象 */ },
      "local_version": 5
    }
  ]
}
```

**批量同步响应**:
```json
{
  "success": true,
  "server_changes": [
    {
      "id": "memo-uuid",
      "action": "update",
      "data": { /* memo 对象 */ },
      "server_version": 6,
      "is_conflict": false
    }
  ],
  "conflicts": [
    {
      "id": "memo-uuid",
      "local_data": { /* 本地版本 */ },
      "server_data": { /* 服务器版本 */ },
      "resolution_required": true
    }
  ],
  "sync_timestamp": "2025-01-27T12:05:00.000Z"
}
```

## 5. 实现指南

### 5.1 仓储层

创建新的接口和实现：

```csharp
// 接口
public interface ICloudSyncService
{
    Task<SyncResult> PullChangesAsync(DateTime? since = null);
    Task<SyncResult> PushChangesAsync();
    Task<SyncResult> FullSyncAsync();
    Task<bool> IsConnectedAsync();
    Task<SyncStatus> GetSyncStatusAsync();
}

// 配置
public class CloudSyncSettings
{
    public bool IsEnabled { get; set; }
    public string ServerUrl { get; set; }
    public string UserId { get; set; }
    public TimeSpan AutoSyncInterval { get; set; }
    public bool SyncOnStartup { get; set; }
    public ConflictResolutionStrategy ConflictStrategy { get; set; }
}
```

### 5.2 数据传输对象

```csharp
public class SyncResult
{
    public bool Success { get; set; }
    public int ItemsSynced { get; set; }
    public int Conflicts { get; set; }
    public List<ConflictItem> ConflictItems { get; set; }
    public string ErrorMessage { get; set; }
    public DateTime SyncTimestamp { get; set; }
}

public class ConflictItem
{
    public string Id { get; set; }
    public string Type { get; set; } // "memo" 或 "todo"
    public object LocalVersion { get; set; }
    public object ServerVersion { get; set; }
    public DateTime LocalUpdatedAt { get; set; }
    public DateTime ServerUpdatedAt { get; set; }
}
```

### 5.3 安全考虑

1. **身份认证**:
   - 使用 JWT 令牌进行 API 认证
   - 实现令牌刷新机制
   - 在本地设置中安全存储令牌

2. **数据加密**:
   - 所有网络通信使用 HTTPS/TLS
   - 敏感内容可选端到端加密
   - 加密存储的凭据

3. **隐私保护**:
   - 用户同意云同步
   - 明确的数据保留策略
   - 选项删除所有云数据

### 5.4 错误处理

```csharp
public enum SyncErrorType
{
    NetworkError,
    AuthenticationError,
    ServerError,
    ConflictError,
    LocalDatabaseError,
    UnknownError
}

public class SyncException : Exception
{
    public SyncErrorType ErrorType { get; }
    public string UserMessage { get; }
    public bool IsRetryable { get; }
}
```

## 6. UI/UX 指南

### 6.1 设置面板

添加新的"云同步"部分：
- 启用/禁用云同步开关
- 服务器连接状态指示器
- 最后同步时间戳显示
- 手动同步按钮
- 冲突解决偏好设置

### 6.2 同步状态指示器

- 系统托盘图标显示同步状态：
  - ✅ 已同步
  - 🔄 同步中
  - ⚠️ 冲突
  - ❌ 错误

### 6.3 冲突解决 UI

当发生冲突时：
1. 显示通知
2. 打开冲突解决对话框
3. 并排显示两个版本
4. 允许用户选择或合并

## 7. 测试要求

### 7.1 单元测试

- 仓储层方法
- 冲突解决逻辑
- 数据序列化/反序列化
- 同步状态管理

### 7.2 集成测试

- 端到端同步流程
- 网络故障场景
- 并发修改处理
- 大数据集同步性能

### 7.3 测试场景

1. 首次同步现有本地数据
2. 多设备同步同一账户
3. 离线更改在线后同步
4. 同步期间网络中断
5. 服务器停机恢复

## 8. 性能优化

### 8.1 优化策略

- **增量同步**: 只同步更改的项目
- **批量操作**: 对多个项目使用批量 API
- **连接池**: 重用 HTTP 连接
- **压缩**: 传输前压缩大内容
- **后台同步**: 同步期间不阻塞 UI

### 8.2 监控指标

- 同步持续时间
- 网络带宽使用
- 数据库查询性能
- 冲突率
- 同步成功率

## 9. 迁移路径

### 阶段 1: 准备（当前）
- ✅ 实现本地 SQLite 存储
- ✅ 为数据模型添加同步状态字段
- 📝 定义云同步规范

### 阶段 2: 后端开发
- 开发 MySQL 架构
- 实现 REST API
- 建立认证系统
- 部署后端基础设施

### 阶段 3: 客户端集成
- 实现 `ICloudSyncService`
- 添加同步 UI 组件
- 实现冲突解决
- 添加全面的错误处理

### 阶段 4: 测试与发布
- 内部测试
- 志愿者 Beta 测试
- 逐步向用户推出
- 监控和迭代

## 10. 未来增强

- **选择性同步**: 选择要同步的项目
- **共享备忘录**: 与其他用户协作
- **版本历史**: 查看和恢复以前的版本
- **附件同步**: 同步图片和文件
- **实时同步**: 基于 WebSocket 的即时同步

## 11. 参考资料

- [MySQL 8.0 文档](https://dev.mysql.com/doc/)
- [ASP.NET Core Web API](https://docs.microsoft.com/zh-cn/aspnet/core/web-api/)
- [JWT 认证](https://jwt.io/)
- [RESTful API 设计最佳实践](https://restfulapi.net/)

---

**文档版本**: 1.0  
**最后更新**: 2025-01-27  
**下次审阅**: 实现开始前

