using UnityEngine;

[ExecuteInEditMode]
public class EdgeDetectionEffect : MonoBehaviour
{
    public Shader edgeDetectShader;
    private Material edgeDetectMaterial;

    [Range(0, 20)]
    public float edgeThreshold = 0.1f; // 엣지 감지 민감도

    void Start()
    {
        if (edgeDetectShader == null)
        {
            Debug.LogError("Edge Detection Shader가 설정되지 않았습니다.");
            enabled = false;
            return;
        }

        if (!edgeDetectShader.isSupported)
        {
            Debug.LogError("이 기기에서는 Edge Detection Shader를 지원하지 않습니다.");
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
