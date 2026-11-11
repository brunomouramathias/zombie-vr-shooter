using System.Collections;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Management;

/// <summary>
/// Garante que o modo VR (Cardboard) esteja ativo no Android, mesmo que a
/// inicialização automática do XR Plug-in Management falhe.
/// </summary>
public class VrBootstrapper : MonoBehaviour
{
#if UNITY_ANDROID && !UNITY_EDITOR
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Create()
    {
        GameObject go = new GameObject("VR Bootstrapper");
        DontDestroyOnLoad(go);
        go.AddComponent<VrBootstrapper>();
        Debug.Log("[VRBOOT] VrBootstrapper criado.");
    }

    private IEnumerator Start()
    {
        // Dá tempo para a inicialização automática do XR Management.
        yield return new WaitForSecondsRealtime(1.5f);

        if (XRSettings.isDeviceActive)
        {
            Debug.Log("[VRBOOT] XR já ativo: " + XRSettings.loadedDeviceName);
            yield break;
        }

        TryManagementStart();

        yield return new WaitForSecondsRealtime(1.0f);
        if (XRSettings.isDeviceActive)
        {
            Debug.Log("[VRBOOT] XR ativo após inicialização via XR Management.");
            yield break;
        }

        TryDirectLoaderStart();

        yield return new WaitForSecondsRealtime(1.0f);
        Debug.Log("[VRBOOT] Estado final XR: isDeviceActive=" + XRSettings.isDeviceActive +
                  " loadedDeviceName='" + XRSettings.loadedDeviceName + "'");
    }

    private static void TryManagementStart()
    {
        try
        {
            XRGeneralSettings settings = XRGeneralSettings.Instance;
            if (settings == null || settings.Manager == null)
            {
                Debug.LogWarning("[VRBOOT] XRGeneralSettings/Manager indisponível.");
                return;
            }

            XRManagerSettings manager = settings.Manager;
            if (manager.activeLoader == null)
            {
                Debug.Log("[VRBOOT] Inicializando loader via XR Management...");
                manager.InitializeLoaderSync();
            }

            if (manager.activeLoader != null && !XRSettings.isDeviceActive)
            {
                Debug.Log("[VRBOOT] Iniciando subsistemas XR...");
                manager.StartSubsystems();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[VRBOOT] Falha no XR Management: " + e.Message);
        }
    }

    private static void TryDirectLoaderStart()
    {
        try
        {
            Google.XR.Cardboard.XRLoader loader = Resources.Load<Google.XR.Cardboard.XRLoader>("XR/XRLoader");
            if (loader == null)
            {
                Debug.LogError("[VRBOOT] XRLoader do Cardboard não encontrado em Resources/XR/XRLoader.");
                return;
            }

            Debug.Log("[VRBOOT] Inicializando Cardboard XRLoader diretamente...");
            if (loader.Initialize())
            {
                loader.Start();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("[VRBOOT] Falha ao inicializar Cardboard diretamente: " + e);
        }
    }
#endif
}
