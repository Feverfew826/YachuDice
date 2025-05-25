using Cysharp.Threading.Tasks;

using UnityEngine;

public class EtCetera : MonoBehaviour
{
    public void OpenQualitySettings()
    {
        QualitySettings.OpenQualitySettingsAsync(this.GetCancellationTokenOnDestroy()).Forget();
    }
}
