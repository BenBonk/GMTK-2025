using DG.Tweening;
using UnityEngine;

public static class LassoCleaner
{
    // Destroys any scene objects named "Lasso(Clone)" or "LassoTip"
    public static void CleanupAll()
    {
        // Includes inactive objects (true)
        var allTransforms = Object.FindObjectsOfType<Transform>(true);

        for (int i = 0; i < allTransforms.Length; i++)
        {
            var go = allTransforms[i].gameObject;
            var n = go.name;

            if (n == "Lasso(Clone)" || n == "LassoTip")
            {
                // Safety: only touch scene objects (not prefabs/assets)
                if (!go.scene.IsValid() || !go.scene.isLoaded) continue;

                // Kill any tweens targeting this object (prevents resurrection)
                DOTween.Kill(go, complete: false);

                Object.Destroy(go);
            }
        }
    }
}

