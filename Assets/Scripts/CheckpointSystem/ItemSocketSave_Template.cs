using UnityEngine;

/// <summary>
/// TEMPLATE for your IItemSocket implementations.
/// Save whether the socket is occupied and (optionally) which item (by SaveableEntity.Id).
/// Wire the Apply* methods to your socket's public API in the inspector (via UnityEvents) or code.
/// </summary>
public class ItemSocketSave_Template : MonoBehaviour, ISaveable
{
    [System.Serializable] class Data { public bool occupied; public string itemId; public bool active; }

    [Header("Apply Hooks (optional)")]
    [SerializeField] private bool applyActiveSelf = true;

    public string CaptureState()
    {
        var d = new Data
        {
            occupied = GetIsOccupied(),
            itemId = GetCurrentItemEntityId(),
            active = gameObject.activeSelf
        };
        return JsonUtility.ToJson(d);
    }

    public void RestoreState(string json)
    {
        var d = JsonUtility.FromJson<Data>(json);
        if (applyActiveSelf) gameObject.SetActive(d.active);
        if (d.occupied) ApplyInsertByEntityId(d.itemId);
        else ApplyEject();
    }

    // ---- Adapt these to your IItemSocket API ----
    private bool GetIsOccupied() { /* TODO */ return false; }
    private string GetCurrentItemEntityId() { /* TODO */ return null; }
    private void ApplyInsertByEntityId(string entityId)
    {
        // TODO: locate SaveableEntity by id and insert it into this socket.
    }
    private void ApplyEject()
    {
        // TODO: force eject if your socket supports it.
    }
}