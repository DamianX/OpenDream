using System.Linq;
using System.Numerics;
using System.Reflection;
using OpenDreamRuntime.Procs.Native;
using OpenDreamRuntime.Resources;
using OpenDreamShared.Dream;
using Robust.Shared.Maths;
using Robust.Shared.Serialization.Manager.Attributes;

namespace OpenDreamRuntime.Objects.Types;

public sealed class DreamObjectFilter(DreamObjectDefinition objectDefinition) : DreamObject(objectDefinition) {
    public static readonly Dictionary<DreamFilter, DreamFilterList> FilterAttachedTo = new();

    /// <summary>
    /// Fields of each filter type keyed by their DM var name (the <see cref="DataFieldAttribute"/> tag)
    /// </summary>
    private static readonly Dictionary<Type, Dictionary<string, (FieldInfo Field, bool Required)>> FilterFieldsCache = new();

    public override bool ShouldCallNew => false;

    public DreamFilter Filter;

    protected override void HandleDeletion() {
        FilterAttachedTo.Remove(Filter);
        base.HandleDeletion();
    }

    // TODO: Variable getting

    protected override void SetVar(string varName, DreamValue value) {
        if (FilterAttachedTo.TryGetValue(Filter, out var attachedTo)) {
            int index = attachedTo.GetIndexOfFilter(Filter);

            // Create a copy of the filter with the modified variable and replace the DreamFilter with it
            DreamFilter newFilter = Filter with { };
            if (!TrySetFilterVar(newFilter, varName, value)) // Var wasn't a field on the filter; ignore it
                return;
            if (newFilter.Equals(Filter)) // No change
                return;

            Filter = newFilter;
            attachedTo.SetFilter(index, newFilter);
        }
    }

    public static DreamObjectFilter? TryCreateFilter(DreamObjectTree objectTree, IEnumerable<(string Name, DreamValue Value)> properties) {
        Type? filterType = null;
        string? filterTypeName = null;
        List<(string Name, DreamValue Value)> filterValues = new();

        foreach (var property in properties) {
            if (property.Value.IsNull)
                continue;

            if (property.Name == "type" && property.Value.TryGetValueAsString(out filterTypeName)) {
                filterType = DreamFilter.GetType(filterTypeName);
            }

            filterValues.Add(property);
        }

        if (filterType == null)
            return null;

        DreamFilter filter = (DreamFilter)Activator.CreateInstance(filterType)!;
        filter.FilterType = filterTypeName!;
        foreach (var (name, value) in filterValues) {
            if (name == "type")
                continue;

            TrySetFilterVar(filter, name, value); // Unknown vars are ignored, invalid values throw
        }

        foreach (var (name, fieldInfo) in GetFilterFields(filterType)) {
            if (fieldInfo.Required && !filterValues.Any(v => v.Name == name))
                throw new Exception($"Filter type \"{filterTypeName}\" requires a value for \"{name}\"");
        }

        var filterObject = objectTree.CreateObject<DreamObjectFilter>(objectTree.Filter);
        filterObject.Filter = filter;
        return filterObject;
    }

    public static DreamObjectFilter? TryCreateFilter(DreamObjectTree objectTree, DreamList list) {
        static IEnumerable<(string, DreamValue)> EnumerateProperties(DreamList list) {
            foreach (var key in list.EnumerateValues()) {
                if (!key.TryGetValueAsString(out var keyStr))
                    continue;

                using var value = list.GetValue(key);
                if (value.IsNull)
                    continue;

                yield return (keyStr, value);
            }
        }

        return TryCreateFilter(objectTree, EnumerateProperties(list));
    }

    /// <summary>
    /// Sets a variable on a <see cref="DreamFilter"/>, converting the <see cref="DreamValue"/> to the field's type.
    /// Returns false if the variable isn't a field on the filter. Throws if the value can't be converted.
    /// </summary>
    private static bool TrySetFilterVar(DreamFilter filter, string varName, DreamValue value) {
        if (!GetFilterFields(filter.GetType()).TryGetValue(varName, out var fieldInfo))
            return false;

        fieldInfo.Field.SetValue(filter, ConvertValue(value, fieldInfo.Field.FieldType));
        return true;
    }

    private static Dictionary<string, (FieldInfo Field, bool Required)> GetFilterFields(Type filterType) {
        if (FilterFieldsCache.TryGetValue(filterType, out var fields))
            return fields;

        fields = new Dictionary<string, (FieldInfo, bool)>();
        foreach (var field in filterType.GetFields(BindingFlags.Public | BindingFlags.Instance)) {
            var dataField = field.GetCustomAttribute<DataFieldAttribute>();
            if (dataField == null)
                continue;

            // A null tag means the C# field's name with the first letter lowercased
            var varName = dataField.Tag ?? char.ToLowerInvariant(field.Name[0]) + field.Name[1..];
            fields[varName] = (field, dataField.Required);
        }

        FilterFieldsCache[filterType] = fields;
        return fields;
    }

    private static object ConvertValue(DreamValue value, Type targetType) {
        if (targetType == typeof(string)) {
            if (!value.TryGetValueAsString(out var strValue))
                throw new Exception($"Value {value} was not a string");

            return strValue;
        }

        if (targetType == typeof(float) || targetType == typeof(double)) {
            if (!value.TryGetValueAsFloat(out var floatValue))
                throw new Exception($"Value {value} was not a float");

            return floatValue;
        }

        if (targetType == typeof(int) || targetType == typeof(short)) {
            // "icon" fields hold an IconResource; try loading one before falling back to a plain integer
            if (targetType == typeof(int) && IoCManager.Resolve<DreamResourceManager>().TryLoadIcon(value, out var icon))
                return icon.Id;

            if (!value.TryGetValueAsInteger(out var intValue))
                throw new Exception($"Value {value} was not an integer");

            return targetType == typeof(short) ? (short)intValue : intValue;
        }

        if (targetType == typeof(Color)) {
            if (!value.TryGetValueAsString(out var strValue) || !ColorHelpers.TryParseColor(strValue, out var color))
                throw new Exception($"Value {value} was not a color");

            return color;
        }

        if (targetType == typeof(Matrix3x2)) {
            if (!value.TryGetValueAsDreamObject<DreamObjectMatrix>(out var matrixObject))
                throw new Exception($"Value {value} was not a matrix");

            // Matrix3 except not really because DM matrix is actually 3x2
            return new Matrix3x2(
                matrixObject.A, matrixObject.D,
                matrixObject.B, matrixObject.E,
                matrixObject.C, matrixObject.F);
        }

        if (targetType == typeof(ColorMatrix)) {
            if (value.TryGetValueAsString(out var maybeColorString) && ColorHelpers.TryParseColor(maybeColorString, out Color basicColor))
                return new ColorMatrix(basicColor);

            if (value.TryGetValueAsDreamList(out var matrixList) && DreamProcNativeHelpers.TryParseColorMatrix(matrixList, out ColorMatrix matrix))
                return matrix;

            throw new Exception($"Value {value} was not a color matrix");
        }

        throw new Exception($"Cannot convert {value} to filter variable type {targetType}");
    }
}
