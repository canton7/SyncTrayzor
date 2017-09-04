Changelog
=========

v1.1.18
-------

 - Fix uninstaller crash if someone filled out the survey while not connected to the internet

v1.1.17
-------

 - Don't store Syncthing's API key in config, and don't log it
 - Fix filesystem notifications when the file contained non-ASCII characters (#400)
 - Don't show device connected/disconnected notifications if a device is reconnecting a lot
 - Don't watch / raise notifications about new folders if no existing folders are watched / have notifications (#393)
 - Don't write to the disk as much by default (#370)
 - Fix crash on the settings screen
 - Be more reslient to weird registry permissions, fixing crash (#378)
 - Fix crash when calculating data transfer stats (#380)
 - Be more reslient when trying to find a free port for Syncthing to use (#381)
 - Add installer command-line flags (for system administrators) (#371, #402)
 - Add an exit poll to the uninstaller

v1.1.16
-------

 - Fix some crashes on startup
 - Fix bug where 'show logs' link on the crash screen would cause another crash
 - Reduce how often SyncTrayzor checks for updates

v1.1.15
-------

 - Add a network usage graph to the tray icon popup
 - Add command-line parameters to start and stop Syncthing programatically
 - Fix problems setting up auto-start on some machines
 - Support custom file browsers (instead of Explorer)

v1.1.14
-------

 - Support selecting and resolving multiple conflicts at once
 - Add better support for changes to Syncthing's event format
 - UI tweaks and improvements
 - Fix a few crashes
 - Better support for right-to-left languages

v1.1.13
-------

 - Improve 'Browse' button next to folder selection input (#297)
 - Fix right-click context menu in embedded browser (#300)
 - Fix crash on conflict resolution screen when Windows can't find an icon for the file type (#301)
 - Fix crash when opening SyncTrayzor (#303, #306, #318)
 - Indication of file conflict in tray icon didn't disappear in some cases (#307)
 - (Hopefully) add workaround for Chinese IME not working (#314)
 - Display folder names instead of folder IDs in tray notifications (#315)
 - Rename 'Restore' option in tray icon context menu (#320)

v1.1.12
-------

 - No functional change. Release is so that new users get Syncthing 0.14 by default.

v1.1.11
-------

 - Display folder labels instead of folder IDs (#284)
 - Fix issue where 'Open Folder' and 'Browse' buttons might not be displayed (#281)
 - Make F5 clear the browser cache, helping with issues were Syncthing's GUI assets have been incorrectly cached
 - Don't spam connected/disconnected events if they happen too quickly (e.g. if a device is paused) (#289)
 - Make it easier to convert between portable and installed versions (#271, #272)
 - Fix race condition causing crash in metered network manager (#291)

v1.1.10
-------

 - Ship with Syncthing 0.13 by default (only affects new users)
 - Fix race condition when reloading config (#259)
 - Don't show 'Finished Syncthing' halfway through a busy sync (#264)
 - Don't crash when right-clicking tray icon early in startup process (#257)
 - Attempt to fix issue where UI half-renders after update to Syncthing 0.13 (#262)

v1.1.9
------

 - Add support for Syncthing v0.13 (#250)
 - Add setting to control tray icon animation (#255)
 - Don't refresh browser when un-minimizing (#248)
 - Don't crash if the filesystem watcher sees a change at the drive root (#253)
 - Handle filesystem notifications when Syncthing is configured with a path of the wrong case (#256)
 - Clarify wording when device paused by metered network (#249)

v1.1.8
------

 - Fix portable upgrades when there's a space in the path (will fix upgrades from 1.1.8 -> 1.1.9) (#232)
 - Improve quality of small tray icons slightly (#140)
 - Add "new device" / "new folder" balloon messages (#235)
 - Improve update checking schedule
 - Don't confuse the user when pausing devices on Windows 8+ (#242)
 - Fix touch screen operation (although touch screen scrolling is still broken upstream) (#241)
 - Allow settings window to be resized vertically (#238)
 - Move to SHA512 for verifying downloads
 - Add logging to help debug case where Syncthing returns odd values on startup
 - Remove API Key from settings

v1.1.7
------

 - Handle thousands of conflicts in the conflict editor without crashing (#224)
 - Handle crash when syncing many files (reappearance of #112) (#227)
 - Fix rendering of some strings
 - Add logging to file to portable upgrades, in case of error

v1.1.6
------

 - Fix portable installer where TEMP is on a different drive to SyncTrayzor (#218)
 - Fix bug where 'Save' button wouldn't be enabled in Syncthing's 'Add/Edit Folder' pane, after using the 'Browse' button (#219)
 - Fix race crash (#221)
 - Don't force Syncthing to use https (should fix #201)
 - Handle problems getting the icon for a file (should fix #224)
 - Improve help output of ProcessRunner.exe (#223)

v1.1.5
------

 - Fix further crash when failing to determine if a connection is metered (#215, #216)

v1.1.4
------

 - Fix issue with embedded browser failing to start on some systems (#211, #213)
 - Fix crash when failing to determine if a connection is metered (#210, #212)

v1.1.3
------

 - Disable devices which connect over a metered network (#167)
 - Don't report conflict files in the .stversions folder (#203)
 - Add a 'Browse' button (which opens a folder browser) to Syncthing's 'Add folder' dialog (#78)
 - Fix a race condition in the alerts system (#208)
 - Log file transfers to a CSV file in the logs directly (#205)
 - Upgrade the embedded browser: may fix issues with Syncthing's UI not loading at first, and adds support for touch-screen devices (#129)
 - Create chocolatey package (#189)
 - Clarify some wording in Settings and the Conflict Resolver (#204, #209)
 - Handle two instances of SyncTrayzor saving their config at the same time (#185)

v1.1.2
------

 - Handle folders with missing markers again (#187)
 - Don't crash in some cases on .NET 4.5 when the conflict editor is completed (#199)
 - Don't crash if the ConflictFileWatcher is aborted (#200, #202)
 - Don't show conflicts alerts bar if Syncthing isn't running
 - Improve conflict file monitoring (should remove inaccuracies)
 - Don't fail if there's a link loop when scanning for conflicted files (#195)
 - Add 'Size' field to the conflict resolver (#194)
 - Add setting to control whether conflict files are deleted to the recycle bin
 - Pressing F5 will fresh the browser
 - Fix the portable installation procedure (sorry portable users: you'll have to manually upgrade
   one last time).

v1.1.1
------

 - Fix crash if we fail to look for conflicted files in a path (#191, #193)
 - Fix crash if the user manually specifies a UNC prefix on a folder (#192)

v1.1.0
-------

 - Log Syncthing's output (#162).
 - Add a Settings tab to enable Syncthing debug facilities without setting STTRACE or restarting (#175).
 - Alerts system: show warning triangle on tray icon, and alerts at the top of SyncTrayzor, when there
   are failed file transfers or conflicted files.
 - Add a tool to find and help resolve file conflicts (under File -> Conflict Resolver).
 - Add support for one-click upgrades for Portable installations.
 - Improve 'Syncthing Console' (#82).
 - Improve update check schedule (#184).

v1.0.32
-------

 - Fix rare crash when trying to save the config file

v1.0.31
-------

 - Fix crash if 'logs archive' folder doesn't exist (#178)

v1.0.30
-------

- Default to Syncthing v0.12. This only affects new users
- Fix issue where AV programs could lock our config file, causing a crash (#159, #166)
- Fix bug where window placement wouldn't be recorded on new installs (#171)
- Display failed transfers separately in balloon messages (#173)
- Add pause/clear buttons to Syncthing console window (#174)
- Clean up config folder

v1.0.29
-------

 - Support Syncthing v0.12
 - Fix a couple of rare crashes (#150, #157)
 

v1.0.28
-------

 - Allow extra Syncthing command-line arguments to be specified (#133)
 - Fix bug which would prevent multiple logged-on users using the same SyncTrayzor installation (#148)
 - Add extra process priority options for Syncthing (other than just 'low priority') (#143)
 - Fix a couple of small crashes
 - Reduce installer/portable zip size slightly
 - Handle restart-less Folder and Device changes in Syncthing
 - Handle some edge-cases where Syncthing state changes in the middle of lots of file transfers may not be noticed
 - Fix a very rare "Error creating the Web Proxy" issue (#131)
 - Improve translations (#142, others)


v1.0.27
-------

 - Fix issues with corrupted config file
 - Don't queue notification messages
 - Update translations

v1.0.26
-------

 - Add support for Syncthing v0.11.12
 - Allow 'synced' notifications to be controlled per-folder (#99)
 - Don't show upgrade prompt if another application is fullscreen (#118)
 - Launching an already-running SyncTrayzor exe won't launch two instances (#119)

v1.0.25
-------

 - Fix crash on shutdown (#117)
 - Don't show file tranfers that are 'starting'
 - Be smarter about network timeouts when resuming from sleep

v1.0.24
-------

 - Fix a couple of crashes (#116)
 - Add Chinese translation (thanks Honpan Lung!)

v1.0.23
-------

 - Support for Syncthing v0.11.10
 - Fix and improve file transfers window (#101, #106)
 - Fix various crashes (#108, #112, #114, #115)
 - Add option to disable hardware rendering (#104)

v1.0.22
-------

 - Improvements to the "File Transfers" view (single-click tray icon)
 - Better error message handling (#93, #96)
 - Update translations

v1.0.21
-------

 - Fix version bump script, which was causing incorrect assembly version to be written

v1.0.20
-------

 - Update translations:
   - Updates to all languages
   - New languages: Catalan (Valencian), Portuguese (Brazil)

v1.0.19
-------

 - Add Dropbox-style window with current file transfers. Single-click the icon to view (#18)
 - Remember Syncthing language selection (#87)
 - Reword 'use custom home for Syncthing' option in Settings to be clearer (#88)
 - Don't show main window outside of desktop on very small screens (#84)
 - Don't crash if watched folder is a symlink (#89)
 - Improve error message if Syncthing cannot start (#90)
 - Fix possible crash if computer locale is changed (#91)
 - Allow custom Syncthing paths (by hand-editing config file, useful for edge case setups) (#86)
 - Add context menu to web browser (cut/copy/paste) (#85)
 - Allow multiple SyncTrayzor installations (portable and installed) to co-exist (#81)
 - Ignore system proxy settings when connecting to Syncthing (#80)

v1.0.18
-------

 - Fix crash when file in watched folder is renamed (#79)

v1.0.17
-------

 - Fix crash when renaming a file whose path exceeds the Windows path length limit (#72)
 - Fix 'Open Folder' button in Syncthing UI (#65)
 - Ensure that folder list in Settings does not exceed screen height (#76)
 - Start minimized after automatic upgrade (#59)
 - Add italian translation (thanks stukdev)
 - Improve text in icon context menu (#71)
 - Console will scroll to end after resize (#67)

v1.0.16
-------

 - Installer recommends Syncthing 0.11 (#64)
 - Fix bad browser zoom after restart (#57)
 - Fix display of folders which contain an underscore (#58)
 - Handle duplicate devices/folders in Syncthing config (#61)
 - Fix bad character encoding in Syncthing console (#62)
 - Fix installer's handling of Syncthing version changes (#63)
 - Clarify some UI wording/typos (#60, others)
 - Remember size of Syncthing console (#56)
 - Updated translations

v1.0.15
-------

 - Fix crash on startup if Syncthing is slow to start (#55)
 - Remember window size/position (#51)
 - Zoom built-in browser (#52)
 - Add support for arbitrary environmental variables for Syncthing

v1.0.14
-------

 - Give Syncthing more than 10 seconds to start, fixing crash (#47, #48, #50)
 - Better Syncthing API management in general
 - Add support for 150% and 200% DPI to tray icon
 - Slightly improve UI

v1.0.13
-------

 - Fix crash if 'Show tray icon only on close' is checked (#45)
 - Fix undocumented REST API change in Syncthing 0.11 (#46)
 - Check for updates on resume from sleep 
 - Ensure SyncTrayzor is started as original user after auto-update

v1.0.12
-------

 - Compatibility with Syncthing v0.11.x (beta) (#43)
 - Improved auto-updates (#39)
 - Add option to hide the console (#41)
 - Obfuscate Device IDs in log files, as well as in the console
 - Logging will take less space if Syncthing is spamming messages (#42)
 - Updated translations (all languages)
 - New translations:
   - Czech (thanks Václav Obrtlík)
   - Greek (thanks alexxtasi and Wasilis Mandratzis-Walz)
   - French (thanks princejosuah, lpoujol and Martin Erpicum)
   - Russian (thanks Ivan Lapenkov)
   - Slovak (thanks Lukáš Černý)
 - Fix bug where 'Close to Tray' from tray context menu would not work if settings say not to close to tray

v1.0.11
-------

 - Fix bug where GUI would freeze if Syncthing logged to console too frequently (#36)

v1.0.10
-------

 - Add "Open Folder" buttons (#32):
   - In the GUI, next to 'Edit' and 'Rescan'
   - In the tray icon's right-click menu
 - Add settings to:
   - Stop Syncthing auto-upgrading
   - Run Syncthing as a low-priority process (#24)
   - Disable localization (#35)
 - Check for updates when resuming from sleep (#34)
 - Add Spanish translation (thanks Diego Sierra!)
 - Break Settings dialog into multiple tabs, for those with small screens (#29)
 - Always use HTTPS (#33)
 - Store path configuration and default user configuration in SyncTrayzor.exe.config (#30)
 - Reload Syncthing's address / home dir / API key / etc when it's restarted (#31)
 - Fix tray icon

v1.0.9
------

 - Fix bug with directory watcher and paths containing a tilde (~)
 - Fix resolution issue with taskbar icon
 - Add 'device connected/disconnected' tray icon balloon messages
 - Add Dutch translation (thanks Heimen Stoffels!)
 - Small reduction in memory usage
 - Add menu item to restart Syncthing

v1.0.8
------

- Support HTTPS
- Add German translation (thanks Adrian Rudnik)
- Ensure SyncTrayzor is terminated properly when updating/uninstalling using the installer

v1.0.7
------

 - Support GUI Authentication
 - Ignore 'synced' events after device connection/disconnection, reducing noise
 - Add option to obfuscate device IDs (thanks Adrian Rudnik)
 - Allow Syncthing to localize by sending correct language headers (thanks Adrian Rudnik)
 - Add validation to the Settings page
 - Better handle exceptions encountered during shutdown
 - Catch case where syncthing.exe can't be started because of group policy settings

v1.0.6
------

 - Include high-quality icon (thanks to d4k0)
 - Improve settings dialog around API key and GUI Host Address
 - Add 32-bit build

v1.0.5
------

 - Replace syncthing.exe in APPDATA if it goes missing for some reason
 - Add option to run Syncthing with a custom home directory
 - Add portable build
 - Add 'Minimize to Tray' option
 - Improve error messages and logging
 - Close Synchthing gracefully on application exit

v1.0.4
------

  - Handle Syncthing upgrades (previously would require a 'Kill all syncthing processes' then 'Start')
  - Fix crash when logging out / shutting down with SyncTrayzor opened (caused by embedded browser component)
  - Don't unload browser when minimized. This means that open Syncthing dialogs aren't closed when minimizing
  - Reduce memory usage if SyncTrayzor is never restored from tray
  - UI tweaks and fixes
  - Add VC++ x64 Redist to the installer

v1.0.3
------

 - Improve directory watching
   - Don't notify Syncthing if path is currently being synchronized
   - Don't notify Syncthing if path is ignored
   - Handle removed/re-created folders (e.g. USB and network drives)
 - Better UI for updates, if Syncthing fails to start, or if an unhandled exception occurs
 - Double-clicking tray icon always brings window into foreground
 - Start logging to an external log file
 - Add -noautostart command-line flag

v1.0.2
------

 - Fix memory leaks
 - Reduce memory usage by switching from WPF's WebBrowser to CefSharp
 - Move from int -> long for most of the Syncthing API, allowing e.g. repos larger than 3.8GB

v1.0.1
------

 - Add support for new ItemFinished event
 - Handle lots of log messages in quick succession

v1.0.0
------

 - Initial version
