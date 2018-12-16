using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace PrivateTech.ProxyType
{
    public static class ProxyTypeBuilder
    {
        private static ModuleBuilder moduleBuilder;

        static ProxyTypeBuilder()
        {
            AssemblyName assemblyName = new AssemblyName("ProxyTypeAssembly");

            AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);

            moduleBuilder = assemblyBuilder.DefineDynamicModule("ProxyTypeModule");
        }

        private static ConcurrentDictionary<Type, Type> dic = new ConcurrentDictionary<Type, Type>();

        public static TInterface GetInstace<TInterface>()
        {
            var type = CheckType<TInterface>();

            if (dic.TryGetValue(type, out var proxyType)) return CreateInstace();

            TypeBuilder typeBuilder = CreateTypeBuilder(ref type);

            Array.ForEach(type.GetProperties(), property => property.DefineProperty(typeBuilder));

            proxyType = typeBuilder.CreateTypeInfo();

            dic.TryAdd(type, proxyType);

            return CreateInstace();

            TInterface CreateInstace() => (TInterface)Activator.CreateInstance(proxyType);
        }

        private static void DefineProperty(this PropertyInfo propertyInfo, TypeBuilder typeBuilder)
        {
            var name = propertyInfo.Name;
            var type = propertyInfo.PropertyType;

            PropertyBuilder propertyBuilder = typeBuilder.DefineProperty(name, PropertyAttributes.HasDefault, type, null);

            SetPropertyMethods(ref typeBuilder, ref propertyBuilder, ref type, ref name);

            SetPropertyAttributes(ref propertyBuilder, ref propertyInfo);
        }

        private static void SetPropertyMethods(ref TypeBuilder typeBuilder, ref PropertyBuilder propertyBuilder, ref Type type, ref string name)
        {
            FieldBuilder fieldBuilder = typeBuilder.DefineField($"{name}_field", type, FieldAttributes.Private);

            MethodAttributes getSetAttr =
               MethodAttributes.Public |
               MethodAttributes.SpecialName |
               MethodAttributes.HideBySig |
               MethodAttributes.Virtual;

            MethodBuilder getMethodBuilder = typeBuilder.DefineMethod($"get_{name}", getSetAttr, type, Type.EmptyTypes);
            ILGenerator getIL = getMethodBuilder.GetILGenerator();
            getIL.Emit(OpCodes.Ldarg_0);
            getIL.Emit(OpCodes.Ldfld, fieldBuilder);
            getIL.Emit(OpCodes.Ret);


            MethodBuilder setMethodBuilder = typeBuilder.DefineMethod($"set_{name}", getSetAttr, null, new Type[] { type });
            ILGenerator setIL = setMethodBuilder.GetILGenerator();
            setIL.Emit(OpCodes.Ldarg_0);
            setIL.Emit(OpCodes.Ldarg_1);
            setIL.Emit(OpCodes.Stfld, fieldBuilder);
            setIL.Emit(OpCodes.Ret);


            propertyBuilder.SetGetMethod(getMethodBuilder);
            propertyBuilder.SetSetMethod(setMethodBuilder);
        }

        private static void SetPropertyAttributes(ref PropertyBuilder propertyBuilder, ref PropertyInfo propertyInfo)
        {
            foreach (var attribute in propertyInfo.CustomAttributes)
            {
                object[] constructorArguments = attribute.ConstructorArguments.Select(x => x.Value).ToArray();

                var properties = attribute.NamedArguments.Where(x => x.MemberInfo.MemberType == MemberTypes.Property).Select(x => new
                {
                    PropertyInfo = x.MemberInfo as PropertyInfo,
                    x.TypedValue.Value
                });

                var fields = attribute.NamedArguments.Where(x => x.MemberInfo.MemberType == MemberTypes.Field).Select(x => new
                {
                    FieldInfo = x.MemberInfo as FieldInfo,
                    x.TypedValue.Value
                });

                propertyBuilder.SetCustomAttribute(
                    new CustomAttributeBuilder(
                        attribute.Constructor,
                        constructorArguments,
                        properties.Select(x => x.PropertyInfo).ToArray(),
                        properties.Select(x => x.Value).ToArray(),
                        fields.Select(x => x.FieldInfo).ToArray(),
                        fields.Select(x => x.Value).ToArray()
                    )
                );
            }
        }

        private static TypeBuilder CreateTypeBuilder(ref Type type)
        {
            var typeBuilder = moduleBuilder.DefineType($"{type.Name}_proxy", TypeAttributes.Public);
            typeBuilder.AddInterfaceImplementation(type);
            return typeBuilder;
        }

        private static Type CheckType<TInterface>()
        {
            var type = typeof(TInterface);

            if (!type.IsInterface)
                throw new Exception("Type is not interface");

            return type;
        }
    }
}
