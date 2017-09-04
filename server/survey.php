<?php

/**
 * Survey endpoint for SyncTrayzor
 */

set_error_handler('error_handler');
date_default_timezone_set('UCT');

function error_handler($severity, $message, $filename, $lineno)
{
   throw new ErrorException($message, 0, $severity, $filename, $lineno);
}

define('DATABASE', 'survey.sqlite');

try 
{
	$exists = file_exists(DATABASE);

	$db = new PDO('sqlite:'.DATABASE);
	$db->setAttribute(PDO::ATTR_ERRMODE, PDO::ERRMODE_EXCEPTION);
	$db->exec('PRAGMA foreign_keys = ON;');

	if (!$exists)
	{
		$db->exec("CREATE TABLE IF NOT EXISTS responses (
			id INTEGER PRIMARY KEY,
			date TEXT NOT NULL,
			version TEXT NOT NULL,
			ip TEXT NOT NULL,
			comment TEXT
		);");
		$db->exec("CREATE TABLE IF NOT EXISTS checklist (
			id INTEGER PRIMARY KEY,
			response_id INTEGER NOT NULL REFERENCES responses(id),
			key TEXT NOT NULL
		);");
	}

	$data = json_decode(file_get_contents('php://input'), true);
	$stmt = $db->prepare("INSERT INTO responses(date, ip, version, comment) VALUES (CURRENT_TIMESTAMP, :ip, :version, :comment);");
	$stmt->execute(array(
		'ip' => $_SERVER['REMOTE_ADDR'],
		'version' => $data['version'],
		'comment' => $data['comment']));
	$responseId = $db->lastInsertId();

	$stmt = $db->prepare("INSERT INTO CHECKLIST (response_id, key) VALUES (:response_id, :key);");

	foreach ($data['checklist'] as $key => $value)
	{
		if ($value)
		{
			$stmt->execute(array('response_id' => $responseId, 'key' => $key));
		}
	}
}
catch (Exception $e)
{
	$loggable_error = $e->getMessage() . "\n" . $e->getTraceAsString();
	file_put_contents("survey_errors.txt", $loggable_error, FILE_APPEND);
}