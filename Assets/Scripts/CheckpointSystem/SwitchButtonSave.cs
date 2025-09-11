using UnityEngine;
using System;
using System.Reflection;

[DisallowMultipleComponent]
public class SwitchButtonSave : MonoBehaviour, ISaveable
{
    [Header("Refs")]
    [SerializeField] private SwitchButtonController controller; // 본체에 붙은 원본 컨트롤러
    [SerializeField] private Transform switchButton;            // 눌리는 자식 Transform
    [SerializeField] private Light pointLight;                  // (선택) 버튼 라이트

    [Header("Tag Restore")]
    [SerializeField] private bool restoreTag = true;
    [SerializeField] private string interactTag = "Interact";   // 초기 상호작용 태그

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

        // 눌림 상태 복원: 자식 회전과 컨트롤러 내부 플래그를 동기화
        if (switchButton) switchButton.localRotation = d.childLocalRot;
        SetPressedPrivate(controller, d.pressed);

        // 태그 복원: 눌린 상태면 Untagged, 아니면 interactTag(혹은 저장 당시 태그)로
        if (restoreTag)
        {
            if (d.pressed) gameObject.tag = "Untagged";
            else gameObject.tag = string.IsNullOrEmpty(interactTag) ? d.tag : interactTag;
        }

        // 라이트 상태 복원(선택)
        if (pointLight && d.hasLight)
        {
            pointLight.range = d.lRange;
            pointLight.intensity = d.lIntensity;
        }
    }

    // SwitchButtonController.isPressed (private bool) 반영
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
            Debug.LogWarning("[SwitchButtonSave] 'isPressed' 필드를 찾지 못했습니다.");
        }
    }
}
