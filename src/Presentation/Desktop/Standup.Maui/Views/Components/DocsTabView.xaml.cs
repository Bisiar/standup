using System.ComponentModel;
using Microsoft.Maui.Controls;
using Standup.Application.ViewModels;

namespace Standup.Maui.Views.Components;

/// <summary>
/// Documentation tab view for displaying embedded wiki documentation with rendered markdown.
/// </summary>
public partial class DocsTabView : ContentView
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DocsTabView"/> class.
    /// </summary>
    public DocsTabView()
    {
        InitializeComponent();
        BindingContextChanged += OnBindingContextChanged;
    }

    private static string GenerateHtml(string markdownContent, bool isDarkMode)
    {
        var escapedMarkdown = System.Text.Json.JsonSerializer.Serialize(markdownContent);
        var bgColor = isDarkMode ? "#1A1A1A" : "#FFFFFF";
        var textColor = isDarkMode ? "#E0E0E0" : "#333333";
        var codeBlockBg = isDarkMode ? "#2D2D2D" : "#F5F5F5";
        var tableBorder = isDarkMode ? "#444444" : "#DDDDDD";
        var linkColor = isDarkMode ? "#6CB4EE" : "#0066CC";

        return $@"<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1"">
    <script src=""https://cdn.jsdelivr.net/npm/marked/marked.min.js""></script>
    <script src=""https://cdn.jsdelivr.net/npm/mermaid/dist/mermaid.min.js""></script>
    <style>
        * {{
            box-sizing: border-box;
        }}
        body {{
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, sans-serif;
            background-color: {bgColor};
            color: {textColor};
            padding: 24px;
            margin: 0;
            line-height: 1.6;
            font-size: 14px;
        }}
        h1, h2, h3, h4, h5, h6 {{
            margin-top: 24px;
            margin-bottom: 16px;
            font-weight: 600;
            line-height: 1.25;
        }}
        h1 {{ font-size: 2em; border-bottom: 1px solid {tableBorder}; padding-bottom: 0.3em; }}
        h2 {{ font-size: 1.5em; border-bottom: 1px solid {tableBorder}; padding-bottom: 0.3em; }}
        h3 {{ font-size: 1.25em; }}
        p {{ margin-top: 0; margin-bottom: 16px; }}
        a {{ color: {linkColor}; text-decoration: none; }}
        a:hover {{ text-decoration: underline; }}
        code {{
            font-family: 'SF Mono', Monaco, Consolas, monospace;
            font-size: 0.9em;
            background-color: {codeBlockBg};
            padding: 0.2em 0.4em;
            border-radius: 4px;
        }}
        pre {{
            background-color: {codeBlockBg};
            padding: 16px;
            border-radius: 8px;
            overflow-x: auto;
            margin: 16px 0;
        }}
        pre code {{
            background-color: transparent;
            padding: 0;
            font-size: 0.85em;
            line-height: 1.45;
        }}
        table {{
            border-collapse: collapse;
            width: 100%;
            margin: 16px 0;
        }}
        th, td {{
            border: 1px solid {tableBorder};
            padding: 8px 12px;
            text-align: left;
        }}
        th {{
            background-color: {codeBlockBg};
            font-weight: 600;
        }}
        blockquote {{
            margin: 0;
            padding: 0 16px;
            border-left: 4px solid {tableBorder};
            color: {(isDarkMode ? "#AAAAAA" : "#666666")};
        }}
        ul, ol {{
            padding-left: 2em;
            margin-top: 0;
            margin-bottom: 16px;
        }}
        li {{ margin: 4px 0; }}
        img {{ max-width: 100%; height: auto; }}
        hr {{
            border: none;
            border-top: 1px solid {tableBorder};
            margin: 24px 0;
        }}
        .mermaid {{
            text-align: center;
            margin: 16px 0;
        }}
        .content-wrapper {{
            max-width: 800px;
            margin: 0 auto;
        }}
    </style>
</head>
<body>
    <div class=""content-wrapper"" id=""content""></div>
    <script>
        // Configure mermaid
        mermaid.initialize({{
            startOnLoad: false,
            theme: '{(isDarkMode ? "dark" : "default")}'
        }});

        // Configure marked
        marked.setOptions({{
            breaks: true,
            gfm: true
        }});

        // Custom renderer for mermaid code blocks
        const renderer = new marked.Renderer();
        const originalCodeRenderer = renderer.code.bind(renderer);

        renderer.code = function(code, language) {{
            if (language === 'mermaid') {{
                return '<div class=""mermaid"">' + code + '</div>';
            }}
            return originalCodeRenderer(code, language);
        }};

        marked.use({{ renderer }});

        // Render markdown
        const markdown = {escapedMarkdown};
        document.getElementById('content').innerHTML = marked.parse(markdown);

        // Initialize mermaid diagrams
        mermaid.run();
    </script>
</body>
</html>";
    }

    private void OnBindingContextChanged(object? sender, EventArgs e)
    {
        if (BindingContext is DocumentsViewModel viewModel)
        {
            viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DocumentsViewModel.DocumentContent) &&
            sender is DocumentsViewModel viewModel)
        {
            LoadMarkdownContent(viewModel.DocumentContent);
        }
    }

    private void LoadMarkdownContent(string markdownContent)
    {
        if (string.IsNullOrEmpty(markdownContent))
        {
            return;
        }

        var isDarkMode = Microsoft.Maui.Controls.Application.Current?.RequestedTheme == AppTheme.Dark;
        var html = GenerateHtml(markdownContent, isDarkMode);

        MarkdownWebView.Source = new HtmlWebViewSource { Html = html };
    }
}
