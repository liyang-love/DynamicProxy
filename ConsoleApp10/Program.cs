using Moq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Proxies;
using System.Text;

namespace ConsoleApp10
{
    class Program
    {
        static void Main(string[] args)
        {
            //var dynamicProxy = new DynamicProxy<IFake>(IFake);

            var test = DynamicProxy.CreateProxyByInterface<IFake>(new II());

            var test1 = test.DoSomething("aaaaaaaaaa");

            Console.ReadKey(false);

            //var proxyBuilder = new ProxyBuilder<IInterceptor>().BuildProxy(typeof(IFake));

            var mo = new Mock<IFake>();

            var mock = new Mock<person>();

            Console.WriteLine(mock.Object.Id + " aaa" + mock.Object.Name);
        }
    }

    /// <summary>

    /// 对应java的InvocationHandler接口

    /// 使用上应该是差不多的

    /// 注意点是，因为C#的Property的getset也是走这一个方法的

    /// 对于接口来说是全部代理，但是对于类只有虚方法代理

    /// </summary>

    public interface IInvocationHandler
    {
        object Invoke(object proxy, MethodInfo method, object[] args);
    }

    public interface myIInvocationHandler
    {
        object m(string args, string arg2);
    }

    public class II : IInvocationHandler
    {
        public object Invoke(object proxy, MethodInfo method, object[] args)
        {
            foreach (object o in args)
            {
                Console.WriteLine(o.ToString());
            }

            Console.WriteLine($"hahahaha");

            return args[1];

        }

    }

    /// <summary>

    /// 缓存类，可以无视

    /// </summary>

    class ProxyTypeInfo

    {

        public TypeBuilder TypeBuilder;

        public int Count;

        public MethodInfo[] MethodInfos;

    }

    /// <summary>

    /// 代理类

    /// </summary>

    public static class DynamicProxy

    {

        private static readonly string AssemblyName = "DynamicProxyAssembly";

        private static readonly string ModuleName = "DynamicProxyModule";

        private static readonly string TypeName = "DynamicProxy";

        /// <summary>

        /// 因为有些方法的指令是需要拆箱装箱的

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

        private static readonly Dictionary<Type, ProxyTypeInfo> ProxyDict = new Dictionary<Type, ProxyTypeInfo>();

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

            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(AssemblyName + type.Name),

        AssemblyBuilderAccess.Run);

            var moduleBuilder = assemblyBuilder.DefineDynamicModule(ModuleName + type.Name);

            return info.TypeBuilder = moduleBuilder.DefineType(TypeName + type.Name + info.Count,

              TypeAttributes.Public | TypeAttributes.Class, parent, interfaces);

        }

        private static void ProxyInit(Type type, TypeBuilder typeBuilder, MethodInfo[] methodInfos,

          MethodInfo handlerInvokeMethodInfo)

        {

            //定义两个字段

            var handlerFieldBuilder =

        typeBuilder.DefineField("_handler", typeof(IInvocationHandler), FieldAttributes.Private);

            var methodInfosFieldBuilder =

              typeBuilder.DefineField("_methodInfos", typeof(MethodInfo), FieldAttributes.Private);

            //定义构造函数

            var constructorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard,

        new[] { typeof(IInvocationHandler), typeof(MethodInfo[]) });

            var ilCtor = constructorBuilder.GetILGenerator();

            ilCtor.Emit(OpCodes.Ldarg_0);

            ilCtor.Emit(OpCodes.Call,

              typeof(object).GetConstructor(new Type[0]) ?? throw new Exception("不可能的错误:object.GetConstructor"));

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

                ilMethod.Emit(CanBox.Contains(methodInfo.ReturnType) ? OpCodes.Unbox_Any : OpCodes.Castclass,

                  methodInfo.ReturnType);

                ilMethod.Emit(OpCodes.Ret);

            }

        }

        /// <summary>

        /// 通过接口创建动态代理

        /// </summary>

        public static T CreateProxyByInterface<T>(IInvocationHandler handler, bool userCache = true)
        {
            return (T)CreateProxyByInterface(typeof(T), handler, userCache);
        }

        public static object CreateProxyByInterface(Type type, IInvocationHandler handler, bool userCache = true)
        {
            if (!userCache || !ProxyDict.TryGetValue(type, out var info))

            {

                var handlerInvokeMethodInfo = typeof(IInvocationHandler).GetMethod("Invoke") ??

                               throw new Exception("不可能的错误:handlerInvokeMethodInfo");

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

            return Activator.CreateInstance(info.TypeBuilder.CreateTypeInfo(), handler, info.MethodInfos) ??

                       throw new Exception("不同环境此处可能需要改写");

        }

        /// <summary>

        /// 通过类创建动态代理

        /// </summary>

        public static T CreateProxyByType<T>(IInvocationHandler handler, bool userCache = true)

        {

            return (T)CreateProxyByType(typeof(T), handler, userCache);

        }

        public static object CreateProxyByType(Type type, IInvocationHandler handler, bool userCache = true)

        {

            if (!userCache || !ProxyDict.TryGetValue(type, out var info))

            {

                var handlerInvokeMethodInfo = typeof(IInvocationHandler).GetMethod("Invoke") ??

                               throw new Exception("不可能的错误:handlerInvokeMethodInfo");

                var typeBuilder = CreateDynamicTypeBuilder(type, type, null);

                var methodInfos = type.GetMethods().Where(methodInfo => methodInfo.IsVirtual || methodInfo.IsAbstract)

                  .ToArray();

                ProxyInit(type, typeBuilder, methodInfos, handlerInvokeMethodInfo);

                info = ProxyDict[type];

                if (info.Count == 1)

                {

                    info.MethodInfos = methodInfos;

                }

            }

            return Activator.CreateInstance(info.TypeBuilder.CreateTypeInfo(), handler, info.MethodInfos) ??

                             throw new Exception("不同环境此处可能需要改写");

        }

    }



    public class person
    {
        public int Id { get; set; }

        public string Name { get; set; }
    }

    public interface IFake
    {
        [Description("系统状态码")]
        [Display(Name = "成功", Description = "请求成功!")]
        bool DoSomething(string actionname);

    }
    /// <summary>
    /// 拦截器
    /// </summary>
    public interface IInterceptor
    {
        object Intercept(Invocation invocation);
    }

    /// <summary>
    /// 代理生成类
    /// </summary>
    public class ProxyBuilder<T> where T : IInterceptor, new()
    {
        protected static AssemblyName DemoName = new AssemblyName("DynamicAssembly");
        /// <summary>
        /// 在内存中保存好存放代理类的动态程序集
        /// </summary>
        protected static AssemblyBuilder assyBuilder = AssemblyBuilder.DefineDynamicAssembly(DemoName, AssemblyBuilderAccess.Run);
        /// <summary>
        /// 在内存中保存好存放代理类的托管模块
        /// </summary>
        protected static ModuleBuilder modBuilder = assyBuilder.DefineDynamicModule(DemoName.Name);
        /// <summary>
        /// 动态构造targetType的代理类
        /// </summary>
        /// <returns></returns>
        public static Type BuildProxy(Type targetType, bool declaredOnly = false)
        {
            //创建一个类型 
            if (targetType.IsInterface)
            {
                throw new Exception("cannot create a proxy class for the interface");
            }
            Type TypeOfParent = targetType;
            Type[] TypeOfInterfaces = new Type[0];
            TypeBuilder typeBuilder = modBuilder.DefineType(targetType.Name + "Proxy" + Guid.NewGuid().ToString("N"), TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.BeforeFieldInit, TypeOfParent, TypeOfInterfaces);
            BindingFlags bindingFlags;
            if (declaredOnly)
            {
                bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;
            }
            else
            {
                bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            }
            MethodInfo[] targetMethods = targetType.GetMethods(bindingFlags);
            //遍历各个方法
            foreach (MethodInfo targetMethod in targetMethods)
            {
                //只挑出virtual的实例方法进行重写
                //只挑出打了RewriteAttribute标记的方法进行重写
                if (targetMethod.IsVirtual && !targetMethod.IsStatic && !targetMethod.IsFinal && !targetMethod.IsAssembly && targetMethod.GetCustomAttributes(true).Any(e => (e as RewriteAttribute != null)))
                {
                    Type[] paramType;
                    Type returnType;
                    ParameterInfo[] paramInfo;
                    Type delegateType = GetDelegateType(targetMethod, out paramType, out returnType, out paramInfo);
                    Type interceptorType = typeof(T);
                    MethodBuilder methodBuilder = typeBuilder.DefineMethod(targetMethod.Name, MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.HideBySig, returnType, paramType);
                    for (var i = 0; i < paramInfo.Length; i++)
                    {
                        ParameterBuilder paramBuilder = methodBuilder.DefineParameter(i + 1, paramInfo[i].Attributes, paramInfo[i].Name);
                        if (paramInfo[i].HasDefaultValue)
                        {
                            paramBuilder.SetConstant(paramInfo[i].DefaultValue);
                        }
                    }
                    ILGenerator il = methodBuilder.GetILGenerator();
                    //下面的il相当于
                    //public class parent
                    //{
                    //    public virtual string test(List<string> p1, int p2)
                    //    {
                    //        return "123";
                    //    }
                    //}
                    //public class child : parent
                    //{
                    //    public override string test(List<string> p1, int p2)
                    //    {
                    //        object[] Parameter = new object[2];
                    //        Parameter[0] = p1;
                    //        Parameter[1] = p2;
                    //        Func<List<string>, int, string> DelegateMethod = base.test;

                    //        Invocation invocation = new Invocation();
                    //        invocation.Parameter = Parameter;
                    //        invocation.DelegateMethod = DelegateMethod;
                    //        Interceptor interceptor = new Interceptor();
                    //        return (string)interceptor.Intercept(invocation);
                    //    }
                    //}

                    Label label1 = il.DefineLabel();

                    il.DeclareLocal(typeof(object[]));
                    il.DeclareLocal(delegateType);
                    il.DeclareLocal(typeof(Invocation));
                    il.DeclareLocal(interceptorType);
                    LocalBuilder re = null;
                    if (returnType != typeof(void))
                    {
                        re = il.DeclareLocal(returnType);
                    }
                    il.Emit(OpCodes.Ldc_I4, paramType.Length);
                    il.Emit(OpCodes.Newarr, typeof(object));
                    il.Emit(OpCodes.Stloc, 0);
                    for (var i = 0; i < paramType.Length; i++)
                    {
                        il.Emit(OpCodes.Ldloc, 0);
                        il.Emit(OpCodes.Ldc_I4, i);
                        il.Emit(OpCodes.Ldarg, i + 1);
                        if (paramType[i].IsValueType)
                        {
                            il.Emit(OpCodes.Box, paramType[i]);
                        }
                        il.Emit(OpCodes.Stelem_Ref);
                    }
                    il.Emit(OpCodes.Ldarg, 0);
                    il.Emit(OpCodes.Ldftn, targetMethod);
                    il.Emit(OpCodes.Newobj, delegateType.GetConstructors()[0]);
                    il.Emit(OpCodes.Stloc, 1);
                    il.Emit(OpCodes.Newobj, typeof(Invocation).GetConstructors(BindingFlags.Public | BindingFlags.Instance).First(e => e.GetParameters().Length == 0));
                    il.Emit(OpCodes.Stloc, 2);
                    il.Emit(OpCodes.Ldloc, 2);
                    il.Emit(OpCodes.Ldloc, 0);
                    il.Emit(OpCodes.Callvirt, typeof(Invocation).GetMethod("set_Parameter"));
                    il.Emit(OpCodes.Ldloc, 2);
                    il.Emit(OpCodes.Ldloc, 1);
                    il.Emit(OpCodes.Callvirt, typeof(Invocation).GetMethod("set_DelegateMethod"));
                    il.Emit(OpCodes.Newobj, interceptorType.GetConstructors(BindingFlags.Public | BindingFlags.Instance).First(e => e.GetParameters().Length == 0));
                    il.Emit(OpCodes.Stloc, 3);
                    il.Emit(OpCodes.Ldloc, 3);
                    il.Emit(OpCodes.Ldloc, 2);
                    il.Emit(OpCodes.Callvirt, interceptorType.GetMethod("Intercept"));
                    if (returnType != typeof(void))
                    {
                        il.Emit(OpCodes.Castclass, returnType);
                        il.Emit(OpCodes.Stloc_S, re);
                        il.Emit(OpCodes.Br_S, label1);
                        il.MarkLabel(label1);
                        il.Emit(OpCodes.Ldloc_S, re);
                    }
                    else
                    {
                        il.Emit(OpCodes.Pop);
                    }
                    il.Emit(OpCodes.Ret);
                }
            }
            //真正创建，并返回
            Type proxyType = typeBuilder.CreateType();
            return proxyType;
        }
        /// <summary>
        /// 通过MethodInfo获得其参数类型列表，返回类型，和委托类型
        /// </summary>
        /// <param name="targetMethod"></param>
        /// <param name="paramType"></param>
        /// <param name="returnType"></param>
        /// <returns></returns>
        public static Type GetDelegateType(MethodInfo targetMethod, out Type[] paramType, out Type returnType, out ParameterInfo[] paramInfo)
        {
            paramInfo = targetMethod.GetParameters();
            //paramType
            paramType = new Type[paramInfo.Length];
            for (int i = 0; i < paramInfo.Length; i++)
            {
                paramType[i] = paramInfo[i].ParameterType;
            }
            //returnType
            returnType = targetMethod.ReturnType;
            //delegateType
            Type delegateType;
            if (returnType == typeof(void))
            {
                switch (paramType.Length)
                {
                    case 0:
                        delegateType = typeof(Action);
                        break;
                    case 1:
                        delegateType = typeof(Action<>).MakeGenericType(paramType);
                        break;
                    case 2:
                        delegateType = typeof(Action<,>).MakeGenericType(paramType);
                        break;
                    case 3:
                        delegateType = typeof(Action<,,>).MakeGenericType(paramType);
                        break;
                    case 4:
                        delegateType = typeof(Action<,,,>).MakeGenericType(paramType);
                        break;
                    case 5:
                        delegateType = typeof(Action<,,,,>).MakeGenericType(paramType);
                        break;
                    case 6:
                        delegateType = typeof(Action<,,,,,>).MakeGenericType(paramType);
                        break;
                    case 7:
                        delegateType = typeof(Action<,,,,,,>).MakeGenericType(paramType);
                        break;
                    case 8:
                        delegateType = typeof(Action<,,,,,,,>).MakeGenericType(paramType);
                        break;
                    case 9:
                        delegateType = typeof(Action<,,,,,,,,>).MakeGenericType(paramType);
                        break;
                    case 10:
                        delegateType = typeof(Action<,,,,,,,,,>).MakeGenericType(paramType);
                        break;
                    case 11:
                        delegateType = typeof(Action<,,,,,,,,,,>).MakeGenericType(paramType);
                        break;
                    case 12:
                        delegateType = typeof(Action<,,,,,,,,,,,>).MakeGenericType(paramType);
                        break;
                    case 13:
                        delegateType = typeof(Action<,,,,,,,,,,,,>).MakeGenericType(paramType);
                        break;
                    case 14:
                        delegateType = typeof(Action<,,,,,,,,,,,,,>).MakeGenericType(paramType);
                        break;
                    case 15:
                        delegateType = typeof(Action<,,,,,,,,,,,,,,>).MakeGenericType(paramType);
                        break;
                    default:
                        delegateType = typeof(Action<,,,,,,,,,,,,,,,>).MakeGenericType(paramType);
                        break;
                }
            }
            else
            {
                Type[] arr = new Type[paramType.Length + 1];
                for (int i = 0; i < paramType.Length; i++)
                {
                    arr[i] = paramType[i];
                }
                arr[paramType.Length] = returnType;
                switch (paramType.Length)
                {
                    case 0:
                        delegateType = typeof(Func<>).MakeGenericType(arr);
                        break;
                    case 1:
                        delegateType = typeof(Func<,>).MakeGenericType(arr);
                        break;
                    case 2:
                        delegateType = typeof(Func<,,>).MakeGenericType(arr);
                        break;
                    case 3:
                        delegateType = typeof(Func<,,,>).MakeGenericType(arr);
                        break;
                    case 4:
                        delegateType = typeof(Func<,,,,>).MakeGenericType(arr);
                        break;
                    case 5:
                        delegateType = typeof(Func<,,,,,>).MakeGenericType(arr);
                        break;
                    case 6:
                        delegateType = typeof(Func<,,,,,,>).MakeGenericType(arr);
                        break;
                    case 7:
                        delegateType = typeof(Func<,,,,,,,>).MakeGenericType(arr);
                        break;
                    case 8:
                        delegateType = typeof(Func<,,,,,,,,>).MakeGenericType(arr);
                        break;
                    case 9:
                        delegateType = typeof(Func<,,,,,,,,,>).MakeGenericType(arr);
                        break;
                    case 10:
                        delegateType = typeof(Func<,,,,,,,,,,>).MakeGenericType(arr);
                        break;
                    case 11:
                        delegateType = typeof(Func<,,,,,,,,,,,>).MakeGenericType(arr);
                        break;
                    case 12:
                        delegateType = typeof(Func<,,,,,,,,,,,,>).MakeGenericType(arr);
                        break;
                    case 13:
                        delegateType = typeof(Func<,,,,,,,,,,,,,>).MakeGenericType(arr);
                        break;
                    case 14:
                        delegateType = typeof(Func<,,,,,,,,,,,,,,>).MakeGenericType(arr);
                        break;
                    case 15:
                        delegateType = typeof(Func<,,,,,,,,,,,,,,,>).MakeGenericType(arr);
                        break;
                    default:
                        delegateType = typeof(Func<,,,,,,,,,,,,,,,,>).MakeGenericType(arr);
                        break;
                }
            }
            return delegateType;

        }
    }

    /// <summary>
    /// 调用器
    /// </summary>
    public class Invocation
    {
        public object[] Parameter { get; set; }
        public Delegate DelegateMethod { get; set; }
        public object Proceed()
        {
            return this.DelegateMethod.DynamicInvoke(Parameter);
        }
    }

    /// <summary>
    /// 为要拦截的方法打上标记
    /// </summary>
    public class RewriteAttribute : System.Attribute
    {
    }

    public class DynamicProxy<T> : RealProxy
    {
        private readonly T _decorated;
        public DynamicProxy(T decorated)
          : base(typeof(T))
        {
            _decorated = decorated;
        }
        private void Log(string msg, object arg = null)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(msg, arg);
            Console.ResetColor();
        }
        public override IMessage Invoke(IMessage msg)
        {
            var methodCall = msg as IMethodCallMessage;
            var methodInfo = methodCall.MethodBase as MethodInfo;
            Log("In Dynamic Proxy - Before executing '{0}'",
              methodCall.MethodName);
            try
            {
                var result = methodInfo.Invoke(_decorated, methodCall.InArgs);
                Log("In Dynamic Proxy - After executing '{0}' ",
                  methodCall.MethodName);
                return new ReturnMessage(result, null, 0,
                  methodCall.LogicalCallContext, methodCall);
            }
            catch (Exception e)
            {
                Log(string.Format(
                  "In Dynamic Proxy- Exception {0} executing '{1}'", e),
                  methodCall.MethodName);
                return new ReturnMessage(e, methodCall);
            }
        }
    }
}
