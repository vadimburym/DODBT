using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

internal sealed class BtTypePicker : AdvancedDropdown
{
    private readonly Type _baseType;
    private readonly Action<Type> _select;
    private readonly Dictionary<int, Type> _types = new();
    private static readonly Dictionary<Type, Type[]> Cache = new();

    internal BtTypePicker(Type baseType, Action<Type> select) : base(new AdvancedDropdownState())
    {
        _baseType = baseType;
        _select = select;
        minimumSize = new Vector2(420, 320);
    }

    internal static Type Resolve(string serializedTypeName)
    {
        if (string.IsNullOrEmpty(serializedTypeName)) return null;
        int space = serializedTypeName.IndexOf(' ');
        return space < 0 ? null : Type.GetType(serializedTypeName.Substring(space + 1) + ", " + serializedTypeName.Substring(0, space));
    }

    internal static Type[] GetTypes(Type baseType)
    {
        if (baseType == null) return Array.Empty<Type>();
        if (Cache.TryGetValue(baseType, out var types)) return types;
        var assemblies = new HashSet<string>(CompilationPipeline.GetAssemblies(AssembliesType.Player).Select(a => a.name));
        types = TypeCache.GetTypesDerivedFrom(baseType).Append(baseType)
            .Where(t => t.IsClass && !t.IsAbstract && !t.ContainsGenericParameters && t.IsSerializable
                && !typeof(UnityEngine.Object).IsAssignableFrom(t)
                && assemblies.Contains(t.Assembly.GetName().Name)
                && t.GetConstructor(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.NonPublic, null, Type.EmptyTypes, null) != null)
            .Distinct().OrderBy(t => t.Name, StringComparer.Ordinal)
            .ThenBy(t => t.Assembly.GetName().Name, StringComparer.Ordinal)
            .ThenBy(t => t.FullName, StringComparer.Ordinal).ToArray();
        Cache[baseType] = types;
        return types;
    }

    protected override AdvancedDropdownItem BuildRoot()
    {
        _types.Clear();
        var root = new AdvancedDropdownItem("Select " + (_baseType?.Name ?? "type"));
        var none = new AdvancedDropdownItem("None") { id = 0 };
        root.AddChild(none);
        int id = 1;
        foreach (var type in GetTypes(_baseType))
        {
            _types[id] = type;
            root.AddChild(new AdvancedDropdownItem(DisplayName(type)) { id = id++ });
        }
        return root;
    }

    protected override void ItemSelected(AdvancedDropdownItem item) =>
        _select(item.id == 0 ? null : _types[item.id]);

    private static string DisplayName(Type type) =>
        type.Name + " (" + type.Assembly.GetName().Name + "; " + (type.Namespace ?? "global namespace") + ")";
}
