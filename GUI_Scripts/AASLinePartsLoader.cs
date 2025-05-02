/// <summary>
/// Script for loading and managing positions of line parts from an AAS submodel.
/// </summary>
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using SimpleJSON;
using System.Collections.Generic;

public class AASLinePartsLoader : MonoBehaviour
{
    //[SerializeField]
    //private string linePartsAasUrl = "http://192.168.1.36:8081/submodels/TGluZVBhcnRzX1N1Ym1vZGVs/submodel-elements";

    // URL for GET from Eclipse Basyx Server - IP of the server with port 8081, based on the config file + /submodels/ + Id of the given Submodel (Id (Base64)) + /submodel-elements => whole URL as string 
    private string LinePartsAasUrl => ConfigLoader.ServerBaseUrl + "/submodels/" + ConfigLoader.LinePartsId + "/submodel-elements";


    /// <summary>
    /// Dictionary mapping each LinePart's idShort to its world position.
    /// </summary>
    public Dictionary<string, Vector3> LinePartsPositions { get; private set; } = new();

    /// <summary>
    /// Coroutine that loads all line part positions from the specified AAS submodel endpoint.
    /// </summary>
    /// <returns>IEnumerator for coroutine execution</returns>
    public IEnumerator LoadLineParts()
    {
        using (UnityWebRequest request = UnityWebRequest.Get(LinePartsAasUrl))
        {
            request.SetRequestHeader("Accept", "application/json");
            request.downloadHandler = new DownloadHandlerBuffer();
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                JSONNode root = JSON.Parse(request.downloadHandler.text);
                JSONArray topLevel = root["result"].AsArray;

                // Recursively extract all valid line parts and store their positions
                foreach (JSONNode node in topLevel)
                    ExtractLinePartsRecursive(node);

                Debug.Log($"Loaded {LinePartsPositions.Count} LineParts.");
            }
            else
            {
                Debug.LogError("URL targeted: " + LinePartsAasUrl);
                Debug.LogError($"Failed to load LineParts: {request.responseCode} - {request.error}");
            }
        }
    }

    /// <summary>
    /// Recursively traverses the AAS structure to find SubmodelElementCollections representing line parts.
    /// </summary>
    /// <param name="node">JSONNode representing a current part of the structure</param>
    private void ExtractLinePartsRecursive(JSONNode node)
    {
        if (node == null) return;

        // Check for SubmodelElementCollection and extract PosX/Y/Z
        if (node["modelType"] == "SubmodelElementCollection")
        {
            string idShort = node["idShort"];
            JSONArray valueArray = node["value"].AsArray;

            float x = ParseFloat(GetValue(valueArray, "PosX"));
            float y = ParseFloat(GetValue(valueArray, "PosY"));
            float z = ParseFloat(GetValue(valueArray, "PosZ"));

            LinePartsPositions[idShort] = new Vector3(x, y, z);

            Debug.Log($"LinePart loaded: {idShort} @ ({x}, {y}, {z})");
        }

        // Recurse through nested 'statements' structures (e.g., Entities)
        if (node["statements"] != null)
        {
            foreach (JSONNode child in node["statements"].AsArray)
                ExtractLinePartsRecursive(child);
        }
    }

    /// <summary>
    /// Retrieves the value from a JSON property based on idShort.
    /// </summary>
    /// <param name="arr">JSONArray of properties</param>
    /// <param name="idShort">Target identifier</param>
    /// <returns>String value or "0" if not found</returns>
    private string GetValue(JSONArray arr, string idShort)
    {
        foreach (JSONNode prop in arr)
            if (prop["idShort"] == idShort)
                return prop["value"];
        return "0";
    }

    /// <summary>
    /// Attempts to safely parse a float from string input.
    /// </summary>
    /// <param name="value">String to parse</param>
    /// <param name="fallback">Fallback value if parsing fails</param>
    /// <returns>Parsed float value or fallback</returns>
    private float ParseFloat(string value, float fallback = 0f)
    {
        return float.TryParse(value, out float result) ? result : fallback;
    }
}

