namespace RunningGearTracker_AquinoE.Model
{
    public class GearUsage
    {
        public int UsageID { get; set; }
        public Gear Gear { get; set; }
        public int GearID { get; set; }
        public TrainingSessions TrainingSessions { get; set; }
        public int SessionID { get; set; }
        public string Notes { get; set; }
    }
}
