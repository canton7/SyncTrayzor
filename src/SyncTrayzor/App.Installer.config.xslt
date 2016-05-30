<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
  version="1.0">
  <xsl:output method="xml" indent="yes"/>

  <xsl:template match="/configuration/settings/EnableAutostartOnFirstStart/text()">true</xsl:template>
  <xsl:template match="/configuration/settings/Variant/text()">Installed</xsl:template>
  
  <xsl:template match="/configuration/settings/PathConfiguration/LogFilePath/text()">%APPDATA%\SyncTrayzor\logs</xsl:template>
  <xsl:template match="/configuration/settings/PathConfiguration/ConfigurationFilePath/text()">%APPDATA%\SyncTrayzor\config.xml</xsl:template>
  <xsl:template match="/configuration/settings/PathConfiguration/ConfigurationFileBackupPath/text()">%APPDATA%\SyncTrayzor\config-backups</xsl:template>
  <xsl:template match="/configuration/settings/PathConfiguration/CefCachePath/text()">%LOCALAPPDATA%\SyncTrayzor\cef\cache</xsl:template>
  <!-- null means use Syncthing's default -->
  <xsl:template match="/configuration/settings/PathConfiguration/SyncthingHomePath/text()"></xsl:template>

  <xsl:template match="/configuration/settings/DefaultUserConfiguration/SyncthingPath/text()">%APPDATA%\SyncTrayzor\syncthing.exe</xsl:template>
  
  <!-- Default template -->
  <xsl:template match="node()|@*">
    <xsl:copy>
      <xsl:apply-templates select="node()|@*"/>
    </xsl:copy>
  </xsl:template>
</xsl:stylesheet>