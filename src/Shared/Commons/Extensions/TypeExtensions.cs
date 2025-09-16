namespace ProductAuthMicroservice.Commons.Extensions;

public static class TypeExtensions
{
    /// <summary>
    /// Gets a human-readable name for generic types
    /// </summary>
    /// <param name="type">The type to get name for</param>
    /// <returns>Human-readable type name</returns>
    public static string GetGenericTypeName(this Type type)
    {
        if (type.IsGenericType)
        {
            var genericTypes = string.Join(",", type.GetGenericArguments().Select(t => t.Name).ToArray());
            return $"{type.Name.Remove(type.Name.IndexOf('`'))}<{genericTypes}>";
        }
        
        return type.Name;
    }

    /// <summary>
    /// Gets the readable name of a type without namespace
    /// </summary>
    /// <param name="type">The type to get name for</param>
    /// <returns>Type name without namespace</returns>
    public static string GetTypeName(this Type type)
    {
        return type.IsGenericType ? type.GetGenericTypeName() : type.Name;
    }
}
