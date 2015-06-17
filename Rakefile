require 'tmpdir'
require 'open-uri'
require 'openssl'

ISCC = ENV['ISCC'] || 'C:\Program Files (x86)\Inno Setup 5\ISCC.exe'
SZIP = ENV['SZIP'] || 'C:\Program Files\7-Zip\7z.exe'
SIGNTOOL = ENV['SIGNTOOL'] || 'C:\Program Files (x86)\Microsoft SDKs\Windows\v7.1A\Bin\signtool.exe'
MSBUILD = ENV['MSBUILD'] || %q{C:\Program Files (x86)\MSBuild\12.0\Bin\MSBuild.exe}

CONFIG = ENV['CONFIG'] || 'Release'
MSBUILD_LOGGER = ENV['MSBUILD_LOGGER']

SRC_DIR = 'src/SyncTrayzor'
INSTALLER_DIR = 'installer'
DEPLOY_DIR = 'deploy'

SLN = 'src/SyncTrayzor.sln'

CHECKSUM_UTIL_CSPROJ = 'src/ChecksumUtil/ChecksumUtil.csproj'
CHECKSUM_UTIL_EXE = 'bin/ChecksumUtil/Release/ChecksumUtil.exe'
SYNCTHING_RELEASES_CERT = 'security/syncthing_releases_cert.asc'

CHECKSUM_FILE_PRIV_KEY = 'security/private_key.asc'

PFX = ENV['PFX'] || File.join(INSTALLER_DIR, 'SyncTrayzorCA.pfx')

PORTABLE_SYNCTHING_VERSION = '0.11'

class ArchDirConfig
  attr_reader :arch
  attr_reader :bin_dir
  attr_reader :installer_dir
  attr_reader :installer_output
  attr_reader :installer_iss
  attr_reader :portable_output_dir
  attr_reader :portable_output_file
  attr_reader :syncthing_binaries

  def initialize(arch, github_arch)
    @arch = arch
    @github_arch = github_arch
    @bin_dir = "bin/#{@arch}/#{CONFIG}"
    @installer_dir = File.join(INSTALLER_DIR, @arch)
    @installer_output = File.join(@installer_dir, "SyncTrayzorSetup-#{@arch}.exe")
    @installer_iss = File.join(@installer_dir, "installer-#{@arch}.iss")
    @portable_output_dir = "SyncTrayzorPortable-#{@arch}"
    @portable_output_file = File.join(DEPLOY_DIR, "SyncTrayzorPortable-#{@arch}.zip")
    @syncthing_binaries = { '0.11' => 'syncthing.exe' }
  end

  def sha1sum_download_uri(version)
    "https://github.com/syncthing/syncthing/releases/download/v#{version}/sha1sum.txt.asc"
  end

  def download_uri(version)
  	"https://github.com/syncthing/syncthing/releases/download/v#{version}/syncthing-windows-#{@github_arch}-v#{version}.zip"
  end
end

SYNCTHING_VERSIONS_TO_UPDATE = ['0.11']

ARCH_CONFIG = [ArchDirConfig.new('x64', 'amd64'), ArchDirConfig.new('x86', '386')]
ASSEMBLY_INFOS = FileList['**/AssemblyInfo.cs']

def ensure_7zip
  unless File.exist?(SIGNTOOL)
    warn "You must install the Windows SDK"
    exit 1
  end
end

def build(sln, platform, rebuild = true)
  tasks = rebuild ? 'Clean;Rebuild' : 'Build'
  cmd = "\"#{MSBUILD}\" \"#{sln}\" /t:#{tasks} /p:Configuration=#{CONFIG};Platform=#{platform}"
  if MSBUILD_LOGGER
    cmd << " /logger:\"#{MSBUILD_LOGGER}\" /verbosity:minimal"
  else
    cmd << " /verbosity:quiet"
  end
  
  sh cmd
end

namespace :build do
  ARCH_CONFIG.each do |arch_config|
    desc "Build the project (#{arch_config.arch})"
    task arch_config.arch do
      build(SLN, arch_config.arch)
    end
  end
end

desc 'Build both 64-bit and 32-bit binaries'
task :build => ARCH_CONFIG.map{ |x| :"build:#{x.arch}" }

task :"build-checksum-util" do
  build(CHECKSUM_UTIL_CSPROJ, 'AnyCPU', false)
end

namespace :installer do
  ARCH_CONFIG.each do |arch_config|
    desc "Create the installer (#{arch_config.arch})"
    task arch_config.arch do
      unless File.exist?(ISCC)
        warn "Please install Inno Setup" 
        exit 1
      end

      rm arch_config.installer_output if File.exist?(arch_config.installer_output)
      sh %Q{"#{ISCC}" #{arch_config.installer_iss}}

      mkdir_p DEPLOY_DIR
      mv arch_config.installer_output, DEPLOY_DIR
    end
  end
end

desc 'Build both 64-bit and 32-bit installers'
task :installer => ARCH_CONFIG.map{ |x| :"installer:#{x.arch}" }

namespace :"sign-installer" do
  ARCH_CONFIG.each do |arch_config|
    desc "Sign the installer (#{arch_config.arch}). Specify PASSWORD if required"
    task arch_config.arch do
      ensure_7zip

      unless File.exist?(PFX)
        warn "#{PFX} must exist"
        exit 1
      end

      args = "sign /f #{PFX} /t http://timestamp.verisign.com/scripts/timstamp.dll"
      args << " /p #{ENV['PASSWORD']}" if ENV['PASSWORD']
      args << " /v #{File.join(DEPLOY_DIR, File.basename(arch_config.installer_output))}"

      # Don't want to print out the pasword!
      puts "Invoking signtool"
      system %Q{"#{SIGNTOOL}" #{args}}
    end
  end
end

desc 'Sign both 64-bit and 32-bit installers. Specify PASSWORD if required'
task :"sign-installer" => ARCH_CONFIG.map{ |x| :"sign-installer:#{x.arch}" }

def cp_to_portable(output_dir, src, output_filename = nil)
  dest = File.join(output_dir, output_filename || src)
  raise "Cannot find #{src}" unless File.exist?(src)
  # It could be an empty directory - so ignore it
  # We'll create it as and when if there are any files in it
  if File.file?(src)
    mkdir_p File.dirname(dest) unless File.exist?(File.dirname(dest))
    cp src, dest
  end
end

namespace :portable do
  ARCH_CONFIG.each do |arch_config|
    desc "Create the portable package (#{arch_config.arch})"
    task arch_config.arch do
      ensure_7zip

      mkdir_p File.dirname(arch_config.portable_output_file)
      rm arch_config.portable_output_file if File.exist?(arch_config.portable_output_file)

      Dir.mktmpdir do |tmp|
        portable_dir = File.join(tmp, arch_config.portable_output_dir)
        Dir.chdir(arch_config.bin_dir) do
          files = FileList['**/*'].exclude(
            '*.xml', '*.vshost.*', '*.log', '*.Installer.config', '*/FluentValidation.resources.dll',
            '*/System.Windows.Interactivity.resources.dll', 'syncthing.exe', 'data/*', 'logs')

          files.each do |file|
            cp_to_portable(portable_dir, file)
          end
        end

        cp File.join(SRC_DIR, 'Icons', 'default.ico'), arch_config.portable_output_dir

        FileList['*.md', '*.txt'].each do |file|
          cp_to_portable(portable_dir, file)
        end
        
        Dir.chdir(arch_config.installer_dir) do
          FileList['*.dll'].each do |file|
            cp_to_portable(portable_dir, file)
          end
          cp_to_portable(portable_dir, arch_config.syncthing_binaries[PORTABLE_SYNCTHING_VERSION], 'syncthing.exe')
        end

        sh %Q{"#{SZIP}" a -tzip -mx=7 #{arch_config.portable_output_file} #{portable_dir}}
      end
    end
  end
end

desc 'Create both 64-bit and 32-bit portable packages'
task :portable => ARCH_CONFIG.map{ |x| :"portable:#{x.arch}" }

namespace :"update-syncthing" do
  ARCH_CONFIG.each do |arch_config|
    desc "Update syncthing binaries (#{arch_config.arch}"
    task arch_config.arch do
      arch_config.syncthing_binaries.values_at(*SYNCTHING_VERSIONS_TO_UPDATE).each do |bin|
        path = File.join(arch_config.installer_dir, bin)
        raise "Could not find #{path}" unless File.exist?(path)
        sh path, '-upgrade' do; end

        old_bin = "#{path}.old"
        rm old_bin if File.exist?(old_bin)
      end
    end
  end
end

desc 'Update syncthing binaries, all architectures'
task :"update-syncthing" => ARCH_CONFIG.map{ |x| :"update-syncthing:#{x.arch}" }

desc 'Create sha1sums file' 
task :"create-sha1sums" => [:"build-checksum-util"] do
  password = ENV['PASSWORD'] || '""'
  checksum_file = File.join(DEPLOY_DIR, 'sha1sum.txt.asc')
  rm checksum_file if File.exist?(checksum_file)

  args = %Q{create "#{checksum_file}" sha1 "#{CHECKSUM_FILE_PRIV_KEY}" "#{password}" } + Dir["#{DEPLOY_DIR}/**"].map{ |x| "\"#{x}\"" }.join(' ')

  # Don't want to print out the pasword!
  puts "Invoking #{CHECKSUM_UTIL_EXE}"
  system %Q{"#{CHECKSUM_UTIL_EXE}" #{args}}
end

desc 'Clean portable and installer, all architectures'
task :clean do
  rm_rf DEPLOY_DIR if File.exist?(DEPLOY_DIR)
end

namespace :package do
  ARCH_CONFIG.each do |arch_config|
    desc "Build installer and portable (#{arch_config.arch})"
    task arch_config.arch => 
      [
        :"update-syncthing:#{arch_config.arch}",
        :"build:#{arch_config.arch}",
        :"installer:#{arch_config.arch}",
        :"sign-installer:#{arch_config.arch}",
        :"portable:#{arch_config.arch}"
      ]
  end
end

desc 'Build installer and portable for all architectures'
task :package => [:clean, *ARCH_CONFIG.map{ |x| :"package:#{x.arch}" }, :"create-sha1sums"]

desc "Bump version number"
task :version, [:version] do |t, args|
  parts = args[:version].split('.')
  parts << '0' if parts.length == 3
  version = parts.join('.')
  ASSEMBLY_INFOS.each do |info|
    content = IO.read(info)
    content[/^\[assembly: AssemblyVersion\(\"(.+?)\"\)\]/, 1] = version
    content[/^\[assembly: AssemblyFileVersion\(\"(.+?)\"\)\]/, 1] = version
    File.open(info, 'w'){ |f| f.write(content) }
  end
end

namespace :"download-syncthing" do
  ARCH_CONFIG.each do |arch_config|
    desc "Download syncthing (#{arch_config.arch})"
    task arch_config.arch, [:version]  => [:"build-checksum-util"] do |t, args|
      ensure_7zip

      Dir.mktmpdir do |tmp|
        download_file = File.join(tmp, File.basename(arch_config.download_uri(args[:version])))
        File.open(download_file, 'wb') do |outfile|
          open(arch_config.download_uri(args[:version]), { ssl_verify_mode: OpenSSL::SSL::VERIFY_NONE }) do |infile|
            outfile.write(infile.read)
          end
        end

        File.open(File.join(tmp, 'sha1sum.txt.asc.'), 'w') do |outfile|
          open(arch_config.sha1sum_download_uri(args[:version]), { ssl_verify_mode: OpenSSL::SSL::VERIFY_NONE }) do |infile|
            outfile.write(infile.read)
          end
        end

        sh CHECKSUM_UTIL_EXE, 'verify', 'sha1', File.join(tmp, 'sha1sum.txt.asc'), SYNCTHING_RELEASES_CERT, download_file

        Dir.chdir(tmp) do
          sh %Q{"#{SZIP}" e #{File.basename(download_file)}}
        end

        cp File.join(tmp, 'syncthing.exe'), File.join(arch_config.installer_dir, 'syncthing.exe')
      end
    end
  end
end

desc 'Download syncthing for all architectures'
task :"download-syncthing", [:version] => ARCH_CONFIG.map{ |x| :"download-syncthing:#{x.arch}" }