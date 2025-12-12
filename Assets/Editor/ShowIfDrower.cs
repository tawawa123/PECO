using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(ShowIfAttribute))]
public class ShowIfDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        ShowIfAttribute at = (ShowIfAttribute)attribute;
        SerializedProperty cond = property.serializedObject.FindProperty(at.condition);

        bool shouldShow = cond != null && cond.enumValueIndex == at.value;

        if (shouldShow)
        {
            EditorGUI.PropertyField(position, property, label, true);
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        ShowIfAttribute at = (ShowIfAttribute)attribute;
        SerializedProperty cond = property.serializedObject.FindProperty(at.condition);

        bool shouldShow = cond != null && cond.enumValueIndex == at.value;

        return shouldShow ? EditorGUI.GetPropertyHeight(property) : 0;
    }
}
