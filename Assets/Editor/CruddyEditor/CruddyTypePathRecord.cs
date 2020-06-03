namespace Editor.CruddyEditor
{
    /// <summary>
    ///  Quick hacky way to avoid having to implement serializable dictionaries
    /// </summary>
    [System.Serializable]
    public class CruddyTypePathRecord
    {
        public string Type;

        public string[] Paths;
    }
}