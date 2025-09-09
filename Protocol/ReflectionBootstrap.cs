using System.Reflection;

namespace Protocol;

public static class ReflectionBootstrap
{
    public static void RegisterAllParsers(params Assembly[]? assemblies)
    {
        if (assemblies == null || assemblies.Length == 0)
            assemblies = [Assembly.GetExecutingAssembly()];

        foreach (var asm in assemblies)
        {
            foreach (var type in asm.GetTypes())
            {
                foreach (var m in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    var attr = m.GetCustomAttribute<ParserAttribute>();
                    if (attr is null) continue;
                    
                    var del = (PayloadParser) Delegate.CreateDelegate(typeof(PayloadParser), m);
                    ParserRegistry.Register(attr.Version, attr.Type, del);
                }
            }
        }
    }
}