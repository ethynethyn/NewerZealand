using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class RenameSelected : EditorWindow
{
    // Filled from the current Editor selection and/or drag-and-drop.
    // Kept as a serialized field so the built-in list UI still lets you
    // reorder / remove / hand-add entries after the fact.
    public List<GameObject> objects = new List<GameObject>();

    private string _gameObjectPrefix;
    private int _startIndex;

    private bool _organize;
    private bool _createParent;
    private string _newParentName;

    private Vector2 _scroll;
    private SerializedObject _serializedObject;

    [MenuItem("GameObject/Rename Selected")]
    public static void ShowWindow()
    {
        RenameSelected window = GetWindow<RenameSelected>();
        window.titleContent = new GUIContent("Rename Selected");
        window.minSize = new Vector2(260, 300);
        window.AddSelection(); // pull in whatever is selected when the window opens
    }

    private void OnEnable()
    {
        if (objects == null)
            objects = new List<GameObject>();

        _serializedObject = new SerializedObject(this);
    }

    private void OnGUI()
    {
        _gameObjectPrefix = EditorGUILayout.TextField("Prefix", _gameObjectPrefix);
        _startIndex = EditorGUILayout.IntField("Start Index", _startIndex);

        _organize = EditorGUILayout.Toggle(
            new GUIContent("Organize In Hierarchy",
                "After renaming, unparent the objects (including ones nested under other objects) and reorder them to match the naming order."),
            _organize);

        _createParent = EditorGUILayout.Toggle(
            new GUIContent("Create Parent",
                "Create a new empty GameObject and nest all the objects under it, in order."),
            _createParent);

        using (new EditorGUI.DisabledScope(!_createParent))
        {
            _newParentName = EditorGUILayout.TextField("Parent Name", _newParentName);
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Add Selected"))
                AddSelection();

            using (new EditorGUI.DisabledScope(objects.Count == 0))
            {
                if (GUILayout.Button("Clear"))
                    objects.Clear();
            }
        }

        DrawDropArea();

        // Scroll the list so a large selection doesn't push the buttons off-screen.
        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        _serializedObject.Update();
        EditorGUILayout.PropertyField(_serializedObject.FindProperty("objects"), true);
        _serializedObject.ApplyModifiedProperties();
        EditorGUILayout.EndScrollView();

        using (new EditorGUI.DisabledScope(objects.Count == 0))
        {
            if (GUILayout.Button("Rename Objects"))
                Rename();
        }
    }

    private void DrawDropArea()
    {
        Rect area = GUILayoutUtility.GetRect(0f, 38f, GUILayout.ExpandWidth(true));
        GUI.Box(area, "Drag GameObjects here", EditorStyles.helpBox);

        Event evt = Event.current;
        bool inside = area.Contains(evt.mousePosition);

        if ((evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform) && inside)
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

            if (evt.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                foreach (Object dragged in DragAndDrop.objectReferences)
                {
                    if (dragged is GameObject go && !objects.Contains(go))
                        objects.Add(go);
                }
            }

            evt.Use();
        }
    }

    private void AddSelection()
    {
        foreach (GameObject go in Selection.gameObjects)
        {
            if (!objects.Contains(go))
                objects.Add(go);
        }

        Repaint();
    }

    private void Rename()
    {
        // Snapshot the valid objects in list order (skip nulls / duplicates).
        List<GameObject> ordered = new List<GameObject>();
        foreach (GameObject go in objects)
        {
            if (go != null && !ordered.Contains(go))
                ordered.Add(go);
        }

        if (ordered.Count == 0)
            return;

        // Group everything below into a single undo step.
        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Rename Selected");

        // 1. Rename.
        int index = _startIndex;
        foreach (GameObject go in ordered)
        {
            Undo.RecordObject(go, "Rename Selected");
            go.name = $"{_gameObjectPrefix}{index++}";
        }

        // 2. Optionally build a new parent to nest everything under.
        Transform targetParent = null;
        if (_createParent)
        {
            string parentName = string.IsNullOrWhiteSpace(_newParentName) ? "New Parent" : _newParentName;
            GameObject parentGO = new GameObject(parentName);
            Undo.RegisterCreatedObjectUndo(parentGO, "Create Parent");
            targetParent = parentGO.transform;
        }

        // 3. Optionally reparent + reorder. Either checking "Organize In Hierarchy"
        //    or creating a parent triggers this; objects nested under other objects
        //    get pulled out. targetParent == null means the scene root.
        if (_organize || _createParent)
        {
            for (int i = 0; i < ordered.Count; i++)
            {
                Transform t = ordered[i].transform;
                Undo.SetTransformParent(t, targetParent, "Rename Selected");
                t.SetSiblingIndex(i);
            }
        }

        if (targetParent != null)
            Selection.activeGameObject = targetParent.gameObject;

        Undo.CollapseUndoOperations(undoGroup);
    }
}