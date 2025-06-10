using Cysharp.Threading.Tasks;

using UnityEngine;

public class EtCetera : MonoBehaviour
{
    public void OpenQualitySettings()
    {
        QualitySettingsModal.OpenQualitySettingsModalAsync(this.GetCancellationTokenOnDestroy()).Forget();
    }
}
