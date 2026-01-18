using Newtonsoft.Json.Linq;
using StarRuptureSaveFixer.Models;
using StarRuptureSaveFixer.Utils;

namespace StarRuptureSaveFixer.Fixers;

/// <summary>
/// Removes all drone entities from the save file
/// </summary>
public class DroneRemover : IFixer
{
    public string Name => "Drone Remover";

    public bool ApplyFix(SaveFile saveFile)
    {
        ConsoleLogger.Info($"Applying fix: {Name}");

        try
        {
            // Parse the JSON using Newtonsoft.Json
            JObject root = JObject.Parse(saveFile.JsonContent);

            // Navigate to itemData > Mass > entities
            JToken? itemData = root["itemData"];
            if (itemData == null)
            {
                ConsoleLogger.Warning("'itemData' not found in JSON.");
                return false;
            }

            JToken? mass = itemData["Mass"];
            if (mass == null)
            {
                ConsoleLogger.Warning("'Mass' not found in itemData.");
                return false;
            }

            JObject? entities = mass["entities"] as JObject;
            if (entities == null)
            {
                ConsoleLogger.Warning("'entities' not found in Mass.");
                return false;
            }

            // First pass: count total entities to scan
            int totalEntities = entities.Properties().Count();
            ConsoleLogger.Info($"Scanning {totalEntities} entities for drones.");
            ConsoleLogger.Plain("");

            // Find all drone entities
            List<string> dronesToDelete = new();
            char[] spinner = new[] { '|', '/', '-', '\\' };
            int spinnerIndex = 0;
            int entitiesScanned = 0;

            foreach (var entity in entities.Properties().ToList())
            {
                entitiesScanned++;
                string entityKey = entity.Name;
                JToken entityValue = entity.Value;

                // Display progress with spinner
                Console.Write($"\rProcessing {entitiesScanned}/{totalEntities} entities {spinner[spinnerIndex]}");
                spinnerIndex = (spinnerIndex + 1) % spinner.Length;

                // Check if this is a drone entity
                if (IsDroneEntity(entityValue))
                {
                    dronesToDelete.Add(entityKey);
                }
            }

            // Clear the progress line
            Console.Write("\r" + new string(' ', 50) + "\r");
            ConsoleLogger.Info($"Found {dronesToDelete.Count} drone(s) to remove.");

            // Delete all drones
            if (dronesToDelete.Count > 0)
            {
                foreach (string droneKey in dronesToDelete)
                {
                    entities.Remove(droneKey);
                }

                // Serialize back to JSON without formatting
                saveFile.JsonContent = root.ToString(Newtonsoft.Json.Formatting.None);

                ConsoleLogger.Success($"Successfully removed {dronesToDelete.Count} drone(s).");
                return true;
            }
            else
            {
                ConsoleLogger.Info("No drones found in save file.");
                return false;
            }
        }
        catch (Newtonsoft.Json.JsonException ex)
        {
            ConsoleLogger.Error($"JSON parsing error: {ex.Message}");
            return false;
        }
        catch (Exception ex)
        {
            ConsoleLogger.Error($"Error during drone removal: {ex.Message}");
            ConsoleLogger.Error($"Stack trace: {ex.StackTrace}");
            return false;
        }
    }

    /// <summary>
    /// Checks if an entity is a drone based on the entityConfigDataPath
    /// </summary>
    private bool IsDroneEntity(JToken entity)
    {
        try
        {
            // Check for spawnData.entityConfigDataPath
            JToken? spawnData = entity["spawnData"];
            if (spawnData == null)
                return false;

            JToken? configPath = spawnData["entityConfigDataPath"];
            if (configPath == null)
                return false;

            string configPathValue = configPath.ToString();
            return configPathValue == "/Game/Chimera/Drones/DA_RailDroneConfig.DA_RailDroneConfig";
        }
        catch
        {
            return false;
        }
    }
}
