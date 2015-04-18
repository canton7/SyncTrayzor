<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
  version="1.0">
  <xsl:output method="xml" indent="yes"/>

  <xsl:template match="/configuration/applicationSettings//setting[@name='EnableAutostartOnFirstStart']/value/text()">True</xsl:template>
  <xsl:template match="/configuration/applicationSettings//setting[@name='Variant']/value/text()">Installed</xsl:template>
  
  <xsl:template match="/configuration/applicationSettings//setting[@name='PathConfiguration']//LogFilePath/text()">%APPDATA%\SyncTrayzor\logs</xsl:template>
  <xsl:template match="/configuration/applicationSettings//setting[@name='PathConfiguration']//SyncthingCustomHomePath/text()">%LOCALAPPDATA%\SyncTrayzor\syncthing</xsl:template>
  <xsl:template match="/configuration/applicationSettings//setting[@name='PathConfiguration']//SyncthingPath/text()">%APPDATA%\SyncTrayzor\syncthing.exe</xsl:template>
  <xsl:template match="/configuration/applicationSettings//setting[@name='PathConfiguration']//ConfigurationFilePath/text()">%APPDATA%\SyncTrayzor\config.xml</xsl:template>
  <xsl:template match="/configuration/applicationSettings//setting[@name='PathConfiguration']//ConfigurationFileBackupPath/text()">%APPDATA%\SyncTrayzor\config-backups</xsl:template>

  <xsl:template match="/configuration/applicationSettings//setting[@name='DefaultUserConfiguration']//SyncthingUseCustomHome/text()">false</xsl:template>
  
  <!-- Default template -->
  <xsl:template match="node()|@*">
    <xsl:copy>
      <xsl:apply-templates select="node()|@*"/>
    </xsl:copy>
  </xsl:template>
</xsl:stylesheet>