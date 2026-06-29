#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(silinecek))]
public class silinecek2 : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        silinecek manager = (silinecek)target;

        GUILayout.Space(15);
        GUILayout.Label("Editor Step Preview", EditorStyles.boldLabel);

        if (manager.steps == null || manager.steps.Length == 0)
        {
            EditorGUILayout.HelpBox("Önce Steps array içine step objelerini ekle.", MessageType.Warning);
            return;
        }

        for (int i = 0; i < manager.steps.Length; i++)
        {
            string stepName = manager.steps[i] != null ? manager.steps[i].name : "Boş Step";

            if (GUILayout.Button("Show Step " + i + " - " + stepName))
            {
                Undo.RecordObjects(manager.steps, "Show Step " + i);

                manager.ShowStep(i);

                foreach (GameObject step in manager.steps)
                {
                    if (step != null)
                        EditorUtility.SetDirty(step);
                }

                EditorUtility.SetDirty(manager);
            }
        }

        GUILayout.Space(10);

        if (GUILayout.Button("Show All Steps"))
        {
            Undo.RecordObjects(manager.steps, "Show All Steps");

            manager.ShowAllSteps();

            foreach (GameObject step in manager.steps)
            {
                if (step != null)
                    EditorUtility.SetDirty(step);
            }

            EditorUtility.SetDirty(manager);
        }

        if (GUILayout.Button("Hide All Steps"))
        {
            Undo.RecordObjects(manager.steps, "Hide All Steps");

            manager.HideAllSteps();

            foreach (GameObject step in manager.steps)
            {
                if (step != null)
                    EditorUtility.SetDirty(step);
            }

            EditorUtility.SetDirty(manager);
        }
    }
}
#endif