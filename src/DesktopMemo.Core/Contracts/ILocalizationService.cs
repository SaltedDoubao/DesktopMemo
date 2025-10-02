using System.Globalization;

namespace DesktopMemo.Core.Contracts;

/// <summary>
/// 本地化服务接口，提供多语言支持
/// </summary>
public interface ILocalizationService
{
    /// <summary>
    /// 通过键获取当前语言的字符串资源
    /// </summary>
    /// <param name="key">资源键</param>
    /// <returns>本地化的字符串，如果未找到则返回键名</returns>
    string this[string key] { get; }

    /// <summary>
    /// 获取当前的语言文化
    /// </summary>
    CultureInfo CurrentCulture { get; }

    /// <summary>
    /// 切换到指定的语言
    /// </summary>
    /// <param name="cultureName">语言文化名称，如 "zh-CN"、"en-US"</param>
    void ChangeLanguage(string cultureName);

    /// <summary>
    /// 获取所有支持的语言列表
    /// </summary>
    /// <returns>支持的语言文化信息</returns>
    IEnumerable<CultureInfo> GetSupportedLanguages();

    /// <summary>
    /// 语言切换事件
    /// </summary>
    event EventHandler<LanguageChangedEventArgs>? LanguageChanged;
}

/// <summary>
/// 语言切换事件参数
/// </summary>
public class LanguageChangedEventArgs : EventArgs
{
    /// <summary>
    /// 新的语言文化
    /// </summary>
    public CultureInfo NewCulture { get; }

    /// <summary>
    /// 旧的语言文化
    /// </summary>
    public CultureInfo? OldCulture { get; }

    public LanguageChangedEventArgs(CultureInfo newCulture, CultureInfo? oldCulture = null)
    {
        NewCulture = newCulture;
        OldCulture = oldCulture;
    }
}

