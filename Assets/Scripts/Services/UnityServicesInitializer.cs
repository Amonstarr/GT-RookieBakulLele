using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;

public class UnityServicesInitializer : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
     private async void Awake()
    {
        try
        {
            await UnityServices.InitializeAsync();

            Debug.Log("Unity Services berhasil diinisialisasi.");

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log("Berhasil masuk sebagai pengguna anonim.");
            }
            Debug.Log("Authentication berhasil.");
            PlayerSession.PlayerId = AuthenticationService.Instance.PlayerId;
            Debug.Log("Player ID: " + AuthenticationService.Instance.PlayerId);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Gagal initialize Unity Services: " + e);
        }
    }
}
