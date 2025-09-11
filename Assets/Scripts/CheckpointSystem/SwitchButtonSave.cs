using UnityEngine;
using System;
using System.Reflection;

[DisallowMultipleComponent]
public class SwitchButtonSave : MonoBehaviour, ISaveable
{
    [Header("Refs")]
    [SerializeField] private SwitchButtonController controller; // ��ü�� ���� ���� ��Ʈ�ѷ�
    [SerializeField] private Transform switchButton;            // ������ �ڽ� Transform
    [SerializeField] private Light pointLight;                  // (����) ��ư ����Ʈ

    [Header("Tag Restore")]
    [SerializeField] private bool restoreTag = true;
    [SerializeField] private string interactTag = "Interact";   // �ʱ� ��ȣ�ۿ� �±�

    [Serializable]
    private struct Data
    {
        public bool active;
        public bool pressed;
        public Quaternion childLocalRot;
        public string tag;
        public bool hasLight;
        public float lRange;
        public float lIntensity;
    }

    public string CaptureState()
    {
        if (controller == null) controller = GetComponent<SwitchButtonController>();
        var d = new Data
        {
            active = gameObject.activeSelf,
            pressed = controller != null ? controller.IsPressed : false,
            childLocalRot = switchButton ? switchButton.localRotation : Quaternion.identity,
            tag = gameObject.tag,
            hasLight = pointLight != null,
            lRange = pointLight ? pointLight.range : 0f,
            lIntensity = pointLight ? pointLight.intensity : 0f
        };
        return JsonUtility.ToJson(d);
    }

    public void RestoreState(string json)
    {
        var d = JsonUtility.FromJson<Data>(json);

        gameObject.SetActive(d.active);

        // ���� ���� ����: �ڽ� ȸ���� ��Ʈ�ѷ� ���� �÷��׸� ����ȭ
        if (switchButton) switchButton.localRotation = d.childLocalRot;
        SetPressedPrivate(controller, d.pressed);

        // �±� ����: ���� ���¸� Untagged, �ƴϸ� interactTag(Ȥ�� ���� ��� �±�)��
        if (restoreTag)
        {
            if (d.pressed) gameObject.tag = "Untagged";
            else gameObject.tag = string.IsNullOrEmpty(interactTag) ? d.tag : interactTag;
        }

        // ����Ʈ ���� ����(����)
        if (pointLight && d.hasLight)
        {
            pointLight.range = d.lRange;
            pointLight.intensity = d.lIntensity;
        }
    }

    // SwitchButtonController.isPressed (private bool) �ݿ�
    private static void SetPressedPrivate(SwitchButtonController ctrl, bool value)
    {
        if (ctrl == null) return;
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var fi = typeof(SwitchButtonController).GetField("isPressed", flags);
        if (fi != null && fi.FieldType == typeof(bool))
        {
            fi.SetValue(ctrl, value);
        }
        else
        {
            Debug.LogWarning("[SwitchButtonSave] 'isPressed' �ʵ带 ã�� ���߽��ϴ�.");
        }
    }
}
