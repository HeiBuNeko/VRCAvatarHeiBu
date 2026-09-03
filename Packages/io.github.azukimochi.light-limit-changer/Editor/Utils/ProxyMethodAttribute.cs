#nullable enable

using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace io.github.azukimochi;

[AttributeUsage(AttributeTargets.Delegate)]
internal sealed class ProxyMethodAttribute : Attribute
{
    public ProxyMethodAttribute(string declaringType, string methodName, params string[] namespaceParts)
    {
        DeclaringType = declaringType;
        MethodName = methodName;
        NamespaceParts = namespaceParts;
    }

    public string DeclaringType { get; }
    public string MethodName { get; }
    public string[] NamespaceParts { get; }

    private static Type[]? typeCache;

    public static T? CreateProxyMethod<T>() where T : Delegate
    {
        var delegateType = typeof(T);
        var delegatePrameters = delegateType.GetMethod("Invoke")!.GetParameters();
        var attribute = delegateType.GetCustomAttribute<ProxyMethodAttribute>();
        if (attribute == null)
            return null;

        Type? targetType = null;

        typeCache ??= AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes()).ToArray();

        foreach (var type in typeCache)
        {
            if (!attribute.DeclaringType.Equals(type.Name, StringComparison.Ordinal))
                continue;

            var @namespace = type.Namespace;
            foreach (var part in attribute.NamespaceParts)
            {
                if (@namespace?.Contains(part, StringComparison.OrdinalIgnoreCase) ?? false)
                {
                    targetType = type;
                    goto Break;
                }
            }
        }

    Break:
        if (targetType == null)
            return null;
        
        return CreateProxyMethod<T>(targetType.GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static).FirstOrDefault(x => x.Name.Equals(attribute.MethodName, StringComparison.Ordinal) && x.GetParameters().Length == delegatePrameters.Length));
    }

    private static T? CreateProxyMethod<T>(MethodInfo originalMethod) where T : Delegate
    {
        if (originalMethod == null)
            return null;

        var parameters = originalMethod.GetParameters().Select(x => x.ParameterType).ToArray();
        var methodBuilder = new DynamicMethod($"{originalMethod.Name}", originalMethod.ReturnType, parameters, originalMethod.DeclaringType, true);

        var il = methodBuilder.GetILGenerator();

        for (int i = 0; i < parameters.Length; i++)
        {
            switch(i)
            {
                case 0:
                    il.Emit(OpCodes.Ldarg_0);
                    break;
                case 1:
                    il.Emit(OpCodes.Ldarg_1);
                    break;
                case 2:
                    il.Emit(OpCodes.Ldarg_2);
                    break;
                case 3:
                    il.Emit(OpCodes.Ldarg_3);
                    break;
                case < 256:
                    il.Emit(OpCodes.Ldarg_S, i);
                    break;
                default:
                    il.Emit(OpCodes.Ldarg, i);
                    break;
            }
        }

        il.Emit(OpCodes.Call, originalMethod);
        il.Emit(OpCodes.Ret);

        return methodBuilder.CreateDelegate(typeof(T)) as T;
    }
}
