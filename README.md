### PrivateServerMod | A VRChat client modification to connect to [Shoya](https://gitlab.com/george/shoya-go) & [Naoka](https://gitlab.com/george/naoka-ng)
This is a VRChat mod that makes the client connect to an emulated server safely.

---

#### Usage Warnings
As outlined in the [End-User documentation for Shoya](https://gitlab.com/george/shoya-go/-/blob/master/docs/End-Users/README.md), Only connect to servers that are operated by people you **absolutely trust!**<br/>
There is no way to validate what a server is running on their end, which means that your account information could be compromised by a malicious actor.

With that in mind, exercise caution & do your due diligence; Use a randomly-generated password for each different server you connect to!

#### Installation
* Step 1: [Install MelonLoader](https://melonwiki.xyz) -- If you need help, you can ask in the [VRChat Modding Group Discord](https://discord.gg/vrcmg).
* Step 2: Drop the `PrivateServer.dll` file into your `Mods` directory.
* Step 3: Launch VRChat at least once for the configuration to be generated in your `UserData/MelonPreferences.cfg` file.

#### Configuration
The configuration for this mod is fairly trivial, and it even features a nifty automatic configuration system based on server-side values!

Configuration options:
 - `Enabled` (boolean): Whether this mod will patch *anything* at all. If this is set to `false`, this mod does not do **anything.**
 - `AutoConfig` (boolean): Whether the automatic configuration is activated.
 - `AutoConfigUrl` (string): The URL to request server configuration details from.
 - `ApiUrl` (string): The URL that the Shoya instance runs on, inclusive of the trailing slash.
 - `WebsocketUrl` (string): The URL that Shoya's websocket event pipeline runs on.
 - `NameServerHost` (string): The host (FQDN) that the Photon NameServer runs on.

**NOTE:** If you are running VRChat under Wine/Proton, MelonLoader will **not** automatically block analytics. Update your `/etc/hosts` file with the following before connecting to a private server:
```
0.0.0.0 cdp.cloud.unity3d.com perf-events.cloud.unity3d.com config.uca.cloud.unity3d.com
0.0.0.0 api.amplitude.com api2.amplitude.com
```

#### Disclaimer
This project is not owned by, affiliated with, or endorsed by VRChat, inc.

The use of client modifications is against VRChat's Terms of Service.