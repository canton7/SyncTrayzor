SyncTrayzor
===========

Introduction
------------

SyncTrayzor is a little tray utility for [Syncthing](http://syncthing.net/) on Windows.
It hosts and wraps Syncthing, making it behave more like a native Windows application and less like a command-line utility with a web browser interface.

Features include:

 - Has a built-in web browser, so you don't need to fire up an external browser.
 - Optionally starts on login, so you don't need to set up Syncthing as a service.
 - Can watch your folders for changes, and informs Syncthing about them instantly. This means you can have large polling intervals, but still have changes propagated straight await.
 - Optional tray messages when folders have finished syncing.
 - Tray icon indicates when synchronization is occurring.


![Screenshot](readme/screenshot.png)

Installation
------------

SyncTrayzor is packged as both an installer and a standalone zip.

### Installer

Grab and run the latest installer from the [releases](https://github.com/canton7/SyncTrayzor/releases) tab.
If you already have SyncTrayzor installed, this will update it.

### Standalone

First, you'll need .net 4.5. [Download the offline](http://www.microsoft.com/en-gb/download/details.aspx?id=42642) or
[web installer](http://www.microsoft.com/en-gb/download/details.aspx?id=42643) if you don't have it installed already.

Grab the latest standalone .zip from the [releases](https://github.com/canton7/SyncTrayzor/releases) tab.
Unzip, and run `SyncTrayzor.exe`. If you're updating, you'll need to copy the `data` folder across from your previous standalone installation.


What will SyncTrayzor do to my system?
--------------------------------------

Good question. The answer depends on whether you installed SyncTrayzor using the installer, or are running it standalone.

### Installer

SyncTrayzor will install itself into `C:\Program Files\SyncTrayzor`. 

By default, SyncTrayzor will put its own configuration in `C:\Users\<You>\AppData\Roaming\SyncTrayor`, and let Syncthing use its default folder for its database, which is `C:\Users\<You>\AppData\Local\Syncthing`.
It will also create a registry key at `HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run\SyncTrayzor`, which will let it start when you log in.

You can delete this registry key by unchecking "Automatically start on login" in the settings.

If you check "Use custom home directory or Syncthing" in the settings, then SyncTrayzor will tell Syncthing to use `C:\Users\<You>\AppData\Local\SyncTrayzor\syncthing` for its database.
This is useful if you want to keep a system copy of Syncthing, and the Syncthing managed by SyncTrayzor, separate.

### Standalone

SyncTrayzor will put its own configuration in `SyncTrayzorPortable\data`, and tell Syncthing to use `SyncTrayzorPortable\data\syncthing` for its database.
This means that, when upgrading, you can simply move the 'data' folder over to move all your settings, and database.
If you uncheck "Use custom home directory or Syncthing" in the settings, then Syncthing will use its default folder for its database, which is `C:\Users\<You>\AppData\Local\Syncthing`.

The portable version won't start on login by default. If you check "Automatically start on login" in the settings, then a registry key will be created at `HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run\SyncTrayzor`.


Building from Source
--------------------

You'll need [Visual Studio 2013](http://www.visualstudio.com/en-us/news/vs2013-community-vs.aspx).
Clone/download the repository, open `src\SyncTrayzor.sln`, and compile.
You'll also need to [download syncthing.exe](https://github.com/syncthing/syncthing/releases) and place it in the `bin\x86\Debug`, `bin\x64\Debug`, `bin\x86\Release`, or `bin\x64\Release` folder as appropriate.