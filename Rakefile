require 'rexml/document'
begin
  require 'albacore'
rescue LoadError
  warn "Please run 'gem install albacore --pre'"
  exit 1
end

ISCC = '"C:\Program Files (x86)\Inno Setup 5\ISCC.exe"'

BIN_DIR = 'bin/x64/Release'
SRC_DIR = 'src/SyncTrayzor'
INSTALLER_DIR = 'installer'

PORTABLE_OUTPUT_DIR = File.absolute_path('SyncTrayzorPortable')

CONFIG = ENV['CONFIG'] || 'Release'
PLATFORM = ENV['PLATFORM'] || 'x64'

def cp_to_portable(src)
  dest = File.join(PORTABLE_OUTPUT_DIR, src)
  mkdir_p File.dirname(dest) unless File.exist?(File.dirname(dest))
  cp src, dest
end

desc 'Build the project'
build :build do |b|
  b.sln = 'src/SyncTrayzor.sln'
  b.target = [:Clean, :Build]
  b.prop 'Configuration', CONFIG
  b.prop 'Platform', PLATFORM
end

task :installer do
  rm File.join(INSTALLER_DIR, 'SyncTrayzorSetup.exe')
  sh ISCC, File.join(INSTALLER_DIR, 'installer.iss')
end

desc 'Create the portable release directory'
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

desc 'Build and package everything'
task :package => [:build, :installer, :portable]