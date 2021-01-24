﻿using UnityEngine;
using UnityEditor;
using System.IO;
using System.Security.Cryptography;

class KeystoreHelper : EditorWindow
{
    [System.Serializable]
    public class KeystoreData
    {
        public string keystorePass = "";
        public string keyaliasName = "";
        public string keyaliasPass = "";

        public AndroidMinification releaseMinify;
    }
    private KeystoreData _keystoreData = new KeystoreData();

    private static readonly string privateKey = "[PLEASE_ENTER_THE_PRIVATE_KEY_YOU_WANT]";
    private static readonly string keyDataFileName = "user.keyinfo";

    [MenuItem("Window/Keystore Helper")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(KeystoreHelper));
    }

    void OnEnable()
    {
        _keystoreData = Load();
        AndroidSetting(_keystoreData);
    }

    void OnDisable()
    {
        Save(_keystoreData);
        AndroidSetting(_keystoreData);
    }

    void OnGUI()
    {
        GUILayout.Label("Android Keystore", EditorStyles.boldLabel);
        _keystoreData.keystorePass = EditorGUILayout.PasswordField("Keystore Password", _keystoreData.keystorePass);
        _keystoreData.keyaliasName = EditorGUILayout.TextField("Key Alias Name", _keystoreData.keyaliasName);
        _keystoreData.keyaliasPass = EditorGUILayout.PasswordField("Key Alias Password", _keystoreData.keyaliasPass);
        EditorGUILayout.Space();

        GUILayout.Label("Build Settings", EditorStyles.boldLabel);
        _keystoreData.releaseMinify = (AndroidMinification)EditorGUILayout.EnumPopup("Release Minify", _keystoreData.releaseMinify);
    }

    static string GetKeyDataPath()
    {
        string pathKeyData;

        if (string.IsNullOrEmpty(PlayerSettings.Android.keystoreName))
            pathKeyData = Application.dataPath + "/../" + keyDataFileName;
        else
        {
            string keyPath = Path.GetDirectoryName(PlayerSettings.Android.keystoreName);
            if (string.IsNullOrEmpty(keyPath) == false)
                keyPath += "/";
            pathKeyData = Application.dataPath + "/../" + keyPath + keyDataFileName;
        }

        return pathKeyData;
    }

    static public void AndroidSetting(KeystoreData keystoreData)
    {
        PlayerSettings.Android.keystorePass = keystoreData.keystorePass;
        PlayerSettings.Android.keyaliasName = keystoreData.keyaliasName;
        PlayerSettings.Android.keyaliasPass = keystoreData.keyaliasPass;

        EditorUserBuildSettings.androidReleaseMinification = keystoreData.releaseMinify;
    }

    static void Save(KeystoreData keystoreData)
    {
        string jsonString = JsonUtility.ToJson(keystoreData);
        string encryptString = Encrypt(jsonString);
        using (FileStream fs = new FileStream(GetKeyDataPath(), FileMode.Create, FileAccess.Write))
        {
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(encryptString);
            fs.Write(bytes, 0, bytes.Length);
        }
    }

    public static KeystoreData Load()
    {
        string pathKeyData = GetKeyDataPath();
        if (File.Exists(pathKeyData) == false)
            return new KeystoreData();

        using (FileStream fs = new FileStream(pathKeyData, FileMode.Open, FileAccess.Read))
        {
            byte[] bytes = new byte[(int)fs.Length];
            fs.Read(bytes, 0, (int)fs.Length);
            string encryptData = System.Text.Encoding.UTF8.GetString(bytes);
            string decryptData = Decrypt(encryptData);

            KeystoreData keystoreData = JsonUtility.FromJson<KeystoreData>(decryptData);
            return keystoreData;
        }
    }

    private static string Encrypt(string data)
    {
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(data);
        RijndaelManaged rm = CreateRijndaelManaged();
        ICryptoTransform ct = rm.CreateEncryptor();
        byte[] results = ct.TransformFinalBlock(bytes, 0, bytes.Length);
        return System.Convert.ToBase64String(results, 0, results.Length);
    }

    private static string Decrypt(string data)
    {
        byte[] bytes = System.Convert.FromBase64String(data);
        RijndaelManaged rm = CreateRijndaelManaged();
        ICryptoTransform ct = rm.CreateDecryptor();
        byte[] resultArray = ct.TransformFinalBlock(bytes, 0, bytes.Length);
        return System.Text.Encoding.UTF8.GetString(resultArray);
    }

    private static RijndaelManaged CreateRijndaelManaged()
    {
        byte[] keyArray = System.Text.Encoding.UTF8.GetBytes(privateKey);
        RijndaelManaged result = new RijndaelManaged();

        byte[] newKeysArray = new byte[16];
        System.Array.Copy(keyArray, 0, newKeysArray, 0, 16);

        result.Key = newKeysArray;
        result.Mode = CipherMode.ECB;
        result.Padding = PaddingMode.PKCS7;
        return result;
    }
}