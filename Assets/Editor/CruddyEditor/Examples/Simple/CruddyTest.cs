using System;
using System.Linq.Expressions;
using UnityEditor;
using UnityEngine;

namespace Editor.CruddyEditor.Samples
{
    public class TestCruddyEditor : CruddyEditor<SimpleSampleScriptableObject>
    {
        public override Func<SimpleSampleScriptableObject, Texture2D> IconProvider => itm =>
            itm?.mySprite != null ? itm.mySprite.texture : base.IconProvider.Invoke(itm);

        public override Expression<Func<SimpleSampleScriptableObject, string>> NameProvier => itm => itm.myName;

        public override Expression<Func<SimpleSampleScriptableObject, string>> DescriptionProvider =>
            itm => "Test Description";

        [MenuItem("Editor/Examples/CruddyExample")]
        public static void ShowWindow()
        {
            var window = GetWindowWithRect(typeof(TestCruddyEditor), new Rect(0, 0, 1280, 720));
            window.titleContent = new GUIContent("TryStuff Editor");
            window.Show();
        }
    }
}