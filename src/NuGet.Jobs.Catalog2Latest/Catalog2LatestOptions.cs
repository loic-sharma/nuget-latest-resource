using System;

namespace NuGet.Jobs.Catalog2Latest
{
    public class Catalog2LatestOptions
    {
        public string IndexPath { get; set; }

        public DateTimeOffset DefaultMinCursor { get; set; } = DateTimeOffset.MinValue;

        public int SleepDurationSeconds { get; set; } = 30;

        public int? MaxPages { get; set; } = null;

        public int ProducerWorkers { get; set; } = 32;
        public int ConsumerWorkers { get; set; } = 32;
    }
}
