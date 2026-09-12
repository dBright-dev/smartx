namespace SmartX.API.Services
{
    public class DeploymentTreeValidator
    {
        public bool ValidateLocation(string locationPath)
        {
            var segments = locationPath.Split('>').Select(s => s.Trim()).ToArray();
            return ValidateSegment(segments, 0);
        }

        private bool ValidateSegment(string[] segments, int index)
        {
            // Base Case: All segments validated
            if (index >= segments.Length) return true;

            // Validate current segment (e.g., check for empty strings or invalid characters)
            if (string.IsNullOrWhiteSpace(segments[index])) return false;

            // Recursive Call: Move to the next segment
            return ValidateSegment(segments, index + 1);
        }
    }
}
