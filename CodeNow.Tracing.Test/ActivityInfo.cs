namespace CodeNow.Tracing
{
    public class ActivityInfo
    {
        public string? TraceId { get; set; }
        
        public string? SpanId { get; set; }
        
        public string? ParentId { get; set; }
    }
}