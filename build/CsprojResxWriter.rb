require 'rexml/document'

class CsprojResxWriter
  attr_reader :language_exceptions

  def initialize(csproj_path, relative_resx_path)
    @csproj_path, @relative_resx_path = csproj_path, relative_resx_path

    @language_exceptions = {
      'ca@valencia' => 'ca-ES-valencia',
    }
  end

  def absolute_resx_path_for_language(language)
    File.join(File.dirname(@csproj_path), relative_resx_path_for_language(language))
  end

  def remove_all_resx
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

  def read_and_sort_source_resx
    doc = REXML::Document.new(IO.read(File.join(File.dirname(@csproj_path), @relative_resx_path, "Resources.resx")), { :attribute_quote => :quote })
    elements = doc.get_elements('//root/data')
    elements.sort_by!{ |x| x.attributes['name'] }
    elements.each do |element|
      parent = element.parent
      parent.delete(element)
      parent.add(element)
    end
    output = ""
    doc.write(output)
    output
  end

  private

  def relative_resx_path_for_language(language)
    normalized_language = @language_exceptions.has_key?(language) ? @language_exceptions[language] : language.sub('_', '-')
    File.join(@relative_resx_path, "Resources.#{normalized_language}.resx")
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
