using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using DesktopMemo.Core.Contracts;

namespace DesktopMemo.App.Localization;

/// <summary>
/// XAML 标记扩展，用于在 XAML 中绑定本地化字符串
/// 用法: {loc:Localize ResourceKey}
/// </summary>
[MarkupExtensionReturnType(typeof(BindingExpression))]
public class LocalizeExtension : MarkupExtension
{
    /// <summary>
    /// 资源键
    /// </summary>
    public string Key { get; set; }

    public LocalizeExtension()
    {
        Key = string.Empty;
    }

    public LocalizeExtension(string key)
    {
        Key = key;
    }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        if (string.IsNullOrEmpty(Key))
        {
            return "[Missing Key]";
        }

        // 创建绑定到 LocalizationService 的索引器
        var binding = new System.Windows.Data.Binding($"LocalizationService[{Key}]")
        {
            Mode = System.Windows.Data.BindingMode.OneWay,
            UpdateSourceTrigger = System.Windows.Data.UpdateSourceTrigger.PropertyChanged
        };

        // 尝试获取目标对象
        if (serviceProvider.GetService(typeof(IProvideValueTarget)) is IProvideValueTarget target)
        {
            if (target.TargetObject is DependencyObject dependencyObject)
            {
                // 返回绑定表达式
                return binding.ProvideValue(serviceProvider);
            }
        }

        // 后备方案：直接返回绑定
        return binding;
    }
}

