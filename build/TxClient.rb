require 'open-uri'
require 'json'
require 'openssl'
require 'rexml/document'

class TxClient
  TX_BASE = 'https://www.transifex.com/api/2/project/%s'
  STATS_URL = TX_BASE + '/resource/strings/stats'
  TRANSLATION_URL = TX_BASE + '/resource/strings/translation/%s'

  attr_reader :language_exceptions

  def initialize(project, user, password, csproj_path, relative_resx_path)
    @project, @user, @password, @csproj_path, @relative_resx_path = project, user, password, csproj_path, relative_resx_path

    @language_exceptions = {
      'ca@valencia' => 'ca-ES-valencia',
    }
  end

  def list_translations(completion_percent = 75)
    request(sprintf(STATS_URL, @project)).select do |lang, stats|
      stats['translated_entities'].fdiv(stats['translated_entities'] + stats['untranslated_entities']) * 100 > completion_percent
    end.keys
  end

  def add_translation(language)
    download_and_write_resx(language)
    add_resx_to_csproj(language)
  end

  def clean_translations
    remove_all_resx_from_csproj
  end

  private

  def request(uri)
    open(uri, ssl_verify_mode: OpenSSL::SSL::VERIFY_NONE, http_basic_authentication: [@user, @password]) do |f|
      JSON.parse(f.read)
    end
  end

  def relative_resx_path_for_language(language)
    normalized_language = @language_exceptions.has_key?(language) ? @language_exceptions[language] : language.sub('_', '-')
    File.join(@relative_resx_path, "Resources.#{normalized_language}.resx")
  end

  def absolute_resx_path_for_language(language)
    File.join(File.dirname(@csproj_path), relative_resx_path_for_language(language))
  end

  def download_and_write_resx(language)
    content = request(sprintf(TRANSLATION_URL, @project, language))["content"]
    File.open(absolute_resx_path_for_language(language), 'w') do |f|
      f.write(content)
    end
  end

  def remove_all_resx_from_csproj
    csproj = parse_csproj()

    csproj.each_element("//Project/ItemGroup/EmbeddedResource") do |resource|
      next unless resource.attributes['Include'] =~ /^#{Regexp.quote(@relative_resx_path.gsub('/', '\\'))}\\Resources\.\S+\.resx$/
      File.delete(File.join(File.dirname(@csproj_path), resource.attributes['Include']))
      resource.parent.delete(resource)
    end

    csproj.each_element("//Project/ItemGroup/Compile") do |resource|
      next unless resource.attributes['Include'] =~ /^#{Regexp.quote(@relative_resx_path.gsub('/', '\\'))}\\Resources\.\S+\.Designer.cs$/
      File.delete(File.join(File.dirname(@csproj_path), resource.attributes['Include']))
      resource.parent.delete(resource)
    end

    write_csproj(csproj)
  end

  def add_resx_to_csproj(language)
    csproj = parse_csproj()

    main_resx_name = @relative_resx_path.gsub('/', '\\') + '\\Resources.resx'
    item_group = csproj.elements["//Project/ItemGroup[.//EmbeddedResource[@Include='#{main_resx_name}']]"]

    item_group.add_element('EmbeddedResource', { 'Include' => relative_resx_path_for_language(language).gsub('/', '\\') })

    write_csproj(csproj)
  end

  def parse_csproj
    REXML::Document.new(IO.read(@csproj_path), { :attribute_quote => :quote })
  end

  def write_csproj(doc)
    formatter = REXMLFormatter.new(2, true) # Add a space before a closing slash
    formatter.width = 10000
    formatter.compact = true
    File.open(@csproj_path, 'w') do |f|
      f.write "\uFEFF"
      f.puts '<?xml version="1.0" encoding="utf-8"?>'
      formatter.write(doc.root, f)
    end
  end

  class REXMLFormatter < REXML::Formatters::Pretty
    def write_element(node, output)
      if node.children.length == 1 && node.text =~ /\A[\n\s]+\z/m && node.attributes.empty?
        output << ' '*@level
        output << "<#{node.expanded_name}>\n"
        output << ' '*@level
        output << "</#{node.expanded_name}>"
      else
        super
      end
    end
  end
end
