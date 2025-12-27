# DesktopMemo Project Architecture Documentation Index

## Documentation Overview

This directory contains complete architecture documentation for the DesktopMemo project, helping developers quickly understand system design, technology choices, and data flow mechanisms.

---

## 📚 Document List

### [01_diagram.md](./01_diagram.md)
**Core Content**:
- Overall system architecture (three-layer architecture diagram)
- Project structure description
- Dependency injection configuration
- Data storage architecture
- Key design patterns
- Core module overview
- External dependency descriptions
- Problem-prone module analysis

**Use Cases**:
- Quick project understanding for new team members
- Architecture reviews and discussions
- Technical debt analysis

**Key Diagrams**:
- Three-layer architecture overview
- Data storage architecture diagram
- Dependency relationship diagram

---

### [02_modules.md](./02_modules.md)
**Core Content**:
- Presentation layer module details (Views, ViewModels, Localization)
- Domain layer module details (Models, Contracts, Helpers)
- Infrastructure layer module details (Repositories, Services)
- Inter-module communication mechanisms
- Module dependency relationships
- Module responsibility matrix
- Module extension guide
- Module maintenance recommendations

**Use Cases**:
- Module positioning before adding new features
- Code review and refactoring
- Module responsibility discussions

**Key Diagrams**:
- Module overview diagram
- Module communication sequence diagrams
- Module dependency diagram

---

### [03_tech-stack.md](./03_tech-stack.md)
**Core Content**:
- .NET 9.0 + WPF technology stack
- MVVM architecture details
- Third-party library dependencies (CommunityToolkit.Mvvm, Dapper, Markdig, etc.)
- Core dependency details
- Data storage technologies (SQLite, Markdown, JSON)
- Windows API calls
- Development tools and environment
- Technology selection comparison

**Use Cases**:
- Dependency upgrade decisions
- Technology selection evaluation
- Environment setup guide

**Key Information**:
- NuGet package version list
- Technology selection comparison table
- Alternative solution analysis

---

### [04_data-flow.md](./04_data-flow.md)
**Core Content**:
- Data flow overview
- Core business processes (app startup, create memo, edit, search, settings save)
- Data storage strategy (dual storage design)
- Data consistency guarantees
- Concurrency control
- Asynchronous data flow
- Event-driven mechanisms
- Key design patterns (Repository, unidirectional data flow, debounce)
- Exception handling
- Performance optimization
- Data flow security

**Use Cases**:
- Understanding business processes
- Troubleshooting data inconsistency issues
- Performance optimization
- Security audits

**Key Diagrams**:
- Data flow overview diagram
- Application startup sequence diagram
- Create/edit memo sequence diagrams
- Search flow diagram
- Async thread switching diagram

---

## 🗺️ Quick Navigation

### I'm new, where do I start?
1. **First**: [01_diagram.md](./01_diagram.md) - Understand overall architecture
2. **Next**: [02_modules.md](./02_modules.md) - Understand layer responsibilities
3. **Finally**: [04_data-flow.md](./04_data-flow.md) - Understand data flow

### I want to add a new feature
1. **Check**: [02_modules.md](./02_modules.md) Section 8 - Module Extension Guide
2. **Reference**: [04_data-flow.md](./04_data-flow.md) Section 2 - Core Data Flow Scenarios

### I want to upgrade dependencies or choose new technology
1. **Check**: [03_tech-stack.md](./03_tech-stack.md) Section 3 - Core Dependency Details
2. **Compare**: [03_tech-stack.md](./03_tech-stack.md) Section 10 - Technology Selection Comparison

### I need to troubleshoot a bug
1. **Locate module**: [02_modules.md](./02_modules.md)
2. **Trace data flow**: [04_data-flow.md](./04_data-flow.md)
3. **Check critical modules**: [01_diagram.md](./01_diagram.md) Section 10 - Problem-Prone Modules

### I want to optimize performance
1. **Check**: [04_data-flow.md](./04_data-flow.md) Section 8 - Performance Optimization
2. **Reference**: [02_modules.md](./02_modules.md) Section 9 - Performance-Sensitive Modules

---

## 📊 Architecture Overview

### Three-Layer Architecture

```
┌─────────────────────────────────────────┐
│  Presentation Layer (DesktopMemo.App)   │
│  - WPF Views                            │
│  - ViewModels (MVVM)                    │
│  - Localization                         │
├─────────────────────────────────────────┤
│  Domain Layer (DesktopMemo.Core)        │
│  - Domain Models                        │
│  - Contracts/Interfaces                 │
│  - Helpers                              │
├─────────────────────────────────────────┤
│  Infrastructure (DesktopMemo.Infrastructure) │
│  - Repositories (Data Access)           │
│  - Services (Business Services)         │
└─────────────────────────────────────────┘
         ↓              ↓
  ┌──────────┐    ┌──────────┐
  │ SQLite DB│    │ Markdown │
  │  Index   │    │  Files   │
  └──────────┘    └──────────┘
```

### Core Technology Stack

- **Development Platform**: .NET 9.0 + C# 13.0
- **UI Framework**: WPF (Windows Presentation Foundation)
- **Architecture Pattern**: MVVM (CommunityToolkit.Mvvm)
- **Data Storage**: SQLite + Markdown + JSON
- **Dependency Injection**: Microsoft.Extensions.DependencyInjection
- **ORM**: Dapper
- **Markdown**: Markdig

### Core Modules

| Module | Responsibility | Key Classes |
|--------|---------------|-------------|
| Memo Management | CRUD operations | MainViewModel, SqliteIndexedMemoRepository |
| TodoList | Task management | TodoListViewModel, SqliteTodoRepository |
| Search | Full-text search | MemoSearchService |
| Settings | Configuration management | JsonSettingsService |
| Logging | Log recording | FileLogService, LogViewModel |
| Tray | System tray | TrayService |
| Data Migration | Version upgrades | *MigrationService |

---

## 🔧 Maintenance Guide

### Documentation Update Rules

Update relevant documentation when:

1. **Adding new modules/features**
   - Update [01_diagram.md](./01_diagram.md) - Core modules table
   - Update [02_modules.md](./02_modules.md) - Module details

2. **Upgrading dependencies or changing technology**
   - Update [03_tech-stack.md](./03_tech-stack.md) - NuGet package list

3. **Modifying business processes**
   - Update [04_data-flow.md](./04_data-flow.md) - Relevant flow diagrams

4. **Architecture adjustments**
   - Update [01_diagram.md](./01_diagram.md) - Architecture diagrams
   - Update [02_modules.md](./02_modules.md) - Module dependencies

---

## 📝 Related Documentation

- [Development Guidelines](../CONTRIBUTING.md)
- [User Guide](../Guide/README.md)
- [API Documentation](../api/README.md)
- [Changelog (Releases)](https://github.com/SaltedDoubao/DesktopMemo/releases)

---

## 🤝 Contribution

If you find errors in the documentation or need supplements, please:
1. Submit an Issue describing the problem
2. Or directly submit a Pull Request to modify the documentation

---

**Last Updated**: 2025-11-15
