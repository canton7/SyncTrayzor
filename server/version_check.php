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
   '1.1.13' => [
      'installed' => [
         'direct_download_url' => [
            'x64' => 'https://github.com/canton7/SyncTrayzor/releases/download/v{version}/SyncTrayzorSetup-x64.exe',
            'x86' => 'https://github.com/canton7/SyncTrayzor/releases/download/v{version}/SyncTrayzorSetup-x86.exe',
         ],
      ],
      'portable' => [
         'direct_download_url' => [
            'x64' => 'https://github.com/canton7/SyncTrayzor/releases/download/v{version}/SyncTrayzorPortable-x64.zip',
            'x86' => 'https://github.com/canton7/SyncTrayzor/releases/download/v{version}/SyncTrayzorPortable-x86.zip',
         ],
      ],     
      'sha1sum_download_url' => 'https://github.com/canton7/SyncTrayzor/releases/download/v{version}/sha1sum.txt.asc',
      'sha512sum_download_url' => 'https://github.com/canton7/SyncTrayzor/releases/download/v{version}/sha512sum.txt.asc',
      'release_page_url' => 'https://github.com/canton7/SyncTrayzor/releases/tag/v{version}',
      'release_notes' => "- Improve 'Browse' button next to folder selection input (#297)\n- Fix right-click context menu in embedded browser (#300)\n- Fix crash on conflict resolution screen when Windows can't find an icon for the file type (#301)\n- Fix crash when opening SyncTrayzor (#303, #306, #318)\n- Indication of file conflict in tray icon didn't disappear in some cases (#307)\n- (Hopefully) add workaround for Chinese IME not working (#314)\n- Display folder names instead of folder IDs in tray notifications (#315)\n- Rename 'Restore' option in tray icon context menu (#320)",
   ]
];

$upgrades = [
   '1.1.12' => ['to' => 'latest', 'formatter' => '5'],
   '1.1.11' => ['to' => 'latest', 'formatter' => '5'],
   '1.1.10' => ['to' => 'latest', 'formatter' => '5'],
   '1.1.9' => ['to' => 'latest', 'formatter' => '5'],
   '1.1.8' => ['to' => 'latest', 'formatter' => '5'],
   '1.1.7' => ['to' => 'latest', 'formatter' => '4'],
   '1.1.6' => ['to' => 'latest', 'formatter' => '4'],
   '1.1.5' => ['to' => 'latest', 'formatter' => '4'],
   '1.1.4' => ['to' => 'latest', 'formatter' => '4'],
   '1.1.3' => ['to' => 'latest', 'formatter' => '4'],
   '1.1.2' => ['to' => 'latest', 'formatter' => '4'],
   '1.1.1' => ['to' => 'latest', 'formatter' => '3'],
   '1.1.0' => ['to' => 'latest', 'formatter' => '3'],
   '1.0.32' => ['to' => 'latest', 'formatter' => '3'],
   '1.0.31' => ['to' => 'latest', 'formatter' => '3'],
   '1.0.30' => ['to' => 'latest', 'formatter' => '3'],
   '1.0.29' => ['to' => 'latest', 'formatter' => '3'],
   '1.0.28' => ['to' => 'latest', 'formatter' => '3'],
   '1.0.27' => ['to' => 'latest', 'formatter' => '3'],
   '1.0.26' => ['to' => 'latest', 'formatter' => '3'],
   '1.0.25' => ['to' => 'latest', 'formatter' => '3'],
   '1.0.24' => ['to' => 'latest', 'formatter' => '3'],
   '1.0.23' => ['to' => 'latest', 'formatter' => '3'],
   '1.0.22' => ['to' => 'latest', 'formatter' => '2'],
   '1.0.21' => ['to' => 'latest', 'formatter' => '2'],
   '1.0.20' => ['to' => 'latest', 'formatter' => '2'],
   // 1.0.19 was never actually released, so no need to represent it
   '1.0.18' => ['to' => 'latest', 'formatter' => '2'],
   '1.0.17' => ['to' => 'latest', 'formatter' => '2'],
   '1.0.16' => ['to' => 'latest', 'formatter' => '2'],
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
   // Prior to sha1sum_download_url
   '2' => function($arch, $variant, $to_version, $to_version_info, $overrides)
   {
      $variant_info = isset($overrides[$variant]) ? get_with_wildcard($overrides, $variant) : get_with_wildcard($to_version_info, $variant);

      $data = [
         'version' => $to_version,
         'release_page_url' => $to_version_info['release_page_url'],
         'release_notes' => isset($overrides['release_notes']) ? $overrides['release_notes'] : $to_version_info['release_notes'],
      ];

      if ($variant == 'installed')
      {
         $data['direct_download_url'] = get_with_wildcard($variant_info['direct_download_url'], $arch);
      }

      return $data;
   },
   // Portable versions don't know how to handle directl downloads (or it's broken...)
   '3' => function($arch, $variant, $to_version, $to_version_info, $overrides)
   {
      $variant_info = isset($overrides[$variant]) ? get_with_wildcard($overrides, $variant) : get_with_wildcard($to_version_info, $variant);

      $data = [
         'version' => $to_version,
         'release_page_url' => $to_version_info['release_page_url'],
         'release_notes' => isset($overrides['release_notes']) ? $overrides['release_notes'] : $to_version_info['release_notes'],
      ];

      if ($variant == 'installed')
      {
         $data['direct_download_url'] = get_with_wildcard($variant_info['direct_download_url'], $arch);
         $data['sha1sum_download_url'] =  $to_version_info['sha1sum_download_url'];
      }

      return $data;
   },
   '4' => function($arch, $variant, $to_version, $to_version_info, $overrides)
   {
      $variant_info = isset($overrides[$variant]) ? get_with_wildcard($overrides, $variant) : get_with_wildcard($to_version_info, $variant);

      $data = [
         'version' => $to_version,
         'direct_download_url' => get_with_wildcard($variant_info['direct_download_url'], $arch),
         'sha1sum_download_url' => $to_version_info['sha1sum_download_url'],
         'release_page_url' => $to_version_info['release_page_url'],
         'release_notes' => isset($overrides['release_notes']) ? $overrides['release_notes'] : $to_version_info['release_notes'],
      ];

      return $data;
   },
   // Learnt about sha512sum
   '5' => function($arch, $variant, $to_version, $to_version_info, $overrides)
   {
      $variant_info = isset($overrides[$variant]) ? get_with_wildcard($overrides, $variant) : get_with_wildcard($to_version_info, $variant);

      $data = [
         'version' => $to_version,
         'direct_download_url' => get_with_wildcard($variant_info['direct_download_url'], $arch),
         'sha512sum_download_url' => $to_version_info['sha512sum_download_url'],
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
      array_walk_recursive($versions[$to_version], function(&$value, $key) use ($to_version) {
         $value = str_replace('{version}', $to_version, $value);
      });
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
