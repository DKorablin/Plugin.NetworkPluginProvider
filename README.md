# Network Plugin Provider
This plugin implements the ability to update and load plugins from remote location. For this purposes XML files is used which will be downloaded from server and stored in local folder.

Plugin is using SAL_Path command line argument with «;» to pass a list of directories to search for XML file Plugins.Network.xml and plugins to load that described in this file. If command line argument is not specified, then current folder will be used as a base folder.

To deploy plugins on the client machine, just add XML file Plugins.Network.xml (Sample file available in the archive) with the following content:


    <?xml version="1.0" encoding="utf-8" ?>
    <Plugins Path="https://api.server.com/SAL_Plugins/">
    </Plugins>

The path specified in the Path attribute of the Plugins node must contain an absolute link to the filter on the remote server where similar XML file will be located. And the same folder will be used to search for updated plugins described in XML file. Before files will be downloaded 2 checks will be performed:

 1. Sync XML file with contains of current folder and if some files are missing they will be downloaded from remote server.
 2. Request Last-Modified HTTP header for XML file on server. If change date on server is more than at local machine then updated file will be downloaded from server.

After all plugins are downloaded plugin provider will update local XML file with remote file from server. And after each launch will start to compare local file with remote.

## Server file example


    <?xml version="1.0" encoding="utf-8" ?>
    <Plugins Path="http://api.server.com/SAL_Plugins/">
        <Plugin Name="Kernel.Empty" Path="Kernel.Empty.dll" Description="Empty kernel library for generic host" Version="1.0.0.0"/>
        <Plugin Name="Plugin.RDP" Description="Remote Desktop Protocol Client" Version="1.0.1.0">
            <Assembly Name="Interop.MSTSCLib" Description="Assembly imported from type library 'MSTSCLib'." Version="1.0.0.0"/>
            <Assembly Name="AxInterop.MSTSCLib" Description="Assembly imported from type library 'MSTSCLib'." Version="1.0.0.0"/>
        </Plugin>
        <Plugin Name="Plugin.Autorun" Description="Autostart application after system starts" Version="1.0.0.0"/>
    </Plugins>