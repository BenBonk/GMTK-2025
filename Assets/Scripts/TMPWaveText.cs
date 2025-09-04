using UnityEngine;
using TMPro;

[RequireComponent(typeof(TMP_Text))]
public class TMPWaveTextSafe : MonoBehaviour
{
    [Header("Wave")]
    public float amplitude = 6f;
    public float speed = 4f;
    public float charPhase = 0.35f;
    public bool horizontal = false;
    public bool unscaledTime = false;

    [Header("Layout Guardrails")]
    public bool enforceSingleLine = true; // prevents wrap -> 1 char per line
    public bool makeChildOfLayoutParent = true; // reminder: keep fitters on parent

    TMP_Text _tmp;
    TMP_MeshInfo[] _original;
    bool _needsRecache;

    void OnEnable()
    {
        _tmp = GetComponent<TMP_Text>();

        if (enforceSingleLine)
        {
            _tmp.enableWordWrapping = false;
            _tmp.overflowMode = TextOverflowModes.Overflow;
        }

        // Re-cache when TMP regenerates geometry
        TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTextChanged);
        Recache();
    }

    void OnDisable()
    {
        TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTextChanged);
    }

    void OnRectTransformDimensionsChange()
    {
        // Layout / scaling changed -> recache
        _needsRecache = true;
    }

    void OnTextChanged(Object obj)
    {
        if (obj == _tmp) _needsRecache = true;
    }

    void Recache()
    {
        _tmp.ForceMeshUpdate();
        _original = _tmp.textInfo.CopyMeshInfoVertexData();
        _needsRecache = false;
    }

    void LateUpdate()
    {
        if (_needsRecache || _original == null) Recache();

        var info = _tmp.textInfo;
        float t = unscaledTime ? Time.unscaledTime : Time.time;

        for (int i = 0; i < info.characterCount; i++)
        {
            var c = info.characterInfo[i];
            if (!c.isVisible) continue;

            int mi = c.materialReferenceIndex;
            int vi = c.vertexIndex;

            Vector3[] src = _original[mi].vertices;
            Vector3[] dst = info.meshInfo[mi].vertices;

            float phase = t * speed + i * charPhase;
            float wave = Mathf.Sin(phase) * amplitude;
            Vector3 offset = horizontal ? new Vector3(wave, 0, 0) : new Vector3(0, wave, 0);

            dst[vi + 0] = src[vi + 0] + offset;
            dst[vi + 1] = src[vi + 1] + offset;
            dst[vi + 2] = src[vi + 2] + offset;
            dst[vi + 3] = src[vi + 3] + offset;
        }

        for (int m = 0; m < info.meshInfo.Length; m++)
        {
            var mi = info.meshInfo[m];
            mi.mesh.vertices = mi.vertices;
            _tmp.UpdateGeometry(mi.mesh, m);
        }
    }
}
