using UnityEngine;
using System.Collections.Generic;

// The three period types the game structures its scenes around.
public enum PeriodType { ClassHalls, Recess, Lunch }

// The master structure of the game: three ordered lists of scenes. Add, remove,
// and reorder freely to build out the game and insert scenes as you develop.
// Create one via: Assets ▸ Create ▸ School ▸ Main Progression Scene List.
[CreateAssetMenu(menuName = "School/Main Progression Scene List")]
public class MainProgressionSceneList : ScriptableObject
{
    [Tooltip("Ordered Class Halls scenes. Element 0 = the first class scene.")]
    public List<SceneReference> classScenes = new List<SceneReference>();

    [Tooltip("Ordered Recess scenes. Element 0 = the first recess.")]
    public List<SceneReference> recessScenes = new List<SceneReference>();

    [Tooltip("Ordered Lunch scenes. Element 0 = the first lunch.")]
    public List<SceneReference> lunchScenes = new List<SceneReference>();

    public List<SceneReference> GetList(PeriodType type)
    {
        switch (type)
        {
            case PeriodType.ClassHalls: return classScenes;
            case PeriodType.Recess:     return recessScenes;
            case PeriodType.Lunch:      return lunchScenes;
            default: return null;
        }
    }

    public int Count(PeriodType type)
    {
        var list = GetList(type);
        return list != null ? list.Count : 0;
    }

    public SceneReference Get(PeriodType type, int index)
    {
        var list = GetList(type);
        if (list == null || index < 0 || index >= list.Count) return null;
        return list[index];
    }
}
