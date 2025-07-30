using UnityEngine; // ✅ 반드시 있어야 함


public interface IItemSocket
{
    bool TryInteract(GameObject item);
    bool CanInteract(GameObject item);
}