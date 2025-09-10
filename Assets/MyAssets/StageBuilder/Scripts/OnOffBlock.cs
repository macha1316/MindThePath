using UnityEngine;

// Marker + visual toggle for ON/OFF blocks ('H')
// 要件: ONの時は子"cube"を表示、OFFの時は子"all_parent"を表示
public class OnOffBlock : MonoBehaviour
{
    private GameObject _cube;
    private GameObject _allParent;

    private void Awake()
    {
        foreach (var t in GetComponentsInChildren<Transform>(true))
        {
            if (_cube == null && t.name == "cube") _cube = t.gameObject;
            if (_allParent == null && t.name == "all_parent") _allParent = t.gameObject;
        }
    }

    public void SetSolid(bool solid)
    {
        if (_cube != null) _cube.SetActive(solid);
        if (_allParent != null) _allParent.SetActive(!solid);
    }
}
