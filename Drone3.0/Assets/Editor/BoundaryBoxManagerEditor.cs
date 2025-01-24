using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BoundaryBoxManager))]
public class BoundaryBoxManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        BoundaryBoxManager manager = (BoundaryBoxManager)target;

        // Draw the boundary mode dropdown
        manager.boundaryMode = (BoundaryBoxManager.BoundaryMode)EditorGUILayout.EnumPopup("Boundary Mode", manager.boundaryMode);

        // Draw fields based on the selected mode
        switch (manager.boundaryMode)
        {
            case BoundaryBoxManager.BoundaryMode.SimpleCube:
                EditorGUILayout.LabelField("Simple Cube Settings", EditorStyles.boldLabel);
                manager.sizeOfBoidBoundingBox = EditorGUILayout.FloatField("Cube Size", manager.sizeOfBoidBoundingBox);
                break;

            case BoundaryBoxManager.BoundaryMode.CustomArea:
                EditorGUILayout.LabelField("Custom Area Settings", EditorStyles.boldLabel);
                manager.customHeight = EditorGUILayout.FloatField("Height", manager.customHeight);
                for (int i = 0; i < manager.cornerPoints.Length; i++)
                {
                    manager.cornerPoints[i] = EditorGUILayout.Vector3Field($"Corner {i + 1}", manager.cornerPoints[i]);
                }
                break;

            case BoundaryBoxManager.BoundaryMode.AreaCreation:
                EditorGUILayout.LabelField("Area Creation Tools", EditorStyles.boldLabel);
                for (int i = 0; i < manager.cornerPoints.Length; i++)
                {
                    manager.cornerPoints[i] = EditorGUILayout.Vector3Field($"Corner {i + 1}", manager.cornerPoints[i]);
                }
                manager.customHeight = EditorGUILayout.FloatField("Height", manager.customHeight);

                if (GUILayout.Button("Save Custom Area"))
                {
                    manager.SaveCustomArea();
                }

                if (GUILayout.Button("Load Custom Area"))
                {
                    manager.LoadCustomArea();
                }
                break;
        }

        // Save changes
        if (GUI.changed)
        {
            EditorUtility.SetDirty(manager);
        }
    }
}
