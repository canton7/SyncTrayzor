require 'rexml/document'
begin
  require 'albacore'
rescue LoadError
  warn "Please run 'gem install albacore --pre'"
  exit 1
end

ISCC = '"C:\Program Files (x86)\Inno Setup 5\ISCC.exe"'

BIN_DIR_64 = 'bin/x64/Release'
BIN_DIR_86 = 'bin/x86/Release'

SRC_DIR = 'src/SyncTrayzor'
INSTALLER_DIR = 'installer'

INSTALLER_64 = File.join(INSTALLER_DIR, 'x64')
INSTALLER_86 = File.join(INSTALLER_DIR, 'x86')

INSTALLER_64_OUTPUT = File.join(INSTALLER_64, 'SyncTrayzorSetup-x64.exe')
INSTALLER_86_OUTPUT = File.join(INSTALLER_86, 'SyncTrayzorSetup-x86.exe')

PORTABLE_OUTPUT_DIR_64 = File.absolute_path('SyncTrayzorPortable-x64')
PORTABLE_OUTPUT_DIR_86 = File.absolute_path('SyncTrayzorPortable-x86')

CONFIG = ENV['CONFIG'] || 'Release'

def cp_to_portable(ouput_dir, src)
  dest = File.join(ouput_dir, src)
  mkdir_p File.dirname(dest) unless File.exist?(File.dirname(dest))
  cp src, dest
end

desc 'Build the project (64-bit)'
build :buildx64 do |b|
  b.sln = 'src/SyncTrayzor.sln'
  b.target = [:Clean, :Build]
  b.prop 'Configuration', CONFIG
  b.prop 'Platform', 'x64'
end

desc 'Build the project (32-bit)'
build :buildx86 do |b|
  b.sln = 'src/SyncTrayzor.sln'
  b.target = [:Clean, :Build]
  b.prop 'Configuration', CONFIG
  b.prop 'Platform', 'x86'
end

desc 'Build both 64-bit and 32-bit binaries'
task :build => [:buildx64, :buildx86]

def create_installer(output_file, installer_dir, iss_name)
  rm output_file if File.exist?(output_file)
  sh ISCC, File.join(installer_dir, iss_name)
end

desc 'Create 64-bit installer'
task :installerx64 do
  create_installer(INSTALLER_64_OUTPUT, INSTALLER_64, 'installer-x64.iss')
end

desc 'Create 32-bit installer'
task :installerx86 do
  create_installer(INSTALLER_86_OUTPUT, INSTALLER_86, 'installer-x86.iss')
end

desc 'Create 32-bit and 64-bit installers'
task :installer => [:installerx64, :installerx86]

def create_portable(bin_dir, output_dir, installer_platform_dir)
  rm_rf output_dir
  mkdir_p output_dir

  Dir.chdir(bin_dir) do
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
      cp_to_portable(output_dir, file)
    end
  end

  cp File.join(SRC_DIR, 'Icons', 'default.ico'), output_dir

  FileList['*.md', '*.txt'].each do |file|
    cp_to_portable(output_dir, file)
  end
  
  Dir.chdir(installer_platform_dir) do
    FileList['syncthing.exe', '*.dll'].each do |file|
      cp_to_portable(output_dir, file)
    end
  end

  puts 'Rewriting app.config'
  config_path = File.join(output_dir, 'SyncTrayzor.exe.config')
  doc = File.open(config_path, 'r') do |f|
    doc = REXML::Document.new(f)
    REXML::XPath.first(doc, '/configuration/applicationSettings//setting[@name="PortableMode"]/value').text = 'True'
    doc
  end
  File.open(config_path, 'w') do |f|
    doc.write(f)
  end
end

desc 'Create the portable (x64) release directory'
task :portablex64 do
  create_portable(BIN_DIR_64, PORTABLE_OUTPUT_DIR_64, INSTALLER_64)
end

desc 'Create the portable (x86) release directory'
task :portablex86 do
  create_portable(BIN_DIR_86, PORTABLE_OUTPUT_DIR_86, INSTALLER_86)
end

desc 'Create portable release directories for x64 and x86'
task :portable => [:portablex64, :portablex86]

desc 'Build and package everything'
task :package => [:build, :installer, :portable]

desc 'Remove portable and installer'
task :clean do
  rm_rf PORTABLE_OUTPUT_DIR_64 if File.exist?(PORTABLE_OUTPUT_DIR_64)
  rm_rf PORTABLE_OUTPUT_DIR_86 if File.exist?(PORTABLE_OUTPUT_DIR_86)
  rm INSTALLER_64 if File.exist?(INSTALLER_64)
  rm INSTALLER_86 if File.exist?(INSTALLER_86)
end