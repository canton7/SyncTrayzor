<?php

/**
 * Version upgrade path manager for SyncTrayzor
 * 
 * Clients request this with their current version, arch, and variant (portable, etc)
 * and this gives them a version to upgrade to (if any), along with the method of
 * upgrading to it (manual navigation to Github release page, automatic silent upgrade,
 * etc). 
 * 
 * $versions is a record of all of the current releases, which we might want to upgrade
 * people to. It has the structure:
 * [
 *    version => [
 *       variant => [
 *          'url' => [
 *             arch => 'url',
 *             ...
 *          ],
 *       ],
 *       ...
 *       'release_notes' => release_notes,
 *    ],
 *    ...
 * ]
 *
 * version: version string e.g. '1.2.3'
 * variant: e.g. 'portable', 'installed'. Matched against the variant provided by the
 *          client, or '*' can be used to specify a default.
 * arch:    e.g. 'x86', 'x64'. Matched against the arch provided by the client, or '*'
 *          can used to specify a default.
 * release_notes: Release notes to display to the user.
 * 
 * $upgrades is a map of old_version => new_version, and specifies the formatter to
 * use to communicate with old_version. It also allows various overrides to be
 * specified (e.g. release notes)
 * It has the structure:
 * [
 *    old_version => ['to' => new_version, 'formatter' => formatter_version, 'overrides' => [overrides]],
 *    ...
 * ]
 *
 * old_version: version being upgraded from
 * new_version: version to upgrade to
 * formatter_version: formatter version to use (in $response_formatters)
 * overrides: optional overrides, used by the formatter
 */

set_error_handler('error_handler');
date_default_timezone_set('UCT');
header('Content-Type: application/json');

function error_handler($severity, $message, $filename, $lineno)
{
   throw new ErrorException($message, 0, $severity, $filename, $lineno);
}

function get_with_wildcard($src, $value, $default = null)
{
   if (isset($src[$value]))
      return $src[$value];
   if (isset($src['*']))
      return $src['*'];
   return $default;
}

$versions = [
   '1.0.16' => [
      'installed' => [
         'direct_download_url' => [
            'x64' => 'https://github.com/canton7/SyncTrayzor/releases/download/v1.0.16/SyncTrayzorSetup-x64.exe',
            'x86' => 'https://github.com/canton7/SyncTrayzor/releases/download/v1.0.16/SyncTrayzorSetup-x86.exe',
         ],
      ],
      'release_page_url' => 'https://github.com/canton7/SyncTrayzor/releases/tag/v1.0.16',
      'release_notes' => "- Installer recommends Syncthing 0.11 (#64)\n- Fix bad browser zoom after restart (#57)\n- Fix display of folders which contain an underscore (#58)\n- Handle duplicate devices/folders in Syncthing config (#61)\n- Fix bad character encoding in Syncthing console (#62)\n- Fix installer's handling of Syncthing version changes (#63)\n- Clarify some UI wording/typos (#60, others)\n- Remember size of Syncthing console (#56)\n- Updated translations",
   ],
   '1.0.15' => [
      'installed' => [
         'direct_download_url' => [
            'x64' => 'https://github.com/canton7/SyncTrayzor/releases/download/v1.0.15/SyncTrayzorSetup-x64.exe',
            'x86' => 'https://github.com/canton7/SyncTrayzor/releases/download/v1.0.15/SyncTrayzorSetup-x86.exe',
         ],
      ],
      'release_page_url' => 'https://github.com/canton7/SyncTrayzor/releases/tag/v1.0.15',
      'release_notes' => "- Fix crash on startup if Syncthing is slow to start (#55)\n- Remember window size/position (#51)\n- Zoom built-in browser (#52)\n- Add support for arbitrary environmental variables for Syncthing",
   ],
   '1.0.14' => [
      'installed' => [
         'direct_download_url' => []
      ],
      'release_page_url' => 'https://github.com/canton7/SyncTrayzor/releases/tag/v1.0.14',
      'release_notes' => "- Give Syncthing more than 10 seconds to start, fixing crash (#47, #48, #50)\n- Better Syncthing API management in general\n- Add support for 150% and 200% DPI to tray icon\n- Slightly improve UI",
   ],
   '1.0.13' => [
      'installed' => [
         'direct_download_url' => []
      ],
      'release_page_url' => 'https://github.com/canton7/SyncTrayzor/releases/tag/v1.0.13',
      'release_notes' => "- Fix crash if 'Show tray icon only on close' is checked (#45)\n- Fix undocumented REST API change in Syncthing 0.11 (#46)\n- Check for updates on resume from sleep\n- Ensure SyncTrayzor is started as original user after auto-update",
   ],
];

$upgrades = [
   '1.0.15' => ['to' => 'latest', 'formatter' => '2'],
   '1.0.14' => ['to' => 'latest', 'formatter' => '2'],
   '1.0.13' => ['to' => 'latest', 'formatter' => '1'],
   '1.0.12' => ['to' => 'latest', 'formatter' => '1'],
];

$response_formatters = [
   // 1.0.12 and 1.0.13 shouldn't download installers directly, as they doesn't know how to run them properly
   '1' => function($arch, $variant, $to_version, $to_version_info, $overrides)
   {
      $data = [
         'version' => $to_version,
         'direct_download_url' => null,
         'release_page_url' => $to_version_info['release_page_url'],
         'release_notes' => isset($overrides['release_notes']) ? $overrides['release_notes'] : $to_version_info['release_notes'],
      ];

      return $data;
   },
   // For when everything's working....
   '2' => function($arch, $variant, $to_version, $to_version_info, $overrides)
   {
      $variant_info = isset($overrides[$variant]) ? get_with_wildcard($overrides, $variant) : get_with_wildcard($to_version_info, $variant);

      $data = [
         'version' => $to_version,
         'direct_download_url' => get_with_wildcard($variant_info['direct_download_url'], $arch),
         'release_page_url' => $to_version_info['release_page_url'],
         'release_notes' => isset($overrides['release_notes']) ? $overrides['release_notes'] : $to_version_info['release_notes'],
      ];

      return $data;
   },
];

$error = null;
$loggable_error = null;
$data = null;

try
{
   $version = isset($_GET['version']) ? $_GET['version'] : null;
   $arch = isset($_GET['arch']) ? $_GET['arch'] : null;
   $variant = isset($_GET['variant']) ? $_GET['variant'] : null;


   if (empty($version) || empty($arch) || empty($variant))
   {
      $error = ['code' => 1, 'message' => 'version, arch, or variant not specified'];
   }
   else if (isset($upgrades[$version]))
   {
      $to_version = $upgrades[$version]['to'];
      if ($to_version == 'latest')
         $to_version = array_keys($versions)[0];
      $formatter = $response_formatters[$upgrades[$version]['formatter']];
      $overrides = isset($upgrades[$version]['overrides']) ? $upgrades[$version]['overrides'] : [];
      $to_version_info = $versions[$to_version];

      $data = $formatter($arch, $variant, $to_version, $to_version_info, $overrides);
   }
}
catch (Exception $e)
{
   $error = ['code' => 2, 'message' => 'Unhandled error. Please try again later'];
   $loggable_error = $e->getMessage() . "\n" . $e->getTraceAsString();
}

$rsp = [];
if ($data != null)
   $rsp['data'] = $data;
if ($error != null)
   $rsp['error'] = $error;

$output = json_encode($rsp, JSON_UNESCAPED_SLASHES | JSON_FORCE_OBJECT);

$date = date('c');
$log_msg = "$date\t{$_SERVER['REMOTE_ADDR']}\t$version\t$arch\t$variant\t$output\t$loggable_error\n";

$fp = fopen('log.txt', 'a+');
flock($fp, LOCK_EX);
fputcsv($fp, [$date, $_SERVER['REMOTE_ADDR'], $version, $arch, $variant, $output, $loggable_error]);
fclose($fp);

echo $output;
