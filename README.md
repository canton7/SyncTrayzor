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

Grab the latest installer from the [releases](https://github.com/canton7/SyncTrayzor/releases) tab.


Building from Source
--------------------

You'll need [Visual Studio 2013](http://www.visualstudio.com/en-us/news/vs2013-community-vs.aspx).
Clone/download the repository, open `src\SyncTrayzor.sln`, and compile.
For Debug builds, you'll need to [download syncthing.exe](https://github.com/syncthing/syncthing/releases) and place it in the `bin\x86\Debug` or `bin\x64\Debug` folder, as appropriate.
For Release builds, you'll need to place it in `%APPDATA%\SyncTrayzor`. 