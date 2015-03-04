require 'rexml/document'

BIN_DIR = 'bin/x64/Release'
SRC_DIR = 'src/SyncTrayzor'
INSTALLER_DIR = 'installer'

PORTABLE_OUTPUT_DIR = File.absolute_path('SyncTrayzorPortable')
PORTABLE_FILES = FileList[
  File.join(BIN_DIR, '*.exe'),
  File.join(BIN_DIR, '*.exe.config'),
  File.join(BIN_DIR, '*.dll'),
  File.join(BIN_DIR, '*.pdb'),
  File.join(BIN_DIR, '*.pak'),
  File.join(BIN_DIR, '*.dat'),
  File.join(BIN_DIR, 'locales', '*'),
  File.join(SRC_DIR, 'Icons', 'default.ico'),
  '*.md',
  '*.txt',
  File.join(INSTALLER_DIR, 'syncthing.exe')
].exclude('*.vshost*')

def cp_to_portable(src)
  dest = File.join(PORTABLE_OUTPUT_DIR, src)
  mkdir_p File.dirname(dest) unless File.exist?(File.dirname(dest))
  cp src, dest
end

desc "Create the portable release directory"
task :portable do
  rm_rf PORTABLE_OUTPUT_DIR
  mkdir_p PORTABLE_OUTPUT_DIR

  Dir.chdir(BIN_DIR) do
    files = FileList[
      '*.exe',
      '*.exe.config',
      '*.dll',
      '*.pdb',
      '*.pak',
      '*.dat',
      File.join('locales', '*'),
    ].exclude('*.vshost.*')

    files.each do |file|
      cp_to_portable(file)
    end
  end

  cp File.join(SRC_DIR, 'Icons', 'default.ico'), PORTABLE_OUTPUT_DIR

  FileList['*.md', '*.txt'].each do |file|
    cp_to_portable(file)
  end

  Dir.chdir(INSTALLER_DIR) do
    FileList['syncthing.exe', '*.dll'].each do |file|
      cp_to_portable(file)
    end
  end

  puts 'Rewriting app.config'
  config_path = File.join(PORTABLE_OUTPUT_DIR, 'SyncTrayzor.exe.config')
  doc = File.open(config_path, 'r') do |f|
    doc = REXML::Document.new(f)
    REXML::XPath.first(doc, '/configuration/applicationSettings//setting[@name="PortableMode"]/value').text = 'True'
    doc
  end
  File.open(config_path, 'w') do |f|
    doc.write(f)
  end
end