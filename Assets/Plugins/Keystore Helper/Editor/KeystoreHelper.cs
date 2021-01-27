using UnityEngine;
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

    private const int PRIVATE_KEY_LEN = 40;
    private static string privateKey = "";
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

    void AddVersion(int addMajor, int addMinor)
    {
        string[] vers = PlayerSettings.bundleVersion.Split('.');
        int major = int.Parse(vers[0]);
        int minor = int.Parse(vers[1]);
        major += addMajor;
        minor += addMinor;
        PlayerSettings.bundleVersion = string.Format("{0}.{1}", major, minor);
        PlayerSettings.Android.bundleVersionCode = major * 100 + minor;
    }

    void SetVersion(int major, int minor)
    {
        PlayerSettings.bundleVersion = string.Format("{0}.{1}", major, minor);
        PlayerSettings.Android.bundleVersionCode = major * 100 + minor;
    }

    void OnGUI()
    {
        GUILayout.Label("Build Version", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Version", PlayerSettings.bundleVersion);
        EditorGUILayout.LabelField("Bundle Version Code", PlayerSettings.Android.bundleVersionCode.ToString());
        EditorGUILayout.BeginHorizontal();
        {
            if (GUILayout.Button("Major Up"))
            {
                AddVersion(1, 0);
            }
            if (GUILayout.Button("R", GUILayout.Width(30)))
            {
                string[] vers = PlayerSettings.bundleVersion.Split('.');
                int major = int.Parse(vers[0]);
                int minor = int.Parse(vers[1]);
                SetVersion(0, minor);
            }

            if (GUILayout.Button("Minor Up"))
            {
                AddVersion(0, 1);
            }
            if (GUILayout.Button("R", GUILayout.Width(30)))
            {
                string[] vers = PlayerSettings.bundleVersion.Split('.');
                int major = int.Parse(vers[0]);
                int minor = int.Parse(vers[1]);
                SetVersion(major, 0);
            }
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();

        GUILayout.Label("Android Keystore", EditorStyles.boldLabel);
        EditorGUILayout.TextField("PrivateKey", privateKey);
        if (GUILayout.Button("Regenerate Key"))
        {
            privateKey = GeneratePrivateKey(PRIVATE_KEY_LEN);
        }
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
            byte[] bytePrivateKey = System.Text.Encoding.UTF8.GetBytes(privateKey);
            fs.Write(bytePrivateKey, 0, bytePrivateKey.Length);

            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(encryptString);
            fs.Write(bytes, 0, bytes.Length);
        }
    }

    static KeystoreData NewKeystoreData()
    {
        privateKey = GeneratePrivateKey(PRIVATE_KEY_LEN);
        return new KeystoreData();
    }

    public static KeystoreData Load()
    {
        string pathKeyData = GetKeyDataPath();
        if (File.Exists(pathKeyData) == false)
            return NewKeystoreData();

        using (FileStream fs = new FileStream(pathKeyData, FileMode.Open, FileAccess.Read))
        {
            if (fs.Length <= 0)
                return NewKeystoreData();

            byte[] bytePrivateKey = new byte[PRIVATE_KEY_LEN];
            fs.Read(bytePrivateKey, 0, PRIVATE_KEY_LEN);
            privateKey = System.Text.Encoding.UTF8.GetString(bytePrivateKey);

            int readLength = (int)fs.Length - PRIVATE_KEY_LEN;
            byte[] bytes = new byte[readLength];
            fs.Read(bytes, 0, readLength);
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

    static string GeneratePrivateKey(int len)
    {
        string keyletter = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!#$%&()*+,-.:;<=>?@[]^_`{|}~";
        string gen = "";
        for (int i = 0; i < len; ++i)
        {
            gen += keyletter[Random.Range(0, keyletter.Length)];
        }
        return gen;
    }
}