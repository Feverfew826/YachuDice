using UnityEditor;
using UnityEditor.UI;

namespace YachuDice.Utilities.Editor
{
    [CustomEditor(typeof(SfxButton))]
    [CanEditMultipleObjects]
    public class SfxButtonEditor : ButtonEditor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_role"));
            serializedObject.ApplyModifiedProperties();
        }
    }
}