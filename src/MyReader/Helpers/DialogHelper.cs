using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace MyReader.Helpers;

/// <summary>
/// 对话框辅助类，解决 WinUI 3 中 XamlRoot 的问题
/// </summary>
public static class DialogHelper
{
    /// <summary>
    /// 显示 ContentDialog（自动设置 XamlRoot）
    /// </summary>
    public static async Task<ContentDialogResult> ShowAsync(ContentDialog dialog, XamlRoot xamlRoot)
    {
        dialog.XamlRoot = xamlRoot;
        return await dialog.ShowAsync();
    }

    /// <summary>
    /// 显示消息对话框
    /// </summary>
    public static async Task ShowMessageAsync(string title, string message, XamlRoot xamlRoot)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = "确定",
            XamlRoot = xamlRoot
        };
        await dialog.ShowAsync();
    }
}
