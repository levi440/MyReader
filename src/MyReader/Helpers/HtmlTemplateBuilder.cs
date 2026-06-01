namespace MyReader.Helpers;

public static class HtmlTemplateBuilder
{
    public static string BuildReadingHtml(string content, string theme = "light", int fontSize = 18, int lineHeight = 30)
    {
        var (bg, ink) = theme switch
        {
            "dark" => ("#2f3131", "#f1f1f1"),
            "sepia" => ("#FBF1D3", "#433422"),
            _ => ("#f9f9f9", "#1A1A1A")
        };

        return $$"""
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset="utf-8">
                <style>
                    :root {
                        --bg: {{bg}};
                        --ink: {{ink}};
                        --font-size: {{fontSize}}px;
                        --line-height: {{lineHeight}}px;
                    }
                    * { margin: 0; padding: 0; box-sizing: border-box; }
                    body {
                        background: var(--bg);
                        color: var(--ink);
                        font-family: "Inter", "Segoe UI", sans-serif;
                        transition: background 0.3s, color 0.3s;
                    }
                    .content {
                        max-width: 800px;
                        margin: 0 auto;
                        padding: 80px 48px;
                    }
                    p {
                        font-size: var(--font-size);
                        line-height: var(--line-height);
                        margin-bottom: 1.5em;
                        text-indent: 2em;
                    }
                    h1, h2, h3 {
                        margin: 1.5em 0 0.5em;
                        text-indent: 0;
                    }
                    img {
                        max-width: 100%;
                        height: auto;
                    }
                </style>
            </head>
            <body>
                <div class="content">
                    {{content}}
                </div>
            </body>
            </html>
            """;
    }
}
