/// <summary>
/// Simulates OPC UA variable updates and reflects the value into associated UI text fields.
/// </summary>
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class SimulatedDataUpdater : MonoBehaviour
{
    /// <summary>
    /// Time interval (in seconds) at which variable values are updated.
    /// </summary>
    [SerializeField] private float updateInterval;

    /// <summary>
    /// Internal timer used to track elapsed time between updates.
    /// </summary>
    private float timer = 0f;

    /// <summary>
    /// List of all simulated variable nodes that will be updated.
    /// These are either assigned manually or generated dynamically by another script (e.g., AASFetcher).
    /// </summary>
    public List<SimulatedVariableNode> variables = new();

    /// <summary>
    /// Unity Update method. Called once per frame.
    /// Increments timer and triggers simulated data updates based on updateInterval.
    /// </summary>
    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= updateInterval)
        {
            timer = 0f;
            SimulateTick();
        }
    }

    /// <summary>
    /// Iterates over each simulated variable, changes its value, and updates the corresponding TextMeshProUGUI field.
    /// </summary>
    private void SimulateTick()
    {
        foreach (var variable in variables)
        {
            // Simulate small change in value for demonstration
            variable.Value += Random.Range(-0.5f, 0.5f);

            // If UI element exists for this variable, update its text
            if (AASFetcher.nodeIdToTextMap.TryGetValue(variable.NodeId, out TextMeshProUGUI textComponent))
            {
                textComponent.text = variable.Value.ToString("F2") + " °C"; // Optional formatting with unit
            }
        }
    }
}

