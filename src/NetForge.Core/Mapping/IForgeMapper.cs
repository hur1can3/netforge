namespace NetForge.Core.Mapping;

public interface IForgeMapper
{
    TDestination Map<TSource, TDestination>(TSource source);
}

// TODO(map-001): Replace reflection placeholder with cached compiled expressions for performance.
public sealed class ForgeReflectionMapper : IForgeMapper
{
    public TDestination Map<TSource, TDestination>(TSource source)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));
        var dest = Activator.CreateInstance<TDestination>();
        var sProps = typeof(TSource).GetProperties();
        var dProps = typeof(TDestination).GetProperties().Where(p => p.CanWrite).ToDictionary(p => p.Name);
        foreach (var sp in sProps)
        {
            if (dProps.TryGetValue(sp.Name, out var dp) && dp.PropertyType.IsAssignableFrom(sp.PropertyType))
            {
                var val = sp.GetValue(source);
                dp.SetValue(dest, val);
            }
        }
        return dest;
    }
}
