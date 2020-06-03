using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Editor.CruddyEditor
{
    public static class CruddyTypeExtensions
    {
        public static List<Type> FindAllDerivedTypes<T>()
        {
            return FindAllDerivedTypes<T>(Assembly.GetAssembly(typeof(T)));
        }

        public static List<Type> FindAllDerivedTypes<T>(Assembly assembly)
        {
            var derivedType = typeof(T);
            return assembly
                .GetTypes()
                .Where(t =>
                    t != derivedType &&
                    derivedType.IsAssignableFrom(t)
                ).ToList();
        }

        public static List<Type> FindAllDerivedTypesGeneric(Type genericType)
        {
            return System.Reflection.Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t => t.BaseType != null && t.BaseType.IsGenericType &&
                            t.BaseType.GetGenericTypeDefinition() == genericType)
                .ToList();
        }

        public static List<Type> FindAllDerivedGenericTypeArgs(Type genericType)
        {
            return FindAllDerivedTypesGeneric(genericType)
                .Select(t => t.GetGenericArguments().First())
                .ToList();
        }
    }

    static class CruddyAssetDatabaseExtensions
    {
        public static void CreateFolderRecursively(string path, [CanBeNull] string[] dirSeperators)
        {
            if (dirSeperators == null)
            {
                dirSeperators = new[] {Path.AltDirectorySeparatorChar.ToString()};
            }

            var uncreatedFolders = path.Split(dirSeperators, StringSplitOptions.RemoveEmptyEntries);
            string currentPath = "";
            for (int i = 0; i < uncreatedFolders.Length; i++)
            {
                string newPart = uncreatedFolders[i];
                string oldPart = currentPath;
                currentPath += currentPath == "" ? newPart : Path.AltDirectorySeparatorChar + newPart;
                if (!AssetDatabase.IsValidFolder(currentPath))
                {
                    AssetDatabase.CreateFolder(oldPart, newPart);
                }
            }

            //TODO: Alt method: Directory.CreateDirectory(Application.dataPath + path;
        }
    }

    public static class CruddyScriptableObjectExtensions
    {
        public static T Duplicate<T>(this T data) where T : ScriptableObject
        {
            T duplicate = ScriptableObject.CreateInstance<T>();
            if (data != null)
            {
                var props = typeof(T).GetFields();
                foreach (var prop in props)
                {
                    var current = prop.GetValue(data);
                    prop.SetValue(duplicate, current);
                }
            }
            return duplicate;
        }
    }


    public static class CruddyVisualElementExtensions
    {
        public static void DisplayHide(this VisualElement ve)
        {
            DisplayToggle(ve, false);
        }

        public static void DisplayShow(this VisualElement ve)
        {
            DisplayToggle(ve, true);
        }

        public static void DisplayToggle(this VisualElement ve, bool state)
        {
            var styleDisplay = ve.style.display;
            styleDisplay.value = state ? DisplayStyle.Flex : DisplayStyle.None;
            ve.style.display = styleDisplay;
        }
    }
}