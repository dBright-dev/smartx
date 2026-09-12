namespace SmartX.Client.Models
{
    public class SensorDevice
    {
        public string MacAddress { get; set; } = string.Empty;
        public string DeploymentLocation { get; set; } = string.Empty;
        public string Zone { get; set; } = string.Empty;
        public string NodeId { get; set; } = string.Empty;
        public SensorCategory Category { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
        public string? ConfigurationFilePath { get; set; }
    }

    public enum SensorCategory
    {
        Environmental = 0,
        PowerConsumption = 1,
        Actuator = 2
    }
}
