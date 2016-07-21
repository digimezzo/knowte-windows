namespace Knowte.Core.Logging
{
    public interface ILogClient
    {
        string LogFile { get; set; }
        NLog.Logger Logger { get; set; }
    }
}