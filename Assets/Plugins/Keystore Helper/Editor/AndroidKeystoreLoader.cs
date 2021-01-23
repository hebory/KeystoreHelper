using UnityEditor;

#if UNITY_EDITOR

[InitializeOnLoad]
public class AndroidKeystoreLoader
{
    static AndroidKeystoreLoader()
    {
        var keystoreData = KeystoreHelper.Load();

        PlayerSettings.Android.keystorePass = keystoreData.keystorePass;
        PlayerSettings.Android.keyaliasName = keystoreData.keyaliasName;
        PlayerSettings.Android.keyaliasPass = keystoreData.keyaliasPass;
    }
}

#endif