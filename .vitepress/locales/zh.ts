import { DefaultTheme, LocaleSpecificConfig } from "vitepress";

export const zhConfig: LocaleSpecificConfig<DefaultTheme.Config> = {
    title: "DesktopMemo 文档",
    description: "DesktopMemo（桌面便签）项目文档：用户手册、开发者指南、架构与 API。",

    themeConfig: {
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

        outline: {
            label: "页面导航",
            level: [2, 3],
        },

        docFooter: {
            prev: "上一页",
            next: "下一页",
        },

        lastUpdated: {
            text: "最后更新于",
            formatOptions: {
                dateStyle: "short",
                timeStyle: "short",
            },
        },

        returnToTopLabel: "回到顶部",
        sidebarMenuLabel: "菜单",
        darkModeSwitchLabel: "主题",
        lightModeSwitchTitle: "切换到浅色模式",
        darkModeSwitchTitle: "切换到深色模式",

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

        editLink: {
            pattern: "https://github.com/SaltedDoubao/DesktopMemo-docs/edit/main/:path",
            text: "在 GitHub 上编辑此页",
        },
    },
};
