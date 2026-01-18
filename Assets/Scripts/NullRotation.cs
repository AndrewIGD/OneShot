using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NullRotation : MonoBehaviour
{
    private void LateUpdate()
    {
        transform.eulerAngles = Vector3.zero;
    }
}
