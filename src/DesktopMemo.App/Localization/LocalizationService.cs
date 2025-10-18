using System.ComponentModel;
using System.Globalization;
using System.Resources;
using DesktopMemo.Core.Contracts;

namespace DesktopMemo.App.Localization;

/// <summary>
/// 本地化服务实现，支持运行时动态切换语言
/// </summary>
public class LocalizationService : ILocalizationService, INotifyPropertyChanged
{
    private readonly ResourceManager _resourceManager;
    private CultureInfo _currentCulture;
    
    // 支持的语言列表
    private static readonly CultureInfo[] SupportedCultures = new[]
    {
        new CultureInfo("zh-CN"), // 简体中文（默认）
        new CultureInfo("en-US"), // 英语（美国）
        new CultureInfo("zh-TW"), // 繁体中文（台湾）
        new CultureInfo("ja-JP"), // 日语
        new CultureInfo("ko-KR"), // 韩语
    };

    public LocalizationService()
    {
        // 初始化资源管理器，指向 Strings 资源文件
        _resourceManager = new ResourceManager(
            "DesktopMemo.App.Localization.Resources.Strings",
            typeof(LocalizationService).Assembly
        );
        
        // 默认使用简体中文
        _currentCulture = new CultureInfo("zh-CN");
    }

    /// <summary>
    /// 通过键获取本地化字符串
    /// </summary>
    public string this[string key]
    {
        get
        {
            try
            {
                var value = _resourceManager.GetString(key, _currentCulture);
                
                // 如果找不到资源，返回键名作为后备
                return value ?? $"[{key}]";
            }
            catch
            {
                return $"[{key}]";
            }
        }
    }

    /// <summary>
    /// 当前语言文化
    /// </summary>
    public CultureInfo CurrentCulture
    {
        get => _currentCulture;
        private set
        {
            if (_currentCulture.Name != value.Name)
            {
                var oldCulture = _currentCulture;
                _currentCulture = value;
                
                // 更新线程的 UI 文化
                Thread.CurrentThread.CurrentUICulture = value;
                Thread.CurrentThread.CurrentCulture = value;
                
                // 触发属性变更通知
                OnPropertyChanged(nameof(CurrentCulture));
                
                // 通知所有资源键的值可能已改变
                OnPropertyChanged("Item[]");
                
                // 触发语言切换事件
                LanguageChanged?.Invoke(this, new LanguageChangedEventArgs(value, oldCulture));
            }
        }
    }

    /// <summary>
    /// 切换语言
    /// </summary>
    public void ChangeLanguage(string cultureName)
    {
        try
        {
            var newCulture = new CultureInfo(cultureName);
            
            // 验证是否为支持的语言
            if (!SupportedCultures.Any(c => c.Name == newCulture.Name))
            {
                throw new CultureNotFoundException(
                    $"Language '{cultureName}' is not supported. " +
                    $"Supported languages: {string.Join(", ", SupportedCultures.Select(c => c.Name))}"
                );
            }
            
            CurrentCulture = newCulture;
        }
        catch (CultureNotFoundException)
        {
            // 如果文化名称无效，回退到默认中文
            CurrentCulture = new CultureInfo("zh-CN");
        }
    }

    /// <summary>
    /// 获取所有支持的语言
    /// </summary>
    public IEnumerable<CultureInfo> GetSupportedLanguages()
    {
        return SupportedCultures;
    }

    /// <summary>
    /// 语言切换事件
    /// </summary>
    public event EventHandler<LanguageChangedEventArgs>? LanguageChanged;

    /// <summary>
    /// 属性变更事件（用于支持数据绑定）
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

