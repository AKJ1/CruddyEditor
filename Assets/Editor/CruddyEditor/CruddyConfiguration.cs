#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Editor.CruddyEditor
{
    public class CruddyConfiguration : ScriptableObject
    {
        #region Static Data

        public static string EditorFriendlyName = "CruddyEditor"; //typeof(CruddyConfiguration).Namespace;

        private static List<Type> ConfiguredEditorTypes
        {
            get
            {
                var genType = typeof(CruddyEditor<>);
                return CruddyTypeExtensions.FindAllDerivedTypesGeneric(genType);
            }
        }

        private static List<Type> ConfiguredEditableTypes
        {
            get
            {
                var genType = typeof(CruddyEditor<>);
                return CruddyTypeExtensions.FindAllDerivedGenericTypeArgs(genType);
            }
        }

        private static string ConfigAssetName => "config.asset";

        private static string ConfigurationAssetDiskPath
        {
            get
            {
                return String.Join(Path.AltDirectorySeparatorChar.ToString(), new[]
                {
                    Application.dataPath,
                    // "Assets",
                    "Editor",
                    EditorFriendlyName,
                    ConfigAssetName
                });
            }
        }

        public static string ConfigurationAssetPath
        {
            get
            {
                return String.Join("/", new[]
                {
                    "Assets",
                    "Editor",
                    EditorFriendlyName,
                    ConfigAssetName
                });
            }
        }

        public static string EditorTemplateUXMLPath
        {
            get
            {
                return string.Join(Path.AltDirectorySeparatorChar.ToString(), new[]
                {
                    "Assets",
                    "Editor",
                    EditorFriendlyName,
                    "UITemplates",
                    "CrudUITemplate.uxml"
                });
            }
        }

        public static string DefaultInspectorTemplateUXMLPath
        {
            get
            {
                return string.Join(Path.AltDirectorySeparatorChar.ToString(), new[]
                {
                    "Assets",
                    "Editor",
                    EditorFriendlyName,
                    "UITemplates",
                    "DefaultInspectorTemplate.uxml"
                });
            }
        }

        public static string DefaultListViewPreviewPath
        {
            get
            {
                return string.Join(Path.AltDirectorySeparatorChar.ToString(), new[]
                {
                    "Assets",
                    "Editor",
                    EditorFriendlyName,
                    "UITemplates",
                    "DefaultListViewPreviewTemplate.uxml"
                });
            }
        }

        /// <summary>
        /// This part is setup as a singleton because Cruddy is deployed per-project,
        /// and one config per project makes sense.
        /// </summary>
        /// <returns></returns>
        public static CruddyConfiguration Current
        {
            get { return GetSingleton(); }
            set
            {
                //TODO: test if works
                var cruddyConfiguration = GetSingleton();
                cruddyConfiguration = value;
            }
        }

        private static CruddyConfiguration cachedConfig = null;

        #endregion

        #region User Configurable Data

        /// <summary>
        /// Use Generic Editor for everything.
        /// Debugging Parameter.
        /// </summary>
        public bool FallbackMode = false;

        /// <summary>
        /// Per-type path definitons. Where the .asset files will be stored on disk.
        /// Relative to root of project.
        /// </summary>
        [SerializeField] public List<TypePathRecord> EditableTypeConfiguredPaths
            = new List<TypePathRecord>();

        /// <summary>
        /// TODO: Implement
        /// Per-type path definitons. Where the .asset files will be stored on disk.
        /// Relative to root of project.
        /// </summary>
        [SerializeField] public List<CruddyVisualTemplateRecord> InspectorsBySource
            = new List<CruddyVisualTemplateRecord>();

        [SerializeField] public List<CruddyVisualTemplateRecord> ListViewPreviewTemplatesByType
            = new List<CruddyVisualTemplateRecord>();

        /// <summary>
        /// Name of context menu where editors will appear.
        /// </summary>
        public string EditorTab;

        #endregion

        private static CruddyConfiguration GetSingleton()
        {
            if (cachedConfig != null)
            {
                // Debug.Log("Cached Config Found!");
                return cachedConfig;
            }

            CruddyConfiguration existingConfig =
                AssetDatabase.LoadAssetAtPath<CruddyConfiguration>(ConfigurationAssetPath);
            // CruddyConfiguration existingConfig2 = (CruddyConfiguration) AssetDatabase.LoadAssetAtPath(ConfigurationAssetPath, typeof(CruddyConfiguration));

            if (existingConfig == null)
            {
                //Create Generic Configuration 
                Debug.Log("No Config Found!");
                var newConfig = InitEmptyConfig();
                newConfig.EditorTab = "Cruddy Editor";
                newConfig.FallbackMode = false;
                Debug.Log("Config Made!");
                cachedConfig = newConfig;
                return newConfig;
            }
            else
            {
                cachedConfig = existingConfig;
            }

            return cachedConfig;
        }

        private static CruddyConfiguration InitEmptyConfig()
        {
            Debug.Log("Creating Empty Config");
            var dirPath = ConfigurationAssetDiskPath;
            var ssIdx = dirPath.LastIndexOf(ConfigAssetName, StringComparison.Ordinal);
            dirPath = dirPath.Substring(0, ssIdx);

            var createPath = String.Join("/", ConfigurationAssetPath.Split('/').Take(ConfigurationAssetPath.Length-1));
            if (!AssetDatabase.IsValidFolder(createPath))
            {
                // Debug.Log("Creating Folder!");
                Directory.CreateDirectory(createPath);
                // CruddyAssetDatabaseExtensions
                //     .CreateFolderRecursively(dirPath, null);
            }

            var newConfig = CreateInstance<CruddyConfiguration>();
            AssetDatabase.CreateAsset(newConfig, ConfigurationAssetPath);
            AssetDatabase.SaveAssets();
            return newConfig;
        }

        public static VisualTreeAsset DefaultInspectorTemplate
        {
            get { return AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(DefaultInspectorTemplateUXMLPath); }
        }

        public static VisualTreeAsset DefaultListViewPreview
        {
            get { return AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(DefaultListViewPreviewPath); }
        }

        public VisualTreeAsset GetInspectorAssetForType(Type type)
        {
            
            if (!this.InspectorsBySource.Any(r => r.Type == type.Name))
            {
                CruddyVisualTemplateRecord newVtr = new CruddyVisualTemplateRecord();
                newVtr.Type = type.Name;
                newVtr.Template = DefaultInspectorTemplate;
                this.InspectorsBySource.Add(newVtr);
            }

            return InspectorsBySource.First(r => r.Type == type.Name).Template;
        }

        public VisualTreeAsset GetListViewPreviewTemplateForType(Type type)
        {
            if (!this.ListViewPreviewTemplatesByType.Any(r => r.Type == type.Name))
            {
                CruddyVisualTemplateRecord newVtr = new CruddyVisualTemplateRecord();
                newVtr.Type = type.Name;
                newVtr.Template = DefaultListViewPreview;
                this.ListViewPreviewTemplatesByType.Add(newVtr);
            }

            return ListViewPreviewTemplatesByType.First(r => r.Type == type.Name).Template;
        }

        public List<string> GetPathsForType(Type type)
        {
            if (EditableTypeConfiguredPaths == null)
            {
                EditableTypeConfiguredPaths = new List<TypePathRecord>();
            }

            if (!EditableTypeConfiguredPaths.Any(p => p.type == type.Name))
            {
                var newRecord = new TypePathRecord();
                newRecord.paths = new List<string>();
                newRecord.type = type.Name;
                EditableTypeConfiguredPaths.Add(newRecord);
                EditorUtility.SetDirty(this);
                AssetDatabase.SaveAssets();
            }

            return EditableTypeConfiguredPaths.First(p => p.type == type.Name).paths.ToList();
        }

        public void AddPathForType(Type type, string path)
        {
            if (EditableTypeConfiguredPaths == null)
            {
                EditableTypeConfiguredPaths = new List<TypePathRecord>();
            }

            CruddyAssetDatabaseExtensions.CreateFolderRecursively(path, new[] {"/"});
            if (!EditableTypeConfiguredPaths.Any(p => p.type == type.Name))
            {
                var newRecord = new TypePathRecord();
                newRecord.paths = new List<string>();
                newRecord.type = type.Name;
                EditableTypeConfiguredPaths.Add(newRecord);
            }

            if (!EditableTypeConfiguredPaths.Any(p => p.paths.Contains(path)))
            {
                EditableTypeConfiguredPaths.First(r => r.type == type.Name).paths.Add(path);
            }

            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }
    }
}
#endif