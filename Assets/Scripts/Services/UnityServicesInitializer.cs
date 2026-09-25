using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;

/// <summary>
/// Boot Unity Gaming Services sekali, idempotent, dan independen terhadap scene.
/// Panggil <see cref="EnsureServicesAsync"/> dari mana saja; beberapa pemanggil
/// akan berbagi satu Task sehingga init/sign-in hanya berjalan sekali.
/// </summary>
public class UnityServicesInitializer : MonoBehaviour
{
    static Task s_BootTask;

    /// <summary>
    /// Memastikan Unity Services + auth anonim siap. Dipanggil berulang aman
    /// (idempotent); jika boot sebelumnya gagal, pemanggilan berikutnya akan mencoba lagi.
    /// </summary>
    public static Task EnsureServicesAsync()
    {
        if (s_BootTask == null || s_BootTask.IsFaulted || s_BootTask.IsCanceled)
        {
            s_BootTask = BootAsync();
        }

        return s_BootTask;
    }

    static async Task BootAsync()
    {
        try
        {
            await UnityServices.InitializeAsync();

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }

            PlayerSession.PlayerId = AuthenticationService.Instance.PlayerId;

            if (!string.IsNullOrWhiteSpace(PlayerSession.Nama))
            {
                await AuthenticationService.Instance.UpdatePlayerNameAsync(PlayerSession.Nama);
            }

            Debug.Log("[UnityServicesInitializer] Unity Services siap. PlayerId: " + PlayerSession.PlayerId);
        }
        catch (System.Exception e)
        {
            Debug.LogError("[UnityServicesInitializer] Gagal initialize Unity Services: " + e);
            throw;
        }
    }

    /// <summary>
    /// Self-bootstrap: boot layanan langsung saat play dimulai dari scene manapun,
    /// sehingga tidak bergantung pada scene dimulai dari MainMenu.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AutoBoot()
    {
        _ = EnsureServicesAsync();
    }

    private async void Awake()
    {
        try
        {
            await EnsureServicesAsync();
        }
        catch (System.Exception e)
        {
            Debug.LogError("[UnityServicesInitializer] Bootstrap gagal di scene: " + e.Message);
        }
    }
}