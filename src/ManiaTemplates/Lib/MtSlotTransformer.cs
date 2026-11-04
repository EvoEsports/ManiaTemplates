using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using ManiaTemplates.Components;
using ManiaTemplates.ControlElements;
using ManiaTemplates.Exceptions;
using ManiaTemplates.Interfaces;

namespace ManiaTemplates.Lib;

/// <summary>
/// Creates the code rendering slots: the calls at the position of a slot, the methods rendering the contents of a slot
/// and the delegates passing the arguments of a slot to its contents.
/// </summary>
/// <param name="maniaTemplateLanguage">The template language the code is generated in.</param>
internal class MtSlotTransformer(IManiaTemplateLanguage maniaTemplateLanguage)
{
    private readonly List<string> _slotRenderMethods = [];
    private readonly Dictionary<string, MtDataContext> _slotArguments = new();
    private readonly HashSet<string> _slotRendererDelegates = [];

    private static readonly Regex SlotArgumentValueRegex = new(@"^\{\{\s*([A-Za-z_]\w*)\s*\}\}$");

    /// <summary>
    /// Creates the call rendering the contents of a slot at the position of a slot-node.
    /// </summary>
    /// <param name="parentComponent">The component containing the slot.</param>
    /// <param name="attributeList">The attributes of the slot node.</param>
    /// <param name="context">The variables available at the slot.</param>
    public string CreateSlotCall(MtComponent parentComponent, MtComponentAttributes attributeList,
        MtDataContext context)
    {
        var slotName = attributeList.PullName().ToLower();
        var slotArguments = ParseSlotArguments(slotName, attributeList, context);
        RegisterSlotArguments(parentComponent, slotName, slotArguments);
        var slotArgumentsList = slotArguments.Select(argument =>
            MtCodeNames.MethodCallArgument(argument.Name, argument.Variable));

        return new StringBuilder()
            .AppendLine(maniaTemplateLanguage.FeatureBlockStart())
            .AppendLine($"{MtCodeNames.SlotRendererVariable(slotName)}?.Invoke({string.Join(", ", slotArgumentsList)});")
            .Append(maniaTemplateLanguage.FeatureBlockEnd())
            .ToString();
    }

    /// <summary>
    /// Goes through the children of a component node and extracts the node containing the content of each slot.
    /// All children that are not a template filling a named slot, are the content of the "default" slot.
    /// </summary>
    /// <returns>
    /// A map of slot names and the nodes containing their contents.
    /// </returns>
    /// <param name="componentNode">The node using the component.</param>
    /// <param name="component">The used component.</param>
    public static Dictionary<string, XmlNode> GetSlotNodesGroupedBySlotName(XmlNode componentNode,
        MtComponent component)
    {
        var contentsByName = new Dictionary<string, XmlNode>();
        var namedSlotTemplates = new HashSet<string>();

        // Iterate over a copy, as named slot templates are removed from the node
        foreach (var childNode in componentNode.ChildNodes.Cast<XmlNode>().ToList())
        {
            if (childNode.Name != MtTransformer.FragmentTag)
            {
                continue;
            }

            // Get the name from the slot attribute, or default if not found.
            var slotName = childNode.Attributes?["slot"]?.Value.ToLower() ?? "default";

            if (slotName.Trim().Length == 0)
            {
                throw new EmptyNodeAttributeException(
                    $"There's a template tag with empty slot name in <{componentNode.Name}></>.");
            }

            if (slotName == "default")
            {
                // Do not strip contents for default slot from node
                continue;
            }

            if (!namedSlotTemplates.Add(slotName))
            {
                throw new DuplicateSlotException(
                    $"""There are multiple templates for the slot "{slotName}" in <{componentNode.Name}></>.""");
            }

            if (childNode.Attributes!.Count > 1)
            {
                throw new UnsupportedAttributeException(
                    $"Templates filling a slot only support the slot attribute, found: {string.Join(", ", childNode.Attributes.Cast<XmlAttribute>().Select(attribute => attribute.Name).Where(name => name != "slot"))}. Place conditions and loops inside the template instead.");
            }

            if (component.Slots.Contains(slotName))
            {
                // Only add templates, if the component does have a fitting slot
                contentsByName[slotName] = childNode.Clone();
            }

            componentNode.RemoveChild(childNode);
        }

        contentsByName["default"] = componentNode;

        return contentsByName;
    }

    /// <summary>
    /// Creates the context of the contents of a slot: the arguments passed by the slot are available in addition to
    /// the variables available where the component is used.
    /// </summary>
    /// <param name="component">The component containing the slot.</param>
    /// <param name="slotName">The name of the slot.</param>
    /// <param name="context">The variables available where the component is used.</param>
    /// <param name="componentTag">The tag of the component, used in error messages.</param>
    public MtDataContext CreateSlotContext(MtComponent component, string slotName, MtDataContext context,
        string componentTag)
    {
        var slotContext = context.Copy();

        foreach (var (name, type) in GetSlotArguments(component, slotName))
        {
            if (!slotContext.TryAdd(name, type))
            {
                throw new InvalidSlotArgumentException(
                    $"The argument {name} passed to the slot {slotName} of <{componentTag}> conflicts with a variable of the same name, please rename the variable.");
            }
        }

        return slotContext;
    }

    /// <summary>
    /// Adds the method rendering the contents of a slot, receiving the variables available in the slot.
    /// </summary>
    /// <param name="scope">The id of the component usage the slot contents belong to.</param>
    /// <param name="context">The variables available in the slot contents.</param>
    /// <param name="slotName">The name of the slot.</param>
    /// <param name="slotContent">The template contents of the slot.</param>
    /// <param name="parentComponent">The component whose template contains the slot contents.</param>
    public void AddSlotRenderMethod(int scope, MtDataContext context, string slotName, string slotContent,
        MtComponent parentComponent)
    {
        var methodArguments = new List<string>();

        // Add slot render methods.
        AppendSlotRendererParameters(parentComponent, methodArguments);

        // Add the variables available in the slot, properties of the rendered component are available everywhere.
        methodArguments.AddRange(context.Select(variable => $"{variable.Value} {variable.Key}"));

        _slotRenderMethods.Add(maniaTemplateLanguage.RenderMethod(MtCodeNames.SlotRenderMethod(scope, slotName),
            methodArguments, slotContent));
    }

    /// <summary>
    /// Creates the lambda passed to a component, which renders the content of a slot with the arguments passed by the
    /// slot.
    /// </summary>
    /// <param name="scope">The id of the component usage the slot contents belong to.</param>
    /// <param name="component">The component containing the slot.</param>
    /// <param name="slotName">The name of the slot.</param>
    /// <param name="slotContext">The variables available in the slot contents.</param>
    /// <param name="parentComponent">The component whose template contains the slot contents.</param>
    public string CreateSlotRendererLambda(int scope, MtComponent component, string slotName,
        MtDataContext slotContext, MtComponent parentComponent)
    {
        // The lambda parameters are named differently, to not collide with the variables around the component
        var parameters = GetSlotArguments(component, slotName).Keys
            .Select((name, index) => (Name: name, Parameter: $"__slotArgument{index}"))
            .ToDictionary(argument => argument.Name, argument => argument.Parameter);

        var arguments = slotContext.Keys
            .Select(name => MtCodeNames.MethodCallArgument(name, parameters.GetValueOrDefault(name, name)))
            .Concat(parentComponent.Slots.Select(MtCodeNames.SlotRendererVariable).Select(MtCodeNames.MethodCallArgument));

        return $"({string.Join(", ", parameters.Values)}) => " +
               $"{MtCodeNames.SlotRenderMethod(scope, slotName)}({string.Join(", ", arguments)})";
    }

    /// <summary>
    /// Takes all available slots of a component and appends the parameters receiving the slot renderers to the list.
    /// </summary>
    /// <param name="component">The component whose slots are rendered.</param>
    /// <param name="arguments">The parameters the slot renderers are appended to.</param>
    public void AppendSlotRendererParameters(MtComponent component, List<string> arguments)
    {
        foreach (var slotName in component.Slots)
        {
            var delegateName = MtCodeNames.SlotRendererDelegate(component, slotName);
            _slotRendererDelegates.Add(delegateName);
            arguments.Add($"{delegateName}? {MtCodeNames.SlotRendererVariable(slotName)}");
        }
    }

    /// <summary>
    /// Creates the delegates rendering slots, receiving the arguments passed by the slot.
    /// </summary>
    public string CreateSlotRendererDelegates()
    {
        var delegates = new StringBuilder();
        foreach (var delegateName in _slotRendererDelegates)
        {
            var parameters = (_slotArguments.GetValueOrDefault(delegateName) ?? new MtDataContext())
                .Select(argument => $"{argument.Value} {argument.Key}");

            delegates.AppendLine($"private delegate void {delegateName}({string.Join(", ", parameters)});");
        }

        return maniaTemplateLanguage.FeatureBlock(delegates.ToString()).ToString();
    }

    /// <summary>
    /// Returns the methods rendering the contents of slots.
    /// </summary>
    public IEnumerable<string> SlotRenderMethods => _slotRenderMethods;

    /// <summary>
    /// Throws if a template filling a slot has a condition or loop, which have to be placed inside of it.
    /// Conditions on these templates are ambiguous, as there is a single slot content, which is rendered by the component.
    /// </summary>
    /// <param name="loop">The loop of the template.</param>
    /// <param name="condition">The condition of the template.</param>
    public static void EnsureSlotTemplateHasNoControlFlow(MtLoop? loop, MtCondition? condition)
    {
        if (loop == null && condition == null)
        {
            return;
        }

        throw new UnsupportedAttributeException(
            $"<{MtTransformer.FragmentTag} slot> does not support conditions and loops, place them inside of it instead.");
    }

    /// <summary>
    /// Parses the arguments a slot passes to its contents, which are variables available at the position of the slot,
    /// like <![CDATA[<slot item="{{ item }}"/>]]>.
    /// </summary>
    private static List<(string Name, string Variable, string Type)> ParseSlotArguments(string slotName,
        MtComponentAttributes attributeList, MtDataContext context)
    {
        var arguments = new List<(string Name, string Variable, string Type)>();
        foreach (var (name, attribute) in attributeList)
        {
            if (!StringUtils.IsVariableName(name))
            {
                throw new InvalidSlotArgumentException(
                    $"The argument {name} of the slot {slotName} must be a valid variable name, not a C# keyword and not starting with __.");
            }

            var variable = SlotArgumentValueRegex.Match(attribute.Value);
            if (!variable.Success || !context.TryGetValue(variable.Groups[1].Value, out var type))
            {
                throw new InvalidSlotArgumentException(
                    $"The argument {name} of the slot {slotName} must pass a single variable available at the slot, like {name}=\"{{{{ variable }}}}\", found: {attribute.Value}");
            }

            arguments.Add((name, variable.Groups[1].Value, type));
        }

        return arguments;
    }

    /// <summary>
    /// Registers the arguments a slot passes to its contents, which must be the same at every position of the slot.
    /// </summary>
    private void RegisterSlotArguments(MtComponent component, string slotName,
        List<(string Name, string Variable, string Type)> arguments)
    {
        var slotArguments = new MtDataContext();
        foreach (var argument in arguments)
        {
            slotArguments[argument.Name] = argument.Type;
        }

        var delegateName = MtCodeNames.SlotRendererDelegate(component, slotName);
        if (!_slotArguments.TryGetValue(delegateName, out var registeredArguments))
        {
            _slotArguments[delegateName] = slotArguments;
            return;
        }

        if (registeredArguments.Count != slotArguments.Count || registeredArguments.Any(argument =>
                !slotArguments.TryGetValue(argument.Key, out var type) || type != argument.Value))
        {
            throw new InvalidSlotArgumentException(
                $"The slot {slotName} must pass arguments with the same names and types at every position.");
        }
    }

    /// <summary>
    /// Returns the arguments the slot of a component passes to its contents.
    /// </summary>
    private MtDataContext GetSlotArguments(MtComponent component, string slotName)
    {
        return _slotArguments.GetValueOrDefault(MtCodeNames.SlotRendererDelegate(component, slotName)) ??
               new MtDataContext();
    }
}
