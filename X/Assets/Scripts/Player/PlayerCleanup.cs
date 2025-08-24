using UnityEngine;
using Unity.Netcode;

public class PlayerCleanup : MonoBehaviour
{
    public System.Action onPlayerDestroyed;

    private void OnDestroy()
    {
        onPlayerDestroyed?.Invoke();
    }
}