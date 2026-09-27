/// <summary>
/// Which mode the currently active server profile operates in.
/// Local:  ConfigurationManager will attempt to launch/verify a locally hosted DB server process.
/// Remote: ServerURL is treated as an already-running, externally reachable server
///         (a colleague's machine, a centralized server, or an ngrok tunnel).
/// </summary>
public enum DatabaseMode
{
    Local,
    Remote
}
