/// <summary>
/// Script for fetching GUI elements from an AAS server and instantiating CanvasDialog prefabs accordingly.
/// </summary>
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using SimpleJSON;
using MixedReality.Toolkit.SpatialManipulation;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityOpcUaPublisher;
using System.Collections.Concurrent;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

public class AASFetcher : MonoBehaviour
{
    //[SerializeField] private string guiAasUrl = "http://192.168.1.36:8081/submodels/R1VJX1N1Ym1vZGVs/submodel-elements";

    // URL for GET from Eclipse Basyx Server - IP of the server with port 8081, based on the config file + /submodels/ + Id of the given Submodel (Id (Base64)) + /submodel-elements => whole URL as string 
    private string GuiAasUrl => ConfigLoader.ServerBaseUrl + "/submodels/" + ConfigLoader.GuiId + "/submodel-elements";

    [SerializeField] private GameObject canvasDialogPrefab;
    [SerializeField] private AASLinePartsLoader linePartsLoader;
    [SerializeField] private GameObject fischertechnikModelParent; // Link to the 3D model parent, which can contain Colliders, RigidBody etc... If LinkedParts are used, be aware that this should be their "container"

    /// <summary>
    /// Maps NodeId to the corresponding TextMeshProUGUI to allow live value updates.
    /// </summary>
    public static Dictionary<string, TMPro.TextMeshProUGUI> nodeIdToTextMap = new();
    [Header("OPC UA PubSub (MQTT) Settings")]
    public string BrokerUrl = "mqtt://192.168.1.36:1883"; 
    public string Topic = "Machine/Sensors";
    public string TargetPublisherId = "UnityPublisher";

    private OpcUaSubscriber _subscriber;
    // Fronta na bezpeèný prenos z background vlákna
    private ConcurrentQueue<(string nodeId, string value)> _uiQueue = new ConcurrentQueue<(string, string)>();
    // Zoznam premenných, na ktoré sa po parsovaní prihlásime
    private List<string> _nodeIdsToSubscribe = new List<string>();
    private long _lastJitterTimeStamp = 0;

    private IEnumerator Start()
    {
        // Poèkáme, kým sa ConfigLoader nenaèíta
        while (string.IsNullOrEmpty(ConfigLoader.ServerBaseUrl))
        {
            Debug.Log("[AASFetcher] Waiting for ConfigLoader to initialize...");
            yield return null; // poèkaj jeden frame a skús znova
        }

        Debug.Log($"[AASFetcher] ConfigLoader ready. ServerBaseUrl = {ConfigLoader.ServerBaseUrl}");
        StartCoroutine(InitializeAll());
    }

    /// <summary>
    /// Initializes the AAS fetcher by first loading line parts and then fetching GUI elements.
    /// </summary>
    private IEnumerator InitializeAll()
    {
        yield return StartCoroutine(linePartsLoader.LoadLineParts());
        yield return StartCoroutine(GetSubmodelElements());
    }

    /// <summary>
    /// Fetches submodel elements from the AAS server and creates GUI dialog elements accordingly.
    /// Adds simulated data support if NodeId is found - this part of the code is strictly made just for the presentation purpose 
    /// and if implemented in other specific scenario, the code should be changed. 
    /// </summary>
    private IEnumerator GetSubmodelElements()
    {
        // Stopwatches for validation, testing and logging
        Stopwatch totalTimer = Stopwatch.StartNew();
        Stopwatch stepTimer = new Stopwatch();

        using (UnityWebRequest request = UnityWebRequest.Get(GuiAasUrl))
        {
            request.SetRequestHeader("Accept", "application/json");
            request.downloadHandler = new DownloadHandlerBuffer();

            stepTimer.Start();
            yield return request.SendWebRequest();
            stepTimer.Stop();
            long fetchTimeMs = stepTimer.ElapsedMilliseconds;

            if (request.result == UnityWebRequest.Result.Success)
            {
                stepTimer.Restart();
                JSONNode root = JSON.Parse(request.downloadHandler.text);
                JSONArray resultArray = root["result"].AsArray;
                stepTimer.Stop();
                long parseTimeMs = stepTimer.ElapsedMilliseconds;

                Debug.Log($"GUI Submodel contains {resultArray.Count} elements.");

                stepTimer.Restart();

                foreach (JSONNode collection in resultArray)
                {
                    string idShort = collection["idShort"];
                    JSONArray valueArray = collection["value"].AsArray;

                    string nodeId = GetValue(valueArray, "NodeId");
                    string header = GetValue(valueArray, "HeaderText");
                    string main = GetValue(valueArray, "MainText");
                    float fontSize = ParseFloat(GetValue(valueArray, "FontSize"));

                    GameObject dialogInstance = Instantiate(canvasDialogPrefab);
                    dialogInstance.name = idShort;
                    SetDialogText(dialogInstance, header, main, fontSize);

                    // If NodeId is defined, bind it to the TextMeshPro component and register simulated value
                    if (!string.IsNullOrEmpty(nodeId))
                    {
                        Transform mainTextTransform = dialogInstance.transform.Find("Canvas/Main Text");
                        if (mainTextTransform != null)
                        {
                            var textComponent = mainTextTransform.GetComponent<TMPro.TextMeshProUGUI>();
                            if (textComponent != null)
                            {
                                if (!nodeIdToTextMap.ContainsKey(nodeId))
                                {
                                    nodeIdToTextMap.Add(nodeId, textComponent);
                                    _nodeIdsToSubscribe.Add(nodeId);
                                    Debug.Log($"[AASFetcher] NodeId ready to subscribe: { nodeId}");
                                }

                                /*SimulatedDataUpdater updater = FindObjectOfType<SimulatedDataUpdater>();
                                if (updater != null && !updater.variables.Exists(v => v.NodeId == nodeId))
                                {
                                    // Create new simulated variable if it does not exist yet = in proper OPC UA implementation this part should containt 
                                    // loading data from given OPC UA node and creating a new variable based on that data. This is just creation of the 
                                    // Temperature variable for demonstration purpose. If proper connection with OPC UA will be updated, the SimulatedDataUpdater.cs 
                                    // should be changed. 
                                    SimulatedVariableNode newVar = new SimulatedVariableNode
                                    {
                                        NodeId = nodeId,
                                        Value = 20.0 + Random.Range(-5f, 5f),
                                        DisplayName = "Sim_var " + nodeId,
                                        BrowseName = "Simulated variable",
                                        NodeClass = "Variable",
                                        DataType = "Double",
                                        TypeDefinition = "BaseDataVariableType",
                                        DataTypeDefinition = "Float",
                                        Description = "Auto-generated simulation node",
                                        ValueRank = -1,
                                        IsHistorizing = false,
                                        AccessLevel = 1
                                    };
                                    updater.variables.Add(newVar);
                                    Debug.Log($"Created simulated variable for NodeId: {nodeId}");
                                }*/
                            }
                        }
                    }
                    

                    // Position and transform data
                    string linkedPartId = GetReferenceTargetId(valueArray, "LinkedPart");
                    Vector3 position = Vector3.zero;
                    Vector3 rotation = GetVector3(valueArray, "Rotation", "RotX", "RotY", "RotZ");
                    Vector3 scale = GetVector3(valueArray, "Scale", "ScaX", "ScaY", "ScaZ");

                    if (!string.IsNullOrEmpty(linkedPartId) && linePartsLoader.LinePartsPositions.TryGetValue(linkedPartId, out Vector3 partPosition))
                    {
                        Vector3 offset = GetVector3(valueArray, "Position", "PosX", "PosY", "PosZ");
                        //GameObject parent = GameObject.Find("Gesamtmodell_5_18_09");

                        if (fischertechnikModelParent != null)
                        {
                            dialogInstance.transform.SetParent(fischertechnikModelParent.transform, worldPositionStays: false);
                            dialogInstance.transform.localPosition = partPosition + offset;
                        }
                        else
                        {
                            Debug.LogWarning("Parent object not assigned.");
                            dialogInstance.transform.position = partPosition + offset;
                        }
                    }
                    else
                    {
                        position = GetVector3(valueArray, "Position", "PosX", "PosY", "PosZ");
                        dialogInstance.transform.position = position;
                    }

                    dialogInstance.transform.localRotation = Quaternion.Euler(rotation);
                    dialogInstance.transform.localScale = scale;

                    // Disabling of the followScript, which is automatically instanstiated wich CanvasDialog
                    Follow followScript = dialogInstance.GetComponent<Follow>();
                    if (followScript != null) followScript.enabled = false;

                    // Disabling of the horizontal part of the CanvasDialog - row with 3 buttons, which could be used in the future
                    Transform horizontal = dialogInstance.transform.Find("Canvas/Horizontal");
                    if (horizontal != null) horizontal.gameObject.SetActive(false);

                    // Configuring of the ContentSizeFitter component
                    SetContentFitter(dialogInstance);
                }
                stepTimer.Stop();
                long instantiateTimeMs = stepTimer.ElapsedMilliseconds;

                if (_nodeIdsToSubscribe.Count > 0) StartOpcUaSubscription();

                totalTimer.Stop();
                long totalTimeMs = totalTimer.ElapsedMilliseconds;

                Debug.Log($"[SCI_METRICS] AAS Load | Elements: {resultArray.Count} | Fetch: {fetchTimeMs}ms | Parse: {parseTimeMs}ms | Instantiate: {instantiateTimeMs}ms | Total: {totalTimeMs}ms");
            }
            else
            {
                Debug.LogError("URL targeted: " + GuiAasUrl);
                Debug.LogError($"Request failed: {request.responseCode} - {request.error}");
            }
        }
    }

    /// <summary>
    /// Retrieves the value of a simple property with given idShort.
    /// </summary>
    /// <param name="arr">JSONArray of properties</param>
    /// <param name="idShort">Identifier of the property</param>
    /// <returns>Value as string or null</returns>
    private string GetValue(JSONArray arr, string idShort)
    {
        foreach (JSONNode prop in arr)
            if (prop["idShort"] == idShort)
                return prop["value"];
        return null;
    }

    /// <summary>
    /// Extracts reference target id from a ReferenceElement node.
    /// </summary>
    /// <param name="array">JSONArray to search</param>
    /// <param name="idShort">Target reference idShort</param>
    /// <returns>Target id value or null</returns>
    private string GetReferenceTargetId(JSONArray array, string idShort)
    {
        foreach (JSONNode node in array)
        {
            if (node["idShort"] == idShort && node["valueId"] != null)
            {
                JSONArray keys = node["valueId"]["keys"].AsArray;
                if (keys.Count > 0)
                    return keys[keys.Count - 1]["value"];
            }
        }
        return null;
    }

    /// <summary>
    /// Attempts to parse a float with fallback value.
    /// </summary>
    /// <param name="value">String value to parse</param>
    /// <param name="fallback">Fallback float if parsing fails</param>
    /// <returns>Parsed float or fallback</returns>
    private float ParseFloat(string value, float fallback = 0f)
    {
        return float.TryParse(value, out float result) ? result : fallback;
    }

    /// <summary>
    /// Gets a value from a nested SubmodelElementCollection group.
    /// </summary>
    /// <param name="valueArray">JSONArray of submodel elements</param>
    /// <param name="groupIdShort">IdShort of the nested group</param>
    /// <param name="coord">Specific coordinate idShort (e.g., PosX)</param>
    /// <returns>Value as string or "0"</returns>
    private string GetNestedValue(JSONArray valueArray, string groupIdShort, string coord)
    {
        foreach (JSONNode node in valueArray)
        {
            if (node["modelType"] == "SubmodelElementCollection" && node["idShort"] == groupIdShort)
            {
                foreach (JSONNode prop in node["value"].AsArray)
                    if (prop["idShort"] == coord)
                        return prop["value"];
            }
        }
        return "0";
    }

    /// <summary>
    /// Constructs a Vector3 from X, Y, Z values in a SubmodelElementCollection.
    /// </summary>
    /// <param name="valueArray">JSONArray containing data</param>
    /// <param name="groupIdShort">Group identifier</param>
    /// <param name="x">X field</param>
    /// <param name="y">Y field</param>
    /// <param name="z">Z field</param>
    /// <returns>Vector3 composed of parsed values</returns>
    private Vector3 GetVector3(JSONArray valueArray, string groupIdShort, string x, string y, string z)
    {
        return new Vector3(
            ParseFloat(GetNestedValue(valueArray, groupIdShort, x)),
            ParseFloat(GetNestedValue(valueArray, groupIdShort, y)),
            ParseFloat(GetNestedValue(valueArray, groupIdShort, z)));
    }

    /// <summary>
    /// Sets the header and main text of the dialog using TextMeshPro components.
    /// </summary>
    /// <param name="dialog">Dialog GameObject</param>
    /// <param name="header">Header text</param>
    /// <param name="main">Main body text</param>
    /// <param name="fontSize">Font size to apply</param>
    private void SetDialogText(GameObject dialog, string header, string main, float fontSize)
    {
        Transform headerText = dialog.transform.Find("Canvas/Header");
        if (headerText != null && headerText.GetComponent<TMPro.TextMeshProUGUI>() != null)
        {
            var tmp = headerText.GetComponent<TMPro.TextMeshProUGUI>();
            tmp.text = header;
            tmp.fontSize = fontSize;
        }

        Transform mainText = dialog.transform.Find("Canvas/Main Text");
        if (mainText != null && mainText.GetComponent<TMPro.TextMeshProUGUI>() != null)
        {
            var tmp = mainText.GetComponent<TMPro.TextMeshProUGUI>();
            tmp.text = main;
            tmp.fontSize = fontSize;
        }
    }

    /// <summary>
    /// Configures the ContentSizeFitter component of the dialog's canvas.
    /// </summary>
    /// <param name="dialog">Dialog GameObject</param>
    private void SetContentFitter(GameObject dialog)
    {
        Transform canvas = dialog.transform.Find("Canvas");
        if (canvas != null && canvas.GetComponent<ContentSizeFitter>() != null)
        {
            var fitter = canvas.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }
    }

    /// <summary>
    /// Starts OPC UA (MQTT) Subscriber for all the NodeIds found
    /// </summary>
    private void StartOpcUaSubscription()
    {
        _subscriber = new OpcUaSubscriber();

        _subscriber.OnMessageReceived += (nodeId, value) =>
        {
            if (nodeId == "ns=4;i=100") 
            {
                long currentTimeStamp = Stopwatch.GetTimestamp();
                if (_lastJitterTimeStamp > 0) 
                {
                    double elapsedMs = (currentTimeStamp - _lastJitterTimeStamp) * 1000.0 / Stopwatch.Frequency;
                    Debug.Log($"[SCI_METRIC] Network Jitter | Node: {nodeId} | Elapsed: {elapsedMs:F2}");
                }
                _lastJitterTimeStamp = currentTimeStamp;
            }
            Debug.Log($"Received from OPC UA: NodeId={nodeId}, Value={value.ToString()}");
            _uiQueue.Enqueue((nodeId, value.ToString()));
        };

        try
        {
            string brokerUrl = !string.IsNullOrEmpty(ConfigLoader.MqttBrokerUrl) ? ConfigLoader.MqttBrokerUrl : BrokerUrl;
            _subscriber.Subscribe(brokerUrl, Topic, TargetPublisherId, _nodeIdsToSubscribe);
            Debug.Log($"[OPC UA] Subscribing of {_nodeIdsToSubscribe.Count} variables.");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[OPC UA] Error during the subscribing: {ex.Message}");
        }
    }

    /// <summary>
    /// Update() transforms data from OPC UA to text in the GUI elements
    /// </summary>
    private void Update()
    {
        if (_uiQueue.IsEmpty) return;

        // Stopwatch for analysis and logging
        Stopwatch frameTimer = Stopwatch.StartNew();
        int processedMessages = 0;

        while (_uiQueue.TryDequeue(out var data))
        {
            if (nodeIdToTextMap.TryGetValue(data.nodeId, out var uiTextComponent))
            {
                if (uiTextComponent != null)
                {
                    uiTextComponent.text = data.value;
                    processedMessages++;
                }
            }
        }
        frameTimer.Stop();
        if (processedMessages > 0)
        {
            Debug.Log($"[SCI_METRIC] OPC UA Process Queue | Processed {processedMessages} messages | Time: {frameTimer.Elapsed.TotalMilliseconds:F4}ms");
        }
    }

    /// <summary>
    /// Correct stoppage of MQTT connection
    /// </summary>
    private void OnDisable()
    {
        if (_subscriber != null)
        {
            _subscriber.Stop();
            Debug.Log("[OPC UA] Connection stopped.");
        }
    }
}




