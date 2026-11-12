using System.Text.RegularExpressions;
using ManiaTemplates.Components;
using ManiaTemplates.ControlElements;

namespace ManiaTemplates.Lib;

/// <summary>
/// Names of the methods, delegates and variables in the generated code.
/// </summary>
internal static class MtCodeNames
{
    private static readonly Regex NonWordCharacterRegex = new(@"\W");

    /// <summary>
    /// Returns the method name that renders the given component.
    /// </summary>
    /// <param name="component">The rendered component.</param>
    /// <param name="context">The variables available where the component is used.</param>
    public static string ComponentRenderMethod(MtComponent component, MtDataContext context) =>
        $"Render_Component_{component.Id()}{context}";

    /// <summary>
    /// Returns the method name that renders the scripts of a given component.
    /// </summary>
    /// <param name="component">The component the scripts belong to.</param>
    public static string ComponentScriptsRenderMethod(MtComponent component) =>
        $"Render_ComponentScript_{component.Id()}";

    /// <summary>
    /// Returns a valid variable name alias for a fallthrough attribute.
    /// </summary>
    /// <param name="attributeName">The name of the attribute.</param>
    public static string FallthroughAttributeAlias(string attributeName) =>
        "FT_" + NonWordCharacterRegex.Replace(attributeName.Replace("-", "HYPH"), "");

    /// <summary>
    /// Returns the name of the method that renders the slot contents.
    /// </summary>
    /// <param name="scope">The id of the component usage the slot contents belong to.</param>
    /// <param name="slotName">The name of the slot.</param>
    public static string SlotRenderMethod(int scope, string slotName) =>
        $"Render_Slot_{scope.GetHashCode()}_{slotName}";

    /// <summary>
    /// Returns the name of the delegate type rendering a slot of the given component.
    /// </summary>
    /// <param name="component">The component containing the slot.</param>
    /// <param name="slotName">The name of the slot.</param>
    public static string SlotRendererDelegate(MtComponent component, string slotName) =>
        $"SlotRenderer_{component.Id()}_{slotName}";

    /// <summary>
    /// Returns the name of the variable that renders a slot.
    /// </summary>
    /// <param name="slotName">The name of the slot.</param>
    public static string SlotRendererVariable(string slotName) => $"__slotRenderer_{slotName}";

    /// <summary>
    /// Formats a local variable to be passed to a render method as named argument.
    /// </summary>
    /// <param name="argumentName">The name of the variable and the parameter.</param>
    public static string MethodCallArgument(string argumentName) => MethodCallArgument(argumentName, argumentName);

    /// <summary>
    /// Formats a value to be passed to a render method as named argument.
    /// </summary>
    /// <param name="argumentName">The name of the parameter.</param>
    /// <param name="argumentValue">The C# expression passed.</param>
    public static string MethodCallArgument(string argumentName, string argumentValue) =>
        $"{argumentName}: {argumentValue}";
}
