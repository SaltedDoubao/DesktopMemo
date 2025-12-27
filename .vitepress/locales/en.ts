import { DefaultTheme, LocaleSpecificConfig } from "vitepress";

export const enConfig: LocaleSpecificConfig<DefaultTheme.Config> = {
    title: "DesktopMemo Docs",
    description: "DesktopMemo project documentation: User Guide, Developer Guide, Architecture & API.",

    themeConfig: {
        nav: [
            { text: "User Guide", link: "/en/Guide/README" },
            { text: "Dev Guide", link: "/en/Dev/README" },
            { text: "Architecture", link: "/en/project_structure/README" },
            { text: "API", link: "/en/api/README" },
            {
                text: "More",
                items: [
                    { text: "Contributing", link: "/en/CONTRIBUTING" },
                    { text: "MySQL Integration", link: "/en/mysql-integration" },
                ],
            },
            { text: "Releases", link: "https://github.com/SaltedDoubao/DesktopMemo/releases" },
        ],

        sidebar: {
            "/en/Guide/": [
                {
                    text: "User Guide",
                    items: [
                        { text: "Overview", link: "/en/Guide/README" },
                        { text: "Installation & Updates", link: "/en/Guide/01_install" },
                        { text: "Getting Started", link: "/en/Guide/02_basics" },
                        { text: "Keyboard Shortcuts", link: "/en/Guide/03_shortcuts" },
                        { text: "Data & Backup", link: "/en/Guide/04_data-backup" },
                        { text: "FAQ", link: "/en/Guide/05_faq" },
                        { text: "Privacy & Security", link: "/en/Guide/06_privacy" },
                    ],
                },
            ],
            "/en/Dev/": [
                {
                    text: "Developer Guide",
                    items: [
                        { text: "Overview", link: "/en/Dev/README" },
                        { text: "Development Setup", link: "/en/Dev/01_setup" },
                        { text: "Build & Release", link: "/en/Dev/02_build" },
                        { text: "Debugging & Logging", link: "/en/Dev/03_debugging" },
                        { text: "Data Migration", link: "/en/Dev/04_migration" },
                    ],
                },
                {
                    text: "Contribution",
                    items: [
                        { text: "Contributing Guide", link: "/en/CONTRIBUTING" },
                    ],
                },
            ],
            "/en/project_structure/": [
                {
                    text: "Architecture",
                    items: [
                        { text: "Index", link: "/en/project_structure/README" },
                        { text: "01 Architecture Diagram", link: "/en/project_structure/01_diagram" },
                        { text: "02 Module Structure", link: "/en/project_structure/02_modules" },
                        { text: "03 Tech Stack & Dependencies", link: "/en/project_structure/03_tech-stack" },
                        { text: "04 Data Flow & Communication", link: "/en/project_structure/04_data-flow" },
                    ],
                },
            ],
            "/en/api/": [
                {
                    text: "API Documentation",
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

        outline: {
            label: "On this page",
            level: [2, 3],
        },

        docFooter: {
            prev: "Previous",
            next: "Next",
        },

        lastUpdated: {
            text: "Last updated",
            formatOptions: {
                dateStyle: "short",
                timeStyle: "short",
            },
        },

        returnToTopLabel: "Return to top",
        sidebarMenuLabel: "Menu",
        darkModeSwitchLabel: "Appearance",
        lightModeSwitchTitle: "Switch to light theme",
        darkModeSwitchTitle: "Switch to dark theme",

        search: {
            provider: "local",
            options: {
                translations: {
                    button: {
                        buttonText: "Search",
                        buttonAriaLabel: "Search",
                    },
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

        editLink: {
            pattern: "https://github.com/SaltedDoubao/DesktopMemo-docs/edit/main/:path",
            text: "Edit this page on GitHub",
        },
    },
};
