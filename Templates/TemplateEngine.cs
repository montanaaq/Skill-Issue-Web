using System.Collections;
using System.Net;
using System.Reflection;
using System.Text;

namespace SkillIssue.Templates;

using RenderState = (
    ITemplateData? TemplateData,
    StringBuilder Output,
    HashSet<string> ActiveFiles,
    Dictionary<string, object?> Locals,
    CancellationToken CancellationToken
);

internal interface ITemplateData
{
    (bool Found, object? Value) ReadValue(string name);
}

internal sealed class TemplateEngine
{
    public static async Task<string> RenderAsync(
        string fileName,
        ITemplateData? templateData,
        CancellationToken cancellationToken
    )
    {
        var output = new StringBuilder();
        var activeFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var locals = new Dictionary<string, object?>();

        await RenderFileAsync(
            fileName,
            (templateData, output, activeFiles, locals, cancellationToken)
        );

        return output.ToString();
    }

    private static async Task RenderFileAsync(string fileName, RenderState render)
    {
        if (!TemplateSyntax.IsTemplatePath(fileName))
        {
            throw new InvalidOperationException($"Invalid template path: {fileName}");
        }

        if (!render.ActiveFiles.Add(fileName))
        {
            throw new InvalidOperationException($"Circular include: {fileName}");
        }

        string filePath = Path.Combine(AppContext.BaseDirectory, "Templates", fileName);
        string template = await File.ReadAllTextAsync(filePath, render.CancellationToken);
        var tags = TemplateSyntax.ReadTags(template, render.CancellationToken);
        await RenderBlockAsync(fileName, template, tags, render, 0, true);

        render.ActiveFiles.Remove(fileName);
    }

    private static async Task<int> RenderBlockAsync(
        string fileName,
        string template,
        List<TemplateTag> tags,
        RenderState render,
        int blockStart,
        bool visible
    )
    {
        var (templateData, output, _, locals, cancellationToken) = render;

        TemplateTag? openingTag = null;
        int offset = 0;
        if (blockStart > 0)
        {
            var previousTag = tags[blockStart - 1];
            openingTag = previousTag;
            offset = previousTag.Index + previousTag.Length;
        }

        var block = openingTag?.Kind;
        int index = blockStart;
        for (; index < tags.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var tag = tags[index];

            if (visible)
            {
                output.Append(template, offset, tag.Index - offset);
            }

            switch (tag.Kind)
            {
                case TemplateTagKind.Parameter when tag.Argument is not null:
                    break;

                case TemplateTagKind.If:
                    bool? condition = !visible ? null : ReadCondition(tag);
                    bool showIf = condition == true;
                    bool showElse = condition == false;

                    int branchEnd = await RenderBlockAsync(
                        fileName,
                        template,
                        tags,
                        render,
                        index + 1,
                        showIf
                    );
                    if (tags[branchEnd].Kind == TemplateTagKind.Else)
                    {
                        branchEnd = await RenderBlockAsync(
                            fileName,
                            template,
                            tags,
                            render,
                            branchEnd + 1,
                            showElse
                        );
                    }

                    index = branchEnd;
                    break;

                case TemplateTagKind.ForEach when tag.Argument is string collectionName:
                    int loopBodyStart = index + 1;
                    if (visible)
                    {
                        object? collection = ReadValue(collectionName, tag);
                        if (collection is string || collection is not IEnumerable items)
                        {
                            throw InvalidTag(tag);
                        }

                        bool hadPreviousValue = locals.TryGetValue(
                            tag.Name,
                            out object? previousValue
                        );
                        foreach (object? item in items)
                        {
                            locals[tag.Name] = item;
                            index = await RenderBlockAsync(
                                fileName,
                                template,
                                tags,
                                render,
                                loopBodyStart,
                                true
                            );
                        }

                        if (hadPreviousValue)
                        {
                            locals[tag.Name] = previousValue;
                        }
                        else
                        {
                            locals.Remove(tag.Name);
                        }
                    }

                    if (index < loopBodyStart)
                    {
                        index = await RenderBlockAsync(
                            fileName,
                            template,
                            tags,
                            render,
                            loopBodyStart,
                            false
                        );
                    }
                    break;

                case TemplateTagKind.Else when block == TemplateTagKind.If:
                case TemplateTagKind.EndIf when block is TemplateTagKind.If or TemplateTagKind.Else:
                case TemplateTagKind.EndForEach when block == TemplateTagKind.ForEach:
                    return index;

                case TemplateTagKind.Include:
                    if (visible)
                    {
                        await RenderFileAsync(tag.Name, render);
                    }
                    break;

                case TemplateTagKind.Variable:
                    if (visible)
                    {
                        object? value = ReadValue(tag.Name, tag);
                        string? encodedValue = WebUtility.HtmlEncode(value?.ToString());
                        output.Append(encodedValue);
                    }
                    break;

                default:
                    throw InvalidTag(tag);
            }

            offset = tags[index].Index + tags[index].Length;
        }

        if (openingTag is TemplateTag unclosedTag)
        {
            throw InvalidTag(unclosedTag);
        }

        output.Append(template, offset, template.Length - offset);
        return index;

        bool ReadCondition(TemplateTag tag)
        {
            object? value = ReadValue(tag.Name, tag);
            if (tag.Argument is string literal)
            {
                if (value is not string text)
                {
                    throw InvalidTag(tag);
                }

                return text == literal;
            }

            if (value is not bool condition)
            {
                throw InvalidTag(tag);
            }

            return condition;
        }

        object? ReadValue(string name, TemplateTag tag)
        {
            string[] path = name.Split('.');
            if (!locals.TryGetValue(path[0], out object? value))
            {
                var (found, parameterValue) = templateData?.ReadValue(path[0]) ?? default;
                value = found ? parameterValue : throw InvalidTag(tag);
            }

            const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
            foreach (string member in path.Skip(1))
            {
                var type = value?.GetType() ?? throw InvalidTag(tag);
                var property = type.GetProperty(member, flags);
                if (
                    property?.GetMethod is { IsPublic: true }
                    && property.GetIndexParameters().Length == 0
                )
                {
                    value = property.GetValue(value);
                }
                else
                {
                    var field = type.GetField(member, flags) ?? throw InvalidTag(tag);
                    value = field.GetValue(value);
                }
            }

            return value;
        }

        InvalidOperationException InvalidTag(TemplateTag tag)
        {
            string source = template.Substring(tag.Index, tag.Length);
            return new InvalidOperationException(
                $"Invalid tag or value in {fileName} at offset {tag.Index}: {source}"
            );
        }
    }
}
