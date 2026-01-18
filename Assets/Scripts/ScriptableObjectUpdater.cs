using System.Collections.Generic;
using UnityEngine;

public class ScriptableObjectUpdater : MonoBehaviour
{
    [SerializeField] private List<IUpdateable> updateables;

    public static ScriptableObjectUpdater Instance;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        foreach (var updateable in updateables)
        {
            updateable.Update();
        }
    }
}
