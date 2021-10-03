using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HpBar : MonoBehaviour
{
    [SerializeField]
    Transform _hpBar = null;
    
    public void SetHpBar(float ratio)
    {
        // Clamp => 0이하, 1이상으로 가면 각각 0, 1로 할당 & 0~1사이면 해당 값으로 할당
        ratio = Mathf.Clamp(ratio, 0, 1);
        _hpBar.localScale = new Vector3(ratio, 1, 1);
    }
}
