using System;
using System.Collections.Generic;
using System.Reflection;

namespace XuchFramework.Core.Utils
{
    public static class TypeHelper
    {
        public const string UnityAssemblyNameRuntime = "Assembly-CSharp";

        public const string UnityAssemblyNameEditor = "Assembly-CSharp-Editor";

        /// <summary>
        /// 从所有程序集中获取类型
        /// </summary>
        /// <param name="typeFullName">类型完全名称</param>
        /// <param name="assemblyName">程序集名称</param>
        /// <returns>获取到的类型</returns>
        public static Type GetType(string typeFullName, string assemblyName = null)
        {
            if (!string.IsNullOrEmpty(assemblyName))
            {
                var type = Type.GetType($"{typeFullName}, {assemblyName}");
                if (type != null)
                    return type;
            }

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var currentAssemblyName = assembly.GetName().Name;
                if (!string.IsNullOrEmpty(assemblyName) && currentAssemblyName != assemblyName)
                    continue;

                foreach (var t in assembly.GetTypes())
                {
                    if (t.FullName == typeFullName)
                        return t;
                }
            }

            return null;
        }

        /// <summary>
        /// 获取指定基类的所有子类名称
        /// </summary>
        /// <param name="baseType">基类类型</param>
        /// <returns>子类名称数组</returns>
        public static string[] GetDerivedTypeNames(Type baseType)
        {
            return GetDerivedTypeNamesInternal(baseType, AppDomain.CurrentDomain.GetAssemblies());
        }

        /// <summary>
        /// 获取指定基类的所有子类名称
        /// </summary>
        /// <param name="baseType">基类类型</param>
        /// <param name="assemblies">编译集名称数组</param>
        /// <returns>子类名称数组</returns>
        public static string[] GetDerivedTypeNames(Type baseType, params string[] assemblies)
        {
            return GetDerivedTypeNamesInternal(baseType, assemblies);
        }

        private static string[] GetDerivedTypeNamesInternal(Type baseType, string[] assemblyNames)
        {
            var assemblies = new List<Assembly>();
            foreach (string assemblyName in assemblyNames)
            {
                Assembly assembly;
                try
                {
                    assembly = Assembly.Load(assemblyName);
                }
                catch (Exception)
                {
                    Log.Error($"[TypeHelper] Failed to load assembly {assemblyName}.");
                    continue;
                }

                if (assembly == null)
                    continue;

                assemblies.Add(assembly);
            }

            return GetDerivedTypeNamesInternal(baseType, assemblies.ToArray());
        }

        private static string[] GetDerivedTypeNamesInternal(Type baseType, Assembly[] assemblies)
        {
            var result = new List<string>();
            foreach (Assembly assembly in assemblies)
            {
                if (assembly == null)
                    continue;

                foreach (Type t in assembly.GetTypes())
                {
                    if (t.IsClass && !t.IsAbstract && baseType.IsAssignableFrom(t))
                    {
                        result.Add(t.FullName);
                    }
                }
            }

            result.Sort();
            return result.ToArray();
        }
    }
}