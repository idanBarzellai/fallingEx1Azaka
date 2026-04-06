using UnityEngine;

public class AutoDestroyAfterSeconds : MonoBehaviour
{
    public float lifetime = 0.4f;

    private void Start()
    {
        Destroy(gameObject, lifetime);
    }
}