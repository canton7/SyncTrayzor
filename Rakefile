require 'rexml/document'
begin
  require 'albacore'
rescue LoadError
  warn "Please run 'gem install albacore --pre'"
  exit 1
end

ISCC = 'C:\Program Files (x86)\Inno Setup 5\ISCC.exe'
unless File.exist?(ISCC)
  warn "Please install Inno Setup" 
  exit 1
end

CONFIG = ENV['CONFIG'] || 'Release'

SRC_DIR = 'src/SyncTrayzor'
INSTALLER_DIR = 'installer'

class ArchDirConfig
  attr_reader :arch
  attr_reader :bin_dir
  attr_reader :installer_dir
  attr_reader :installer_output
  attr_reader :installer_iss
  attr_reader :portable_output_dir

  def initialize(arch)
    @arch = arch
    @bin_dir = "bin/#{@arch}/#{CONFIG}"
    @installer_dir = File.join(INSTALLER_DIR, @arch)
    @installer_output = File.join(@installer_dir, "SyncTrayzorSetup-#{@arch}.exe")
    @installer_iss = File.join(@installer_dir, "installer-#{@arch}.iss")
    @portable_output_dir = File.absolute_path("SyncTrayzorPortable-#{@arch}")
  end
end

ARCH_CONFIG = [ArchDirConfig.new('x64'), ArchDirConfig.new('x86')]

namespace :build do
  ARCH_CONFIG.each do |arch_config|
    desc "Build the project (#{arch_config.arch})"
    build arch_config.arch do |b|
      b.sln = 'src/SyncTrayzor.sln'
      b.target = [:Clean, :Build]
      b.prop 'Configuration', CONFIG
      b.prop 'Platform', arch_config.arch
    end
  end
end

desc 'Build both 64-bit and 32-bit binaries'
task :build => ARCH_CONFIG.map{ |x| :"build:#{x.arch}" }

namespace :installer do
  ARCH_CONFIG.each do |arch_config|
    desc "Create the installer (#{arch_config.arch})"
    task arch_config.arch do
      rm arch_config.installer_output if File.exist?(arch_config.installer_output)
      sh %Q{"#{ISCC}"}, arch_config.installer_iss
    end
  end
end

desc 'Build both 64-bit and 32-bit installers'
task :installer => ARCH_CONFIG.map{ |x| :"installer:#{x.arch}" }

def cp_to_portable(ouput_dir, src)
  dest = File.join(ouput_dir, src)
  mkdir_p File.dirname(dest) unless File.exist?(File.dirname(dest))
  cp src, dest
end

namespace :portable do
  ARCH_CONFIG.each do |arch_config|
    desc "Create the portable package (#{arch_config.arch})"
    task arch_config.arch do
      rm_rf arch_config.portable_output_dir
      mkdir_p arch_config.portable_output_dir

      Dir.chdir(arch_config.bin_dir) do
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
          cp_to_portable(arch_config.portable_output_dir, file)
        end
      end

      cp File.join(SRC_DIR, 'Icons', 'default.ico'), arch_config.portable_output_dir

      FileList['*.md', '*.txt'].each do |file|
        cp_to_portable(arch_config.portable_output_dir, file)
      end
      
      Dir.chdir(arch_config.installer_dir) do
        FileList['syncthing.exe', '*.dll'].each do |file|
          cp_to_portable(arch_config.portable_output_dir, file)
        end
      end

      puts 'Rewriting app.config'
      config_path = File.join(arch_config.portable_output_dir, 'SyncTrayzor.exe.config')
      doc = File.open(config_path, 'r') do |f|
        doc = REXML::Document.new(f)
        REXML::XPath.first(doc, '/configuration/applicationSettings//setting[@name="PortableMode"]/value').text = 'True'
        doc
      end
      File.open(config_path, 'w') do |f|
        doc.write(f)
      end
    end
  end
end

desc 'Create both 64-bit and 32-bit portable packages'
task :portable => ARCH_CONFIG.map{ |x| :"portable:#{x.arch}" }

namespace :clean do
  ARCH_CONFIG.each do |arch_config|
    desc "Clean everything (#{arch_config.arch})"
    task arch_config.arch do
      rm_rf arch_config.portable_output_dir if File.exist?(arch_config.portable_output_dir)
      rm arch_config.installer_output if File.exist?(arch_config.installer_output)
    end
  end
end

desc 'Clean portable and installer, all architectures'
task :clean => ARCH_CONFIG.map{ |x| :"clean:#{x.arch}" }

namespace :package do
  ARCH_CONFIG.each do |arch_config|
    desc "Build installer and portable (#{arch_config.arch})"
    task arch_config.arch => [:"clean:#{arch_config.arch}", :"build:#{arch_config.arch}", :"installer:#{arch_config.arch}", :"portable:#{arch_config.arch}"]
  end
end

desc 'Build installer and portable for all architectures'
task :package => ARCH_CONFIG.map{ |x| :"package:#{x.arch}" }
