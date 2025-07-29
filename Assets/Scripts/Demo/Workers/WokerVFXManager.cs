using System.Collections;
using UnityEngine;
using UnityEngine.VFX;

public class WokerVFXManager : MonoBehaviour
{
    public VisualEffectAsset clickedVFX;


    private GameObject particleTransform;
    private VisualEffect vfxComponent;

    private void Start()
    {
        particleTransform = new GameObject("ParticleTransform");
        particleTransform.transform.parent = transform;
        particleTransform.transform.localPosition= Vector3.zero;
    }
    public void onClicked()
    {
        StartCoroutine(playVFX(clickedVFX));
    }

    private IEnumerator playVFX(VisualEffectAsset vfxAsset, float duration = 2)
    {
        GameObject vfxInstance = new GameObject("vfxInstance");
        vfxInstance.transform.parent = transform;
        vfxInstance.transform.localPosition = Vector3.zero;
        VisualEffect vfxComponentInstance = vfxInstance.AddComponent<VisualEffect>();
        vfxComponentInstance.visualEffectAsset = clickedVFX;
        vfxComponentInstance.Play();
        yield return new WaitForSeconds(duration);
        Destroy(vfxInstance);
    }
}
