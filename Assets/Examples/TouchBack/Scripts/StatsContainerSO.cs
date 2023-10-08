using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "StatsContainter")]
public class StatsContainerSO : ScriptableObject
{
    public List<StatsSO> stats = new List<StatsSO>();

    /// <summary>
    /// Returns stats list length.
    /// </summary>
    public int GetListLength() {
        return stats.Count;
    }
}
