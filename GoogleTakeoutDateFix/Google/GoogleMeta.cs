namespace GoogleTakeoutDateFix.Google;

public class GoogleMeta
{
    public string title { get; set; }
    public string description { get; set; }
    public GoogleTimeMeta creationTime { get; set; }
    public GoogleTimeMeta photoTakenTime { get; set; }
}

public class GoogleTimeMeta
{
    public string timestamp { get; set; }
    public string formatted { get; set; }
}