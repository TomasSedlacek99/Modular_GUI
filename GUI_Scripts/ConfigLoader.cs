using UnityEngine;
using System.IO;

/// <summary>
/// Loads the server configuration (like IP address) from a JSON file stored in persistent data path.
/// </summary>
public class ConfigLoader : MonoBehaviour
{
    public static string ServerBaseUrl { get; private set; } // = "http://192.168.1.36:8081";
    public static string GuiId { get; private set; }
    public static string LinePartsId { get; private set; }
    public static string MqttBrokerUrl { get; private set; }

    private const string ConfigFileName = "config.json";

    [System.Serializable]
    private class ConfigData
    {
        public string serverBaseUrl;
        public string GUISubmodelId;
        public string LinePartsSubmodelId;
        public string mqttBrokerUrl;
    }

    void Awake()
    {
        string path;
        #if UNITY_EDITOR
            path = Path.Combine(Application.streamingAssetsPath, ConfigFileName);
        #else
            path = Path.Combine(Application.persistentDataPath, ConfigFileName);
        #endif

        //string path = Path.Combine(Application.persistentDataPath, ConfigFileName);

        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            ConfigData config = JsonUtility.FromJson<ConfigData>(json);

            if (!string.IsNullOrEmpty(config.serverBaseUrl) && !string.IsNullOrEmpty(config.GUISubmodelId))
            {
                ServerBaseUrl = config.serverBaseUrl;
                Debug.Log($"[ConfigLoader] Loaded server address: {ServerBaseUrl}");
                GuiId = config.GUISubmodelId;
                Debug.Log($"[ConfigLoader] Loaded GUI Submodel Id: {GuiId}");
                if (!string.IsNullOrEmpty(config.LinePartsSubmodelId)) 
                {
                    LinePartsId = config.LinePartsSubmodelId;
                    Debug.Log($"[ConfigLoader] Loaded LineParts Submodel Id: {LinePartsId}");
                }
                if (!string.IsNullOrEmpty(config.mqttBrokerUrl)) 
                {
                    MqttBrokerUrl = config.mqttBrokerUrl;
                    Debug.Log($"[ConfigLoader] Loaded MQTT Broker URL: {MqttBrokerUrl}");
                }
            }
        }
        else
        {
            Debug.LogWarning($"[ConfigLoader] Config file not found at {path}. Using default address.");
        }
    }
}

