namespace SmartX.API.Services
{
    public class TelemetryBatchProcessor
    {
        private readonly List<double> _processedReadings = new();

        public void ProcessBatch(double[][] rawBatch)
        {
            // rawBatch is a jagged array, e.g., [[1.0, 2.0], [3.0], [4.0, 5.0, 6.0]]
            foreach (var batch in rawBatch)
            {
                foreach (var reading in batch)
                {
                    _processedReadings.Add(reading);
                }
            }
        }
    }
}
