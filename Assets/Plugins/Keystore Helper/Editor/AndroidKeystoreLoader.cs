using UnityEditor;

#if UNITY_EDITOR

[InitializeOnLoad]
public class AndroidKeystoreLoader
{
    static AndroidKeystoreLoader()
    {
        var keystoreData = KeystoreHelper.Load();
        KeystoreHelper.AndroidSetting(keystoreData);
    }
}

#endif