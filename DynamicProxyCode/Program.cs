using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace DynamicProxyCode
{
    class Program
    {
        static void Main(string[] args)
        {
        }
    }
    /// <summary>
    /// Defines the <see cref="IFake" />.
    /// </summary>
    public interface IFake
    {
        /// <summary>
        /// The DoSomething.
        /// </summary>
        /// <param name="actionname">The actionname<see cref="string"/>.</param>
        /// <returns>The <see cref="bool"/>.</returns>
        [Description("系统状态码")]
        [Display(Name = "成功", Description = "请求成功!")]
        bool DoSomething(string actionname);
    }

    /// <summary>
    /// Defines the <see cref="IInvocationHandler" />.
    /// </summary>
    public interface IInvocationHandler
    {
        /// <summary>
        /// The Invoke.
        /// </summary>
        /// <param name="proxy">The proxy<see cref="object"/>.</param>
        /// <param name="method">The method<see cref="MethodInfo"/>.</param>
        /// <param name="args">The args<see cref="object[]"/>.</param>
        /// <returns>The <see cref="object"/>.</returns>
        object Invoke(object proxy, MethodInfo method, object[] args);
    }

    /// <summary>
    /// Defines the <see cref="myIInvocationHandler" />.
    /// </summary>
    public interface myIInvocationHandler
    {
        /// <summary>
        /// The m.
        /// </summary>
        /// <param name="args">The args<see cref="string"/>.</param>
        /// <param name="arg2">The arg2<see cref="string"/>.</param>
        /// <returns>The <see cref="object"/>.</returns>
        object m(string args, string arg2);
    }

    /// <summary>
    /// Defines the <see cref="II" />.
    /// </summary>
    public class II : IInvocationHandler
    {
        /// <summary>
        /// The Invoke.
        /// </summary>
        /// <param name="proxy">The proxy<see cref="object"/>.</param>
        /// <param name="method">The method<see cref="MethodInfo"/>.</param>
        /// <param name="args">The args<see cref="object[]"/>.</param>
        /// <returns>The <see cref="object"/>.</returns>
        public object Invoke(object proxy, MethodInfo method, object[] args)
        {

            foreach (object o in args)

            {

                Console.WriteLine(o.ToString());

            }

            Console.WriteLine($"hahahaha");

            return args[0];
        }
    }

    /// <summary>
    /// Defines the <see cref="ProxyTypeInfo" />.
    /// </summary>
    internal class ProxyTypeInfo
    {
        /// <summary>
        /// Defines the TypeBuilder.
        /// </summary>
        public TypeBuilder TypeBuilder;

        /// <summary>
        /// Defines the Count.
        /// </summary>
        public int Count;

        /// <summary>
        /// Defines the MethodInfos.
        /// </summary>
        public MethodInfo[] MethodInfos;
    }

    /// <summary>
    /// Defines the <see cref="DynamicProxy" />.
    /// </summary>
    public static class DynamicProxy
    {
        /// <summary>
        /// Defines the AssemblyName.
        /// </summary>
        private static readonly string AssemblyName = "DynamicProxyAssembly";

        /// <summary>
        /// Defines the ModuleName.
        /// </summary>
        private static readonly string ModuleName = "DynamicProxyModule";

        /// <summary>
        /// Defines the TypeName.
        /// </summary>
        private static readonly string TypeName = "DynamicProxy";

        /// <summary>
        /// Defines the CanBox.
        /// </summary>
        private static readonly HashSet<Type> CanBox = new HashSet<Type>
        {
            typeof(int), typeof(uint),
            typeof(short), typeof(ushort),
            typeof(long), typeof(ulong),
            typeof(float), typeof(double),
            typeof(sbyte), typeof(byte),
            typeof(char),
            typeof(decimal),
          };

        /// <summary>
        /// Defines the ProxyDict.
        /// </summary>
        private static readonly Dictionary<Type, ProxyTypeInfo> ProxyDict = new Dictionary<Type, ProxyTypeInfo>();

        /// <summary>
        /// The CreateDynamicTypeBuilder.
        /// </summary>
        /// <param name="type">The type<see cref="Type"/>.</param>
        /// <param name="parent">The parent<see cref="Type"/>.</param>
        /// <param name="interfaces">The interfaces<see cref="Type[]"/>.</param>
        /// <returns>The <see cref="TypeBuilder"/>.</returns>
        private static TypeBuilder CreateDynamicTypeBuilder(Type type, Type parent, Type[] interfaces)
        {
            if (ProxyDict.TryGetValue(type, out var info))
            {
                info.Count++;
            }
            else
            {

                ProxyDict[type] = info = new ProxyTypeInfo
                {
                    Count = 1
                };
            }

            //AssemblyBuilder ab =

            //      AssemblyBuilder.DefineDynamicAssembly(

            //          aName,

            //          AssemblyBuilderAccess.Run);

            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(AssemblyName + type.Name), AssemblyBuilderAccess.Run);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule(ModuleName + type.Name);
            return info.TypeBuilder = moduleBuilder.DefineType(TypeName + type.Name + info.Count,
            TypeAttributes.Public | TypeAttributes.Class, parent, interfaces);
        }

        /// <summary>
        /// The ProxyInit.
        /// </summary>
        /// <param name="type">The type<see cref="Type"/>.</param>
        /// <param name="typeBuilder">The typeBuilder<see cref="TypeBuilder"/>.</param>
        /// <param name="methodInfos">The methodInfos<see cref="MethodInfo[]"/>.</param>
        /// <param name="handlerInvokeMethodInfo">The handlerInvokeMethodInfo<see cref="MethodInfo"/>.</param>
        private static void ProxyInit(Type type, TypeBuilder typeBuilder, MethodInfo[] methodInfos, MethodInfo handlerInvokeMethodInfo)
        {
            //定义两个字段
            var handlerFieldBuilder = typeBuilder.DefineField("_handler", typeof(IInvocationHandler), FieldAttributes.Private);

            var methodInfosFieldBuilder = typeBuilder.DefineField("_methodInfos", typeof(MethodInfo), FieldAttributes.Private);

            //定义构造函数
            var constructorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new[] { typeof(IInvocationHandler), typeof(MethodInfo[]) });
            var ilCtor = constructorBuilder.GetILGenerator();

            ilCtor.Emit(OpCodes.Ldarg_0);
            ilCtor.Emit(OpCodes.Call, typeof(object).GetConstructor(new Type[0]) ?? throw new Exception("不可能的错误:object.GetConstructor"));
            ilCtor.Emit(OpCodes.Ldarg_0);
            ilCtor.Emit(OpCodes.Ldarg_1);
            ilCtor.Emit(OpCodes.Stfld, handlerFieldBuilder);
            ilCtor.Emit(OpCodes.Ldarg_0);
            ilCtor.Emit(OpCodes.Ldarg_2);
            ilCtor.Emit(OpCodes.Stfld, methodInfosFieldBuilder);
            ilCtor.Emit(OpCodes.Ret);
            for (var i = 0; i < methodInfos.Length; i++)
            {
                var methodInfo = methodInfos[i];
                var parameterTypes = methodInfo.GetParameters().Select(p => p.ParameterType).ToArray();
                var methodBuilder = typeBuilder.DefineMethod(methodInfo.Name,
                  MethodAttributes.Public | MethodAttributes.Virtual,
                  methodInfo.CallingConvention, methodInfo.ReturnType, parameterTypes);
                var ilMethod = methodBuilder.GetILGenerator();
                ilMethod.Emit(OpCodes.Ldarg_0);
                ilMethod.Emit(OpCodes.Ldfld, handlerFieldBuilder);
                ilMethod.Emit(OpCodes.Ldarg_0);
                ilMethod.Emit(OpCodes.Ldarg_0);
                ilMethod.Emit(OpCodes.Ldfld, methodInfosFieldBuilder);
                ilMethod.Emit(OpCodes.Ldc_I4, i);
                ilMethod.Emit(OpCodes.Ldelem_Ref);
                ilMethod.Emit(OpCodes.Ldc_I4, parameterTypes.Length);
                ilMethod.Emit(OpCodes.Newarr, typeof(object));
                for (var j = 0; j < parameterTypes.Length; j++)
                {
                    ilMethod.Emit(OpCodes.Dup);
                    ilMethod.Emit(OpCodes.Ldc_I4_S, (short)j);
                    ilMethod.Emit(OpCodes.Ldarg_S, (short)(j + 1));
                    if (CanBox.Contains(parameterTypes[j]))
                    {
                        ilMethod.Emit(OpCodes.Box, parameterTypes[j]);
                    }
                    ilMethod.Emit(OpCodes.Stelem_Ref);
                }

                ilMethod.Emit(OpCodes.Callvirt, handlerInvokeMethodInfo);
                ilMethod.Emit(CanBox.Contains(methodInfo.ReturnType) ? OpCodes.Unbox_Any : OpCodes.Castclass, methodInfo.ReturnType);
                ilMethod.Emit(OpCodes.Ret);
            }
        }

        /// <summary>
        /// The CreateProxyByInterface.
        /// </summary>
        /// <typeparam name="T">.</typeparam>
        /// <param name="handler">The handler<see cref="IInvocationHandler"/>.</param>
        /// <param name="userCache">The userCache<see cref="bool"/>.</param>
        /// <returns>The <see cref="T"/>.</returns>
        public static T CreateProxyByInterface<T>(IInvocationHandler handler, bool userCache = true)
        {
            return (T)CreateProxyByInterface(typeof(T), handler, userCache);
        }

        /// <summary>
        /// The CreateProxyByInterface.
        /// </summary>
        /// <param name="type">The type<see cref="Type"/>.</param>
        /// <param name="handler">The handler<see cref="IInvocationHandler"/>.</param>
        /// <param name="userCache">The userCache<see cref="bool"/>.</param>
        /// <returns>The <see cref="object"/>.</returns>
        public static object CreateProxyByInterface(Type type, IInvocationHandler handler, bool userCache = true)
        {
            if (!userCache || !ProxyDict.TryGetValue(type, out var info))
            {
                var handlerInvokeMethodInfo = typeof(IInvocationHandler).GetMethod("Invoke") ?? throw new Exception("不可能的错误:handlerInvokeMethodInfo");

                var typeBuilder = CreateDynamicTypeBuilder(type, null, new[] { type });
                var methodInfos = type.GetMethods();
                ProxyInit(type, typeBuilder, methodInfos, handlerInvokeMethodInfo);
                info = ProxyDict[type];
                if (info.Count == 1)
                {
                    info.MethodInfos = methodInfos;
                }
            }

            //Type t = info.TypeBuilder.CreateTypeInfo();
            return Activator.CreateInstance(info.TypeBuilder.CreateTypeInfo(), handler, info.MethodInfos) ?? throw new Exception("不同环境此处可能需要改写");
        }

        /// <summary>
        /// The CreateProxyByType.
        /// </summary>
        /// <typeparam name="T">.</typeparam>
        /// <param name="handler">The handler<see cref="IInvocationHandler"/>.</param>
        /// <param name="userCache">The userCache<see cref="bool"/>.</param>
        /// <returns>The <see cref="T"/>.</returns>
        public static T CreateProxyByType<T>(IInvocationHandler handler, bool userCache = true)
        {
            return (T)CreateProxyByType(typeof(T), handler, userCache);
        }

        /// <summary>
        /// The CreateProxyByType.
        /// </summary>
        /// <param name="type">The type<see cref="Type"/>.</param>
        /// <param name="handler">The handler<see cref="IInvocationHandler"/>.</param>
        /// <param name="userCache">The userCache<see cref="bool"/>.</param>
        /// <returns>The <see cref="object"/>.</returns>
        public static object CreateProxyByType(Type type, IInvocationHandler handler, bool userCache = true)
        {

            if (!userCache || !ProxyDict.TryGetValue(type, out var info))
            {
                var handlerInvokeMethodInfo = typeof(IInvocationHandler).GetMethod("Invoke") ??
                               throw new Exception("不可能的错误:handlerInvokeMethodInfo");

                var typeBuilder = CreateDynamicTypeBuilder(type, type, null);
                var methodInfos = type.GetMethods().Where(methodInfo => methodInfo.IsVirtual || methodInfo.IsAbstract).ToArray();

                ProxyInit(type, typeBuilder, methodInfos, handlerInvokeMethodInfo);
                info = ProxyDict[type];
                if (info.Count == 1)
                {
                    info.MethodInfos = methodInfos;
                }
            }

            return Activator.CreateInstance(info.TypeBuilder.CreateTypeInfo(), handler, info.MethodInfos) ?? throw new Exception("不同环境此处可能需要改写");
        }
    }
}
