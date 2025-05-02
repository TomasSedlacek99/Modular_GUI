/// <summary>
/// Represents a simulated OPC UA variable node based on mandatory attributes from OPC UA specification.
/// </summary>
[System.Serializable]
public class SimulatedVariableNode
{
    public string NodeId;           // Unique identifier of the variable node
    public double Value;            // The current value of the node
    public string DisplayName;      // Human-readable name
    public string BrowseName;       // Browse name used in the OPC UA model
    public string NodeClass;        // Typically "Variable"
    public string DataType;         // Data type of the variable (e.g., Double)
    public string TypeDefinition;   // Type definition (e.g., BaseDataVariableType)
    public string DataTypeDefinition; // More detailed data type description
    public string Description;      // Description of the variable
    public int ValueRank;           // Indicates the dimensionality (e.g., scalar = -1)
    public bool IsHistorizing;      // Indicates if the variable supports history
    public byte AccessLevel;        // Bit field defining read/write access
}


