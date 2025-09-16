using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DesktopMemo.Core.Interfaces;

namespace DesktopMemo.Core.Services
{
    public class LocalizationService : ILocalizationService
    {
        private readonly ISettingsService _settingsService;
        private readonly Dictionary<string, Dictionary<string, string>> _resources;
        private string _currentLanguage;
        private readonly List<LanguageInfo> _availableLanguages;

        public event EventHandler<LanguageChangedEventArgs>? LanguageChanged;

        public LocalizationService(ISettingsService settingsService)
        {
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _resources = new Dictionary<string, Dictionary<string, string>>();
            _availableLanguages = new List<LanguageInfo>();

            InitializeLanguages();
            _currentLanguage = _settingsService.GetSetting("Language", "zh-CN");
        }

        private void InitializeLanguages()
        {
            // Chinese (Simplified)
            _availableLanguages.Add(new LanguageInfo
            {
                Code = "zh-CN",
                Name = "Chinese (Simplified)",
                NativeName = "简体中文"
            });

            _resources["zh-CN"] = new Dictionary<string, string>
            {
                // Application
                ["app.name"] = "桌面便签",
                ["app.version"] = "版本：{0}",

                // Window
                ["window.title"] = "备忘录",
                ["window.close"] = "关闭",
                ["window.minimize"] = "最小化",
                ["window.settings"] = "设置",

                // Memo operations
                ["memo.new"] = "新建备忘录",
                ["memo.edit"] = "编辑备忘录",
                ["memo.delete"] = "删除备忘录",
                ["memo.save"] = "保存",
                ["memo.pin"] = "置顶",
                ["memo.unpin"] = "取消置顶",
                ["memo.welcome.title"] = "欢迎使用DesktopMemo",
                ["memo.welcome.content"] = "这是您的第一条备忘录！\n\n点击此处开始编辑...",

                // Tray menu
                ["tray.tooltip"] = "DesktopMemo 便签 - 桌面便签工具",
                ["tray.show_hide"] = "显示/隐藏",
                ["tray.new_memo"] = "新建便签",
                ["tray.exit"] = "退出",
                ["tray.topmost_mode"] = "置顶模式",
                ["tray.normal_mode"] = "普通模式",
                ["tray.desktop_mode"] = "桌面置顶",
                ["tray.always_mode"] = "总是置顶",
                ["tray.click_through"] = "穿透模式",

                // Status messages
                ["status.ready"] = "就绪",
                ["status.saved"] = "已保存",
                ["status.deleted"] = "已删除",
                ["status.mode_normal"] = "已切换到普通模式",
                ["status.mode_desktop"] = "已切换到桌面置顶模式",
                ["status.mode_always"] = "已切换到总是置顶模式",
                ["status.click_through_on"] = "穿透模式已启用",
                ["status.click_through_off"] = "穿透模式已禁用",
                ["status.memo_created"] = "已创建新备忘录",
                ["status.memo_loaded"] = "备忘录已加载",

                // Positions
                ["position.top_left"] = "左上角",
                ["position.top_center"] = "顶部中央",
                ["position.top_right"] = "右上角",
                ["position.middle_left"] = "左侧中央",
                ["position.center"] = "屏幕中央",
                ["position.middle_right"] = "右侧中央",
                ["position.bottom_left"] = "左下角",
                ["position.bottom_center"] = "底部中央",
                ["position.bottom_right"] = "右下角",

                // Dialogs
                ["dialog.delete_confirm"] = "确定要删除备忘录\"{0}\"吗？",
                ["dialog.clear_content"] = "是否清空当前便签内容？",
                ["dialog.exit_confirm"] = "确定要退出应用程序吗？",
                ["dialog.yes"] = "是",
                ["dialog.no"] = "否",
                ["dialog.ok"] = "确定",
                ["dialog.cancel"] = "取消",

                // Settings
                ["settings.title"] = "设置",
                ["settings.general"] = "常规",
                ["settings.appearance"] = "外观",
                ["settings.theme"] = "主题",
                ["settings.language"] = "语言",
                ["settings.font_size"] = "字体大小",
                ["settings.opacity"] = "透明度",
                ["settings.auto_save"] = "自动保存",
                ["settings.show_exit_prompt"] = "显示退出提示",
                ["settings.show_delete_prompt"] = "显示删除提示"
            };

            // English
            _availableLanguages.Add(new LanguageInfo
            {
                Code = "en-US",
                Name = "English",
                NativeName = "English"
            });

            _resources["en-US"] = new Dictionary<string, string>
            {
                // Application
                ["app.name"] = "Desktop Memo",
                ["app.version"] = "Version: {0}",

                // Window
                ["window.title"] = "Memo",
                ["window.close"] = "Close",
                ["window.minimize"] = "Minimize",
                ["window.settings"] = "Settings",

                // Memo operations
                ["memo.new"] = "New Memo",
                ["memo.edit"] = "Edit Memo",
                ["memo.delete"] = "Delete Memo",
                ["memo.save"] = "Save",
                ["memo.pin"] = "Pin",
                ["memo.unpin"] = "Unpin",
                ["memo.welcome.title"] = "Welcome to DesktopMemo",
                ["memo.welcome.content"] = "This is your first memo!\n\nClick here to start editing...",

                // Tray menu
                ["tray.tooltip"] = "DesktopMemo - Desktop Sticky Notes",
                ["tray.show_hide"] = "Show/Hide",
                ["tray.new_memo"] = "New Note",
                ["tray.exit"] = "Exit",
                ["tray.topmost_mode"] = "Topmost Mode",
                ["tray.normal_mode"] = "Normal Mode",
                ["tray.desktop_mode"] = "Desktop Topmost",
                ["tray.always_mode"] = "Always Topmost",
                ["tray.click_through"] = "Click Through",

                // Status messages
                ["status.ready"] = "Ready",
                ["status.saved"] = "Saved",
                ["status.deleted"] = "Deleted",
                ["status.mode_normal"] = "Switched to normal mode",
                ["status.mode_desktop"] = "Switched to desktop topmost mode",
                ["status.mode_always"] = "Switched to always topmost mode",
                ["status.click_through_on"] = "Click through enabled",
                ["status.click_through_off"] = "Click through disabled",
                ["status.memo_created"] = "New memo created",
                ["status.memo_loaded"] = "Memos loaded",

                // Positions
                ["position.top_left"] = "Top Left",
                ["position.top_center"] = "Top Center",
                ["position.top_right"] = "Top Right",
                ["position.middle_left"] = "Middle Left",
                ["position.center"] = "Center",
                ["position.middle_right"] = "Middle Right",
                ["position.bottom_left"] = "Bottom Left",
                ["position.bottom_center"] = "Bottom Center",
                ["position.bottom_right"] = "Bottom Right",

                // Dialogs
                ["dialog.delete_confirm"] = "Are you sure you want to delete memo \"{0}\"?",
                ["dialog.clear_content"] = "Clear current note content?",
                ["dialog.exit_confirm"] = "Are you sure you want to exit?",
                ["dialog.yes"] = "Yes",
                ["dialog.no"] = "No",
                ["dialog.ok"] = "OK",
                ["dialog.cancel"] = "Cancel",

                // Settings
                ["settings.title"] = "Settings",
                ["settings.general"] = "General",
                ["settings.appearance"] = "Appearance",
                ["settings.theme"] = "Theme",
                ["settings.language"] = "Language",
                ["settings.font_size"] = "Font Size",
                ["settings.opacity"] = "Opacity",
                ["settings.auto_save"] = "Auto Save",
                ["settings.show_exit_prompt"] = "Show Exit Prompt",
                ["settings.show_delete_prompt"] = "Show Delete Prompt"
            };
        }

        public string GetString(string key)
        {
            if (_resources.TryGetValue(_currentLanguage, out var languageResources))
            {
                if (languageResources.TryGetValue(key, out var value))
                {
                    return value;
                }
            }

            // Fallback to English
            if (_resources.TryGetValue("en-US", out var fallbackResources))
            {
                if (fallbackResources.TryGetValue(key, out var value))
                {
                    return value;
                }
            }

            // Return key if not found
            return key;
        }

        public string GetString(string key, params object[] args)
        {
            var format = GetString(key);
            try
            {
                return string.Format(format, args);
            }
            catch
            {
                return format;
            }
        }

        public void SetLanguage(string languageCode)
        {
            if (!_resources.ContainsKey(languageCode))
            {
                throw new ArgumentException($"Language '{languageCode}' is not supported");
            }

            _currentLanguage = languageCode;
            _settingsService.SetSetting("Language", languageCode);

            var culture = new CultureInfo(languageCode);
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;

            LanguageChanged?.Invoke(this, new LanguageChangedEventArgs(languageCode, culture));
        }

        public string GetCurrentLanguage()
        {
            return _currentLanguage;
        }

        public IEnumerable<LanguageInfo> GetAvailableLanguages()
        {
            return _availableLanguages.AsEnumerable();
        }
    }
}