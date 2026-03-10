using System.Net;
using System.Reflection;

namespace Infrastructure.Email;

internal static class EmailTemplateRenderer
{
    private static readonly Assembly _assembly = typeof(EmailTemplateRenderer).Assembly;

    /// <summary>
    /// Loads an embedded HTML template from Infrastructure/EmailTemplates/{templateName}.html
    /// and replaces all {{Key}} placeholders with the provided variable values.
    /// </summary>
    public static string Render(string templateName, Dictionary<string, string> variables)
    {
        var resourceName = $"Infrastructure.EmailTemplates.{templateName}.html";

        using var stream = _assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException(
                $"Email template '{templateName}' not found. " +
                $"Ensure '{resourceName}' is marked as EmbeddedResource.");

        using var reader = new StreamReader(stream);
        var html = reader.ReadToEnd();

        foreach (var (key, value) in variables)
            html = html.Replace($"{{{{{key}}}}}", WebUtility.HtmlEncode(value ?? string.Empty));

        return html;
    }
}
