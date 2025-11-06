# Network Plugin Provider
[![Auto build](https://github.com/DKorablin/Plugin.NetworkPluginProvider/actions/workflows/release.yml/badge.svg)](https://github.com/DKorablin/Plugin.NetworkPluginProvider/releases/latest)

A plugin provider for the SAL (Software Application Layer) framework that enables automatic downloading, updating, and loading of plugins from remote servers.

## Features

- ✅ **Automatic Plugin Updates**: Downloads and updates plugins from a remote server automatically
- ✅ **Version Management**: Tracks plugin versions and downloads updates when available
- ✅ **Dependency Resolution**: Handles plugin dependencies and referenced assemblies
- ✅ **Multi-Path Support**: Search for plugins in multiple directories
- ✅ **HTTP/HTTPS Support**: Download plugins from web servers
- ✅ **Local File Support**: Support for local network paths
- ✅ **Proxy Support**: Configurable proxy settings with default credentials support
- ✅ **XML-Based Configuration**: Simple XML format for plugin manifest
- ✅ **Multi-Framework**: Supports .NET Framework 3.5 and .NET Standard 2.0

## Installation
To install the Network Plugin Provider Plugin, follow these steps:
1. Download the latest release from the [Releases](https://github.com/DKorablin/Plugin.Winlogon/releases)
2. Extract the downloaded ZIP file to a desired location.
3. Use the provided [Flatbed.Dialog (Lite)](https://dkorablin.github.io/Flatbed-Dialog-Lite) executable or download one of the supported host applications:
	- [Flatbed.Dialog](https://dkorablin.github.io/Flatbed-Dialog)
	- [Flatbed.MDI](https://dkorablin.github.io/Flatbed-MDI)
	- [Flatbed.MDI (WPF)](https://dkorablin.github.io/Flatbed-MDI-Avalon)
	- [Flatbed.WorkerService](https://dkorablin.github.io/Flatbed-WorkerService)

## Quick Start

### 1. Create Local Configuration File

Create a file named `Plugins.Network.xml` in your application directory:

```xml
<?xml version="1.0" encoding="utf-8" ?>
<Plugins Path="https://api.yourserver.com/plugins/">
</Plugins>
```

The `Path` attribute should point to the remote server location where the server-side plugin manifest is located.

### 2. Create Server-Side Plugin Manifest

On your server, create a `Plugins.Network.xml` file that describes available plugins:

```xml
<?xml version="1.0" encoding="utf-8" ?>
<Plugins Path="https://api.yourserver.com/plugins/">
    <Plugin Name="YourPlugin" Path="YourPlugin.dll" Description="Your plugin description" Version="1.0.0.0"/>
    
    <!-- Plugin with dependencies -->
    <Plugin Name="AdvancedPlugin" Description="Plugin with dependencies" Version="1.0.1.0">
        <Assembly Name="Dependency1" Description="Required dependency" Version="1.0.0.0"/>
        <Assembly Name="Dependency2" Description="Another dependency" Version="2.0.0.0"/>
    </Plugin>
</Plugins>
```

### 3. Deploy Plugin Files

Upload your plugin DLL files to the same server directory specified in the `Path` attribute.

## Configuration

### Command Line Arguments

Use the `SAL_Path` command line argument to specify plugin search directories (separated by semicolons):

```bash
YourApp.exe /SAL_Path="C:\Plugins;D:\SharedPlugins;E:\NetworkPlugins"
```

If not specified, the current application directory is used as the default.

### Plugin Settings

The Network Plugin Provider exposes the following settings through the SAL settings interface:

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `RebuildXmlFile` | Boolean | false | Regenerates the XML manifest from currently loaded plugins (creates a copy) |
| `UseDefaultCredentials` | Boolean | false | Use default Windows credentials for proxy authentication |

## How It Works

### Update Process

1. **Initial Check**: On startup, the plugin provider scans the directories specified in `SAL_Path`
2. **Local Validation**: Checks if all plugins listed in the local `Plugins.Network.xml` exist
3. **Remote Check**: Requests the `Last-Modified` HTTP header from the server to detect changes
4. **Download Updates**: If the server version is newer or files are missing:
   - Downloads missing plugins
   - Updates outdated plugins (version comparison)
   - Removes obsolete plugins no longer in the server manifest
5. **Local Sync**: Updates the local XML file with the server configuration
6. **Plugin Loading**: Loads all plugins from the local directory

### Version Comparison

Plugins are updated based on version comparison:
- Server version **greater than** local version → Download update
- Plugin exists on server but not locally → Download new plugin
- Plugin exists locally but not on server → Remove obsolete plugin

### Dependency Resolution

When a plugin has dependencies (referenced assemblies), the provider:
1. Downloads the main plugin DLL
2. Downloads all referenced assemblies listed in the `<Assembly>` elements
3. Handles assembly resolution for these dependencies at runtime

## XML Schema

### Plugins Element

```xml
<Plugins Path="[URL or local path]">
    <!-- Plugin definitions -->
</Plugins>
```

**Attributes:**
- `Path` (required): URL or local path to the plugin repository. Must end with `/` or `\` for directories.

### Plugin Element

```xml
<Plugin Name="[PluginName]" Path="[Optional]" Description="[Optional]" Version="[Required]">
    <!-- Optional assembly references -->
</Plugin>
```

**Attributes:**
- `Name` (required): Unique plugin identifier
- `Version` (required): Plugin version (e.g., "1.0.0.0")
- `Path` (optional): Relative path to the plugin DLL. Defaults to `{Name}.dll`
- `Description` (optional): Human-readable description

### Assembly Element

```xml
<Assembly Name="[AssemblyName]" Path="[Optional]" Description="[Optional]" Version="[Required]"/>
```

**Attributes:**
- `Name` (required): Assembly name
- `Version` (required): Assembly version
- `Path` (optional): Relative path to the assembly. Defaults to `{Name}.dll`
- `Description` (optional): Assembly description

## Examples

### Example 1: Simple Plugin Configuration

**Server-side `Plugins.Network.xml`:**
```xml
<?xml version="1.0" encoding="utf-8" ?>
<Plugins Path="https://cdn.example.com/app-plugins/">
    <Plugin Name="Kernel.Empty" Path="Kernel.Empty.dll" 
            Description="Empty kernel library for generic host" 
            Version="1.0.0.0"/>
    <Plugin Name="Plugin.Autorun" 
            Description="Autostart application after system starts" 
            Version="1.0.0.0"/>
</Plugins>
```

### Example 2: Plugin with Dependencies

```xml
<?xml version="1.0" encoding="utf-8" ?>
<Plugins Path="https://cdn.example.com/app-plugins/">
    <Plugin Name="Plugin.RDP" 
            Description="Remote Desktop Protocol Client" 
            Version="1.0.1.0">
        <Assembly Name="Interop.MSTSCLib" 
                  Description="Assembly imported from type library 'MSTSCLib'." 
                  Version="1.0.0.0"/>
        <Assembly Name="AxInterop.MSTSCLib" 
                  Description="Assembly imported from type library 'MSTSCLib'." 
                  Version="1.0.0.0"/>
    </Plugin>
</Plugins>
```

### Example 3: Local Network Path

```xml
<?xml version="1.0" encoding="utf-8" ?>
<Plugins Path="\\FileServer\SharedPlugins\">
    <Plugin Name="Plugin.Custom" Version="2.0.0.0"/>
</Plugins>
```

### Example 4: Using Proxy Settings

```csharp
// Access plugin settings through SAL framework
var networkProvider = host.Plugins.GetProvider<NetworkPluginProvider>();
var settings = host.Plugins.Settings(networkProvider).LoadAssemblyParameters<PluginSettings>();

// Enable proxy with default credentials
settings.UseDefaultCredentials = true;
```

## Advanced Usage

### Rebuilding Plugin Manifest

To generate a new XML manifest from currently loaded plugins:

```csharp
var settings = GetPluginSettings(); // Your method to access settings
settings.RebuildXmlFile = true; // Triggers manifest rebuild
```

This creates a new XML file with a unique name (e.g., `Plugins.Network(1).xml`) containing all currently loaded plugins.

### Custom Assembly Resolution

The Network Plugin Provider implements `IPluginProvider.ResolveAssembly()` to handle assembly resolution for downloaded dependencies. It searches all configured plugin directories for matching assemblies.

## Troubleshooting

### Plugins Not Updating

1. Check network connectivity to the server
2. Verify the `Path` attribute in the local XML points to the correct server location
3. Check file permissions on the local plugin directory
4. Enable trace logging to see detailed error messages

### HTTP 401/403 Errors

If you encounter authentication errors:
- Set `UseDefaultCredentials = true` in plugin settings
- Verify server authentication requirements
- Check proxy configuration

### Assembly Loading Errors

- Ensure all referenced assemblies are listed in the XML manifest
- Verify assembly versions match between manifest and actual files
- Check that .NET Framework version is compatible

## Architecture

The Network Plugin Provider consists of several key components:

- **`Plugin`**: Main plugin provider implementation
- **`UpdateBll`**: Business logic for update detection and download
- **`UpdateInfo`**: XML parsing and serialization
- **`PluginInfo`**: Plugin metadata container
- **`PluginLoader`**: File management and loading
- **`BinaryWebRequest`**: HTTP download utility

## Requirements

- **SAL.Flatbed**: Core framework for plugin architecture
- **.NET Framework 3.5** or **.NET Standard 2.0** or higher
- Network access to plugin repository (for remote updates)

## Contributing

Contributions are welcome! Please feel free to submit issues or pull requests.