namespace RunningGearTracker_AquinoE.Model
{
    public class TrainingSessions
    {
        public int SessionID { get; set; }
        public string ActivityDate { get; set; }
        public string Location { get; set; }
        public double Distance_KM { get; set; }
        public string Duration { get; set; }
        public string AvgHeartRate { get; set; }
    }
}
