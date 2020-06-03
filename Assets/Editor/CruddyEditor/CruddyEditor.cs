#define CRUDDY_ENABLE
#if CRUDDY_ENABLE
#if UNITY_EDITOR
using UnityEditor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using JetBrains.Annotations;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

namespace Editor.CruddyEditor
{


    /// <summary>
    /// TODO : Copypasta the actual CRUD operations over // Done
    /// TODO : Write Update Logic for the Front End // Done
    /// TODO : Make a way to create a new asset when there are none // Done
    /// TODO : Make a way to add a folder when there are none // Done but jank
    /// TODO : ( Plus Button and Full Screen "Create Asset" Question) // Done but jank
    /// TODO : Add user-controllable preview height override and custom preview templates. //Done, but needs to be assigned manually
    /// TODO : Debug // Done
    /// TODO : Seperate paths for directory and asset name. // Done
    /// TODO : Fix erratic path field, allow defining extra paths from "Browsing Path" state. // Done
    /// TODO : Give a straightforward way to create new item that isn't copying. // Done
    /// TODO : Implement Search Field. // Done
    /// TODO : Add user-contnrollable ListView binding to be able to use different ListView templates. // Done
    /// TODO : Run full import test in a new project. // Done
    /// TODO : Split into smaller files . // Done
    /// TODO : Refactor & Retest. // Done 
    /// TODO : Make a tutorial document. 
    /// TODO : Package and Release a unitypackage.
    /// MAYBE TODO : Add Drag & Drop Functionality.
    /// MAYBE FUTURE TODO : Add Import Wizard
    /// MAYBE FUTURE TODO : Add an abstraction layer over this editor to automatically create editors for every ScriptableObject-type asset.
    /// </summary>
    /// <typeparam name="T">The ScriptableObject you want to make and Editor for</typeparam>
    public abstract class CruddyEditor<T> : EditorWindow where T : ScriptableObject
    {
        public static class UXMLNames
        {
            public static string PathFieldName = "PathField";
            public static string AssetPathFieldName = "AssetPathField";
            public static string AssetPathContainer = "AssetPathContainer";

            public static string InspectorContainerName = "InspectorContainer";
            public static string InspectorIMGUIWindow = "InspectorIMGUI";
            public static string ListViewName = "ListView";
            public static string TemplateIcon = "Icon";
            public static string TemplateNameLabel = "Name";
            public static string TemplateNameDescription = "Description";
            public static string ListViewTemplateName = "PreviewTemplate";
            public static string SaveButton = "SaveButton";
            public static string DeleteButton = "DeleteButton";
            public static string PathDropdownSelect = "PathDropdownSelect";
            public static string WarningIcon = "WarningIcon";
            public static string CreateFirstEditor = "NoEntriesContainer";
            public static string CreateNewEditor = "NewEntryContainer";
            public static string CreateFirstButton = "CreateFirstButton";
            public static string CreateNewButton = "CreateNewButton";

            public static string EmptyDirectoryCreateNewPathField = "CreateFirstPath";
            public static string CreateNewPathField = "CreateNewPath";
            public static string SearchField = "SearchField";
            public static string TemplatePathLabel = "Path";
        }

        public static class CruddyResources
        {
            public static string DefaultImagePath = "Assets/Editor/CruddyEditor/Visuals/cruddy_4u6ka.png";
        }

        public List<List<T>> Values => ValuesByPath.Values.ToList();

        public List<string> Paths => ValuesByPath.Keys.ToList();

        public Dictionary<string, List<T>> ValuesByPath = new Dictionary<string, List<T>>();

        public Dictionary<string, List<SerializedObject>> SerializedObjectsByPath =
            new Dictionary<string, List<SerializedObject>>();

        public Dictionary<string, Dictionary<T, string>> AssetsByPath = new Dictionary<string, Dictionary<T, string>>();

        public string ActivePath;


        /// <summary>
        /// Override this method to extend or re-implement a custom ListView template.
        /// </summary>
        /// <param name="element">The root visual template element. This is called for every instancce spawned by the ListView.</param>
        /// <param name="scriptableObject">The data object that should be assigned to the visual element.</param>
        /// <param name="serializedObject">A SerializedObject Wrapper of the ScriptableObject. Useful for directly binding data.</param>
        public virtual void BindListViewItem(VisualElement element, T scriptableObject,
            SerializedObject serializedObject)
        {
            string assetName = AssetDatabase.GetAssetPath(scriptableObject).Split('/').Last().Replace(".asset", "");

            var icon = element.Q<VisualElement>(UXMLNames.TemplateIcon);
            var bgImgValue = icon.style.backgroundImage;
            var background = bgImgValue.value;
            background.texture = IconProviderInternal((serializedObject?.targetObject as T));
            icon.style.backgroundImage = background;

            var templateLabel = element.Q<Label>(UXMLNames.TemplateNameLabel);
            templateLabel.bindingPath = ((MemberExpression) NameProvier.Body).Member.Name;
            templateLabel.Bind(serializedObject);

            Label nameLabel = element.Q<Label>(UXMLNames.TemplatePathLabel);
            nameLabel.text = assetName;

            var descriptionLabel = element.Q<Label>(UXMLNames.TemplateNameDescription);
            try
            {
                descriptionLabel.bindingPath = ((MemberExpression) DescriptionProvider.Body).Member.Name;
            }
            catch
            {
                //not pointing to a property, return string 
                descriptionLabel.text = DescriptionProvider.Compile().Invoke((serializedObject?.targetObject as T));
            }

            descriptionLabel.Bind(serializedObject);
        }

        public List<T> ActiveValues
        {
            get
            {
                if (ValuesByPath != null && !string.IsNullOrEmpty(ActivePath))
                {
                    return ValuesByPath[ActivePath];
                }

                return null;
            }
        }

        public List<SerializedObject> ActiveSerializedObjects
        {
            get
            {
                if (SerializedObjectsByPath != null && !string.IsNullOrEmpty(ActivePath))
                {
                    return SerializedObjectsByPath[ActivePath];
                }

                return null;
            }
        }

        public List<SerializedObject> FilteredSerializedObjects
        {
            get
            {
                if (ActiveSerializedObjects != null && !string.IsNullOrWhiteSpace(searchString))
                {
                    return ActiveSerializedObjects.Where(p =>
                    {
                        var data = (p.targetObject as T);
                        string name = NameProvier.Compile().Invoke(data);
                        string assetName = AssetDatabase.GetAssetPath(data);
                        assetName = assetName.Split('/').Last().Replace(".asset", "");
                        if (name.ToLower().Contains(searchString.ToLower()) ||
                            assetName.ToLower().Contains(searchString.ToLower()))
                        {
                            return true;
                        }

                        return false;
                    }).ToList();
                }

                return null;
            }
        }

        private T currentlyEditedValue;

        private T currentlyEditedValueNewState;

        private string CurrentlyEditedAssetPath => AssetDatabase.GetAssetPath(currentlyEditedValue);

        private string CurrentlyEditedAsssetFileName => CurrentlyEditedAssetPath
            .Split('/')
            .Last()
            .Replace(".asset", "");


        #region UserProperties

        public virtual Func<T, Texture2D> IconProvider =>
            t => AssetDatabase.LoadAssetAtPath<Texture2D>(CruddyResources.DefaultImagePath);

        public abstract Expression<Func<T, string>> NameProvier { get; }

        public abstract Expression<Func<T, string>> DescriptionProvider { get; }

        #endregion

        private List<string> ConfiguredPaths => CruddyConfiguration.Current.GetPathsForType(typeof(T));

        private CruddyAction state = CruddyAction.Uninitialized;

        // ReSharper disable once InconsistentNaming
        private VisualElement root => rootVisualElement;

        private bool editorIsPendingDelete => (EditorApplication.timeSinceStartup - editorTimeAtPreviousDeleteClick) <
                                              editorDeleteConfimTime;

        private float editorDeleteConfimTime = 3.25f;

        private double editorTimeAtPreviousDeleteClick;

        [ExecuteInEditMode]
        public void Update()
        {
            // Debug.Log(state);
            UpdatePath();
            UpdateEditorState();
            UpdateEditorView();
            UpdateCreateNewPath();
            UpdateSearch();
        }

        private string searchString;

        public void UpdateSearch()
        {
            ToolbarSearchField searchField = root.Q<ToolbarSearchField>(UXMLNames.SearchField);
            string newVal = searchField.value;
            if (newVal != searchString)
            {
                searchString = searchField.value;
                AssignData();
                ResetPathField();
            }

            searchString = searchField.value;
        }

        public void UpdateDragAndDrop()
        {
            //TODO: Read & Implement drag & drop
            //TODO: Dragging a listview item should grab the asset and
            //TODO: Allow you to assign it to a field that accepts that type
        }

        public void OnEnable()
        {
            LoadEditor();
            LoadInspector();
            Initialize();
        }

        public void Initialize()
        {
            BindEditorButtons();
            BindListView();
            BindInspector();
            BindCreateNewDialog();
            Refresh();
            AssignData();
        }

        public void AssignData()
        {
            ListView lv = root.Q<ListView>(UXMLNames.ListViewName);
            if (FilteredSerializedObjects != null)
            {
                lv.itemsSource = FilteredSerializedObjects;
            }
            else if (ActiveSerializedObjects != null)
            {
                lv.itemsSource = ActiveSerializedObjects;
            }

            AssignDropdownPaths();
        }

        private void AssignPath(string path)
        {
            this.ActivePath = path;
            ResetEditor();
        }

        private void LoadEditor()
        {
            var editorPath = CruddyConfiguration.EditorTemplateUXMLPath;
            var edtiorTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(editorPath);
            edtiorTemplate.CloneTree(root);
        }

        private void LoadInspector()
        {
            var inspectorContainer = root.Q(UXMLNames.InspectorContainerName);
            VisualElement newVe = new VisualElement();
            var inspectorTemplate = CruddyConfiguration.Current.GetInspectorAssetForType(typeof(T));
            inspectorTemplate.CloneTree(newVe);
            inspectorContainer.Add(newVe);
        }

        private void BindEditorButtons()
        {
            var btnSave = root.Q<Button>(UXMLNames.SaveButton);
            btnSave.clickable.clicked += UpdateValue;

            var btnDelete = root.Q<Button>(UXMLNames.DeleteButton);
            btnDelete.clickable.clicked += DeleteValue;
        }

        public void UpdateValue()
        {
            if (state == CruddyAction.DefiningNewPathForTypeInitial
                || state == CruddyAction.DefiningNewPathForType)
            {
                CruddyConfiguration.Current.AddPathForType(typeof(T), this.targetPath);
                AssignPath(this.targetPath);
                // ResetEditor();
                return;
            }
            else if (state == CruddyAction.CloningAsset)
            {
                // Debug.Log(fullTargetPath);
                AssetDatabase.CreateAsset(currentlyEditedValueNewState, fullTargetPath);
                ResetEditor();
                return;
            }

            var storedPath = CurrentlyEditedAssetPath;
            // Debug.Log(storedPath);
            AssetDatabase.DeleteAsset(storedPath);
            AssetDatabase.CreateAsset(currentlyEditedValueNewState, storedPath);
            ResetEditor();
            SelectEditedItem(AssetDatabase.LoadAssetAtPath<T>(storedPath));
        }

        public void CreateEmpty(string name)
        {
            var newInstance = ScriptableObject.CreateInstance<T>();
            // TextField editorPathField;
            string fullPath = ActivePath + "/" + name + ".asset";
            if (AssetDatabase.LoadAllAssetsAtPath(fullPath).Length == 0)
            {
                CruddyAssetDatabaseExtensions.CreateFolderRecursively(ActivePath, new [] {"/"});
                AssetDatabase.CreateAsset(newInstance, fullPath);
                ResetEditor();
            }
        }

        public void DeleteValue()
        {
            if (!editorIsPendingDelete)
            {
                editorTimeAtPreviousDeleteClick = EditorApplication.timeSinceStartup;
            }
            else
            {
                AssetDatabase.DeleteAsset(CurrentlyEditedAssetPath);
                editorTimeAtPreviousDeleteClick = 0;
                ResetEditor();
            }
        }

        private void ResetEditor()
        {
            currentlyEditedValue = null;
            currentlyEditedValueNewState = null;
            Refresh();
            AssignData();
            ResetPathField();
            ResetCreateNewPath();
        }

        private void SelectEditedItem(T item)
        {
            ResetPathField();
            ResetCreateNewPath();
            currentlyEditedValue = item;
            currentlyEditedValueNewState = currentlyEditedValue.Duplicate();
        }

        private void AssignDropdownPaths()
        {
            var pdd = root.Q<ToolbarMenu>(UXMLNames.PathDropdownSelect);
            pdd.variant = ToolbarMenu.Variant.Popup;
            // pdd.Clear();
            pdd.menu.MenuItems().Clear();
            if (Paths.Count > 0)
            {
                pdd.text = string.IsNullOrWhiteSpace(ActivePath) ? "SELECT!" : GetShortenedPath(ActivePath);
            }
            else
            {
                pdd.text = "Define New";
            }

            foreach (var path in Paths)
            {
                // var prettyPath = path.Replace("/", "->");
                string friendlyName = path.Replace("/", "->");
                // pdd.text = "SELECT";
                string chosenName = GetShortenedPath(path);

                pdd.menu.AppendAction(friendlyName, (d) =>
                {
                    AssignPath(path);
                    pdd.text = chosenName;
                });
            }
        }

        private string GetShortenedPath(string path)
        {
            string[] splitString = path.Split('/');
            string shortenedPath = "";
            for (int i = 0; i < Mathf.Min(splitString.Length - 1, 2); i++)
            {
                shortenedPath += ". . /";
            }

            shortenedPath += splitString.Last();
            return shortenedPath;
        }

        private Texture2D IconProviderInternal(T item)
        {
            try
            {
                return IconProvider.Invoke(item);
            }
            catch
            {
                // ignored
            }

            return Texture2D.whiteTexture;
        }

        private void BindListView()
        {
            ListView lv = root.Q<ListView>(UXMLNames.ListViewName);
            lv.bindItem = (element, i) =>
            {
                // Debug.Log("binding item");
                var serializedObject = lv.itemsSource[i] as SerializedObject;
                var scriptableObject = (serializedObject?.targetObject as T);
                BindListViewItem(element, scriptableObject, serializedObject);
            };

            var treeAsset = CruddyConfiguration.Current.GetListViewPreviewTemplateForType(typeof(T));
            int detectedHeight = 60;
            lv.makeItem = () =>
            {
                // Debug.Log("making item");
                var newVe = new VisualElement();
                treeAsset.CloneTree(newVe);
                detectedHeight = (int) newVe.layout.height;
                return newVe.Q(UXMLNames.ListViewTemplateName);
            };

            lv.onSelectionChanged += o =>
            {
                var item = (lv.selectedItem as SerializedObject)?.targetObject as T;
                SelectEditedItem(item);
            };
            lv.itemHeight = detectedHeight;
        }

        /// <summary>
        /// Extend or re-implement the functionality by overriding this.
        /// This is exposed in case you want to make your own editor template.
        /// For a specific data type.
        /// </summary>
        public virtual void BindInspector()
        {
            IMGUIContainer container = root.Q<IMGUIContainer>(UXMLNames.InspectorIMGUIWindow);
            container.onGUIHandler += () =>
            {
                if (currentlyEditedValueNewState != null)
                {
                    var editor = UnityEditor.Editor.CreateEditor(currentlyEditedValueNewState);
                    editor.DrawHeader();
                    editor.DrawDefaultInspector();
                }
            };
        }

        string GetFullPath(string str) => ActivePath + "/" + str + ".asset";

        public void ResetCreateNewPath()
        {
            string defaultName = "NewItem";
            TextField txtFld = root.Q<TextField>(UXMLNames.CreateNewPathField);
            txtFld.value = defaultName;
            string additive = "";
            string basePath = txtFld.value;
            string tarPath = GetFullPath(basePath + additive);
            int i = 0;
            do
            {
                i++;
                additive = $"({i})";
                tarPath = GetFullPath(basePath + additive);
            } while (AssetDatabase.LoadAllAssetsAtPath(tarPath).Length > 0 && !string.IsNullOrWhiteSpace(ActivePath));

            txtFld.value = txtFld.value + additive;
        }

        public void UpdateCreateNewPath()
        {
            if (state != CruddyAction.BrowsingPath)
            {
                return;
            }

            TextField txtFld = root.Q<TextField>(UXMLNames.CreateNewPathField);
            Button createBtn = root.Q<Button>(UXMLNames.CreateNewButton);
            string tarPath = GetFullPath(txtFld.value);
            if (AssetDatabase.LoadAllAssetsAtPath(tarPath).Length > 0)
            {
                var styleColor = createBtn.style.backgroundColor;
                styleColor.value = new Color(0.65f, 0.20f, 0.20f);
                createBtn.style.backgroundColor = styleColor;
                createBtn.text = " Already Exists ";
                var styleUnityFontStyleAndWeight = txtFld.style.unityFontStyleAndWeight;
                styleUnityFontStyleAndWeight.value = FontStyle.Bold;
                txtFld.style.unityFontStyleAndWeight = styleUnityFontStyleAndWeight;
            }
            else
            {
                var styleColor = createBtn.style.backgroundColor;
                styleColor.value = new Color(0.35f, 0.35f, 0.35f);
                createBtn.style.backgroundColor = styleColor;
                createBtn.text = " Create New ";
                var styleUnityFontStyleAndWeight = txtFld.style.unityFontStyleAndWeight;
                styleUnityFontStyleAndWeight.value = FontStyle.Normal;
                txtFld.style.unityFontStyleAndWeight = styleUnityFontStyleAndWeight;
            }
        }

        private void BindCreateNewDialog()
        {
            Button createFirstButton = root.Q<Button>(UXMLNames.CreateFirstButton);
            TextField createFirstPath = root.Q<TextField>(UXMLNames.EmptyDirectoryCreateNewPathField);
            createFirstButton.clickable.clicked += () => { CreateEmpty(createFirstPath.value); };

            Button createNewButton = root.Q<Button>(UXMLNames.CreateNewButton);
            TextField createNewPath = root.Q<TextField>(UXMLNames.CreateNewPathField);
            createNewButton.clickable.clicked += () => { CreateEmpty(createNewPath.value); };
        }

        private void UpdateEditorView()
        {
            IMGUIContainer inspector = root.Q<IMGUIContainer>(UXMLNames.InspectorIMGUIWindow);
            Button saveButton = root.Q<Button>(UXMLNames.SaveButton);
            Button deleteButton = root.Q<Button>(UXMLNames.DeleteButton);
            VisualElement warningIcon = root.Q(UXMLNames.WarningIcon);
            VisualElement createFirstEditor = root.Q(UXMLNames.CreateFirstEditor);
            VisualElement createNewEditor = root.Q(UXMLNames.CreateNewEditor);
            VisualElement assetPathContainer = root.Q(UXMLNames.AssetPathContainer);

            assetPathContainer?.DisplayShow();
            saveButton?.DisplayHide(); // saveButton.text = "Save";
            deleteButton?.DisplayHide();
            // deleteButton.text = "Delete";
            inspector?.DisplayHide();
            warningIcon?.DisplayHide();
            createFirstEditor?.DisplayHide();
            createNewEditor?.DisplayHide();

            if (deleteButton != null) deleteButton.text = "Delete";
            // Debug.Log(state);
            switch (state)
            {
                case CruddyAction.Uninitialized:
                    saveButton.text = "Init in Folder";
                    assetPathContainer?.DisplayHide();
                    saveButton.DisplayShow();
                    deleteButton.DisplayHide();
                    break;
                case CruddyAction.BrowsingPath:
                    assetPathContainer?.DisplayHide();
                    saveButton.DisplayHide();
                    deleteButton.DisplayHide();
                    if (ActiveValues == null || ActiveValues?.Count == 0)
                    {
                        createFirstEditor?.DisplayShow();
                    }
                    else
                    {
                        createNewEditor.DisplayShow();
                    }

                    break;
                case CruddyAction.DefiningNewPathForType:
                    assetPathContainer?.DisplayHide();
                    saveButton.DisplayShow();
                    if (saveButton != null) saveButton.text = "Init in Folder";
                    break;
                case CruddyAction.CloningAsset:
                    inspector.DisplayShow();
                    saveButton.DisplayShow();
                    if (saveButton != null) saveButton.text = "Save as New";
                    deleteButton.DisplayShow();
                    break;
                case CruddyAction.PendingDelete:
                    inspector.DisplayShow();
                    deleteButton.DisplayShow();
                    warningIcon.DisplayShow();
                    if (deleteButton != null) deleteButton.text = "Are you Sure?";
                    break;
                case CruddyAction.BrowsingAsset:
                    inspector.DisplayShow();
                    deleteButton.DisplayShow();
                    saveButton.DisplayHide();
                    break;
                case CruddyAction.PendingUpdate:
                    inspector.DisplayShow();
                    deleteButton.DisplayShow();
                    saveButton.DisplayShow();
                    warningIcon.DisplayShow();
                    if (saveButton != null) saveButton.text = "Save\n(Overwrite)";
                    break;
                case CruddyAction.DefiningNewPathForTypeInitial:
                    assetPathContainer?.DisplayHide();
                    if (saveButton != null)
                    {
                        saveButton.text = "Init in Folder";
                        saveButton.DisplayShow();
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private string targetPath;
        private string assetTargetFileName;

        private string userModdedPath;
        private string assetUserModdedFileName;

        private string fullTargetPath => targetPath + assetTargetFileName + ".asset";

        private void UpdatePath()
        {
            TextField pathField = root.Q<TextField>(UXMLNames.PathFieldName);
            TextField assetPathField = root.Q<TextField>(UXMLNames.AssetPathFieldName);
            var newText = pathField.text;
            if (newText != targetPath)
            {
                userModdedPath = pathField.text;
            }

            assetUserModdedFileName = assetPathField.text;

            if (state == CruddyAction.DefiningNewPathForTypeInitial
                || state == CruddyAction.BrowsingPath
                || state == CruddyAction.DefiningNewPathForType
                || state == CruddyAction.Uninitialized)
            {
                if (string.IsNullOrWhiteSpace(userModdedPath) || userModdedPath == "")
                {
                    if (state == CruddyAction.BrowsingPath || state == CruddyAction.DefiningNewPathForType)
                    {
                        targetPath = ActivePath;
                    }
                    // if (state == CruddyAction.DefiningNewPathForType)
                    // {
                    //     targetPath = ActivePath;
                    // }
                    else
                    {
                        var objSelection = Selection.activeObject;
                        if (objSelection == null)
                        {
                            targetPath = "Assets/Resources/" + typeof(T).Name + "s";
                        }
                        else
                        {
                            string path = AssetDatabase.GetAssetPath(objSelection);
                            int dirIdx = path.Length;
                            if (path.Contains("."))
                            {
                                dirIdx = path.LastIndexOf("/", StringComparison.Ordinal);
                            }

                            string dirPath = path.Substring(0, dirIdx);
                            targetPath = dirPath;
                        }
                    }
                }
                else
                {
                    targetPath = userModdedPath;
                }
            }
            else
            {
                var newTargetPath = ActivePath + "/";
                targetPath = newTargetPath;
                if (string.IsNullOrWhiteSpace(assetUserModdedFileName)
                    || assetUserModdedFileName == "")
                {
                    // Debug.Log(CurrentlyEditedAsssetFileName);
                    assetTargetFileName = CurrentlyEditedAsssetFileName;
                }
                else
                {
                    assetTargetFileName = assetUserModdedFileName;
                }
            }

            pathField.value = targetPath;
            assetPathField.value = assetTargetFileName;
        }

        private void ResetPathField()
        {
            TextField pathField = root.Q<TextField>(UXMLNames.PathFieldName);
            pathField.value = "";

            TextField assetPathField = root.Q<TextField>(UXMLNames.AssetPathFieldName);
            assetPathField.value = "";
        }

        private void UpdateEditorState()
        {
            if (!ConfiguredPaths.Any() || string.IsNullOrEmpty(ActivePath))
            {
                state = CruddyAction.DefiningNewPathForTypeInitial;
                return;
            }

            if (currentlyEditedValue == null)
            {
                if (NewPathDetected())
                {
                    state = CruddyAction.DefiningNewPathForType;
                }
                else
                {
                    state = CruddyAction.BrowsingPath;
                }

                return;
            }

            if (currentlyEditedValue != null)
            {
                if (editorIsPendingDelete)
                {
                    state = CruddyAction.PendingDelete;
                    return;
                }

                if (NewAssetPathDetected())
                {
                    state = CruddyAction.CloningAsset;
                    return;
                }

                bool changed = AreDifferent(currentlyEditedValue, currentlyEditedValueNewState);
                state = changed ? CruddyAction.PendingUpdate : CruddyAction.BrowsingAsset;
            }
        }

        private bool NewAssetPathDetected()
        {
            // Debug.Log("current: " +  CurrentlyEditedAsssetFileName);
            // Debug.Log("target: " +  assetTargetFileName);

            bool val = (CurrentlyEditedAsssetFileName != assetTargetFileName);
            return val;
        }

        private bool NewPathDetected()
        {
            bool? val = !this.Paths?.Contains(targetPath);
            if (val != null) return val.GetValueOrDefault();
            return false;
        }

        private bool AreDifferent(T itm1, T itm2)
        {
            bool isDifferent = false;
            var props = typeof(T).GetFields();
            foreach (var prop in props)
            {
                var currentVal = prop.GetValue(itm1);
                var newVal = prop.GetValue(itm2);
                // Debug.Log(prop.Name);
                if (currentVal != null && newVal != null)
                {
                    if (!currentVal.Equals(newVal)) isDifferent = true;
                }
                else
                {
                    isDifferent = !(currentVal == null && newVal == null);
                }

                if (isDifferent) break;
            }

            return isDifferent;
        }

        private void Refresh()
        {
            this.AssetsByPath.Clear();
            this.ValuesByPath.Clear();
            this.SerializedObjectsByPath.Clear();
            foreach (var path in ConfiguredPaths)
            {
                // Debug.Log(path);
                List<T> newList = new List<T>();
                List<SerializedObject> soList = new List<SerializedObject>();
                Dictionary<T, string> newDict = new Dictionary<T, string>();
                var assetGUIDs = AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] {path});

                foreach (var guid in assetGUIDs)
                {
                    var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    // Debug.Log("adding asset at path " + assetPath);
                    var asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
                    newList.Add(asset);
                    soList.Add(new SerializedObject(asset));
                    newDict.Add(asset, assetPath);
                }

                this.AssetsByPath.Add(path, newDict);
                this.ValuesByPath.Add(path, newList);
                this.SerializedObjectsByPath.Add(path, soList);
            }
        }
    }
}
public enum CruddyAction
{
    Uninitialized,
    BrowsingPath,
    CloningAsset,
    PendingDelete,
    BrowsingAsset,
    PendingUpdate,
    DefiningNewPathForTypeInitial,
    DefiningNewPathForType
}

#endif
#endif