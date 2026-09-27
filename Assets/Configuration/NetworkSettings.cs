using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class NetworkSettings
{
    /// <summary>Name of the profile currently in use. Must match a Name in Profiles.</summary>
    public string ActiveProfile = "Local";

    public List<ServerProfile> Profiles = new()
    {
        new ServerProfile("Local", "http://127.0.0.1:8000", DatabaseMode.Local),
        new ServerProfile("Ngrok", "https://your-tunnel-id.ngrok-free.app", DatabaseMode.Remote),
    };

    public ServerProfile GetActiveProfile()
    {
        return Profiles.FirstOrDefault(p => p.Name == ActiveProfile) ?? Profiles.FirstOrDefault();
    }

    public ServerProfile GetProfile(string name)
    {
        return Profiles.FirstOrDefault(p => p.Name == name);
    }
}
