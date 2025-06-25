using UnityEngine;

[ExecuteInEditMode]
public class EdgeDetectionEffect : MonoBehaviour
{
    public Shader edgeDetectShader;
    private Material edgeDetectMaterial;

    [Range(0, 20)]
    public float edgeThreshold = 0.1f; // ���� ���� �ΰ���

    void Start()
    {
        if (edgeDetectShader == null)
        {
            Debug.LogError("Edge Detection Shader�� �������� �ʾҽ��ϴ�.");
            enabled = false;
            return;
        }

        if (!edgeDetectShader.isSupported)
        {
            Debug.LogError("�� ��⿡���� Edge Detection Shader�� �������� �ʽ��ϴ�.");
            enabled = false;
            return;
        }

        edgeDetectMaterial = new Material(edgeDetectShader)
        {
            hideFlags = HideFlags.HideAndDontSave
        };
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (edgeDetectMaterial != null)
        {
            edgeDetectMaterial.SetFloat("_Threshold", edgeThreshold);
            Graphics.Blit(source, destination, edgeDetectMaterial);
        }
        else
        {
            Graphics.Blit(source, destination);
        }
    }

    void OnDestroy()
    {
        if (edgeDetectMaterial)
        {
            DestroyImmediate(edgeDetectMaterial);
        }
    }
}
