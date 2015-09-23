require 'json'
require 'net/http'
require 'uri'
require 'openssl'

class TxClient
  TX_BASE = 'https://www.transifex.com/api/2/project/%s'
  STATS_URL = '/resource/%s/stats'
  TRANSLATION_URL = '/resource/strings/translation/%s'
  CONTENT_URL = '/resource/%s/content'

  attr_reader :language_exceptions

  def initialize(project, resource, user, password)
    @project, @resource, @user, @password = project, resource, user, password
  end

  def list_translations(completion_percent = 75)
    get_request(stats_url).select do |lang, stats|
      stats['translated_entities'].fdiv(stats['translated_entities'] + stats['untranslated_entities']) * 100 > completion_percent
    end.keys
  end

  def download_translation(language, dest)
    content = get_request(translation_url(language))["content"]
    File.open(dest, 'w') do |f|
      f.write(content)
    end
  end

  def upload_source(source)
    put_request(content_url, JSON.generate({ 'content' => source }))
  end

  private

  def stats_url
    sprintf(TX_BASE, @project) + sprintf(STATS_URL, @resource)
  end

  def translation_url(language)
    sprintf(TX_BASE, @project) + sprintf(TRANSLATION_URL, language)
  end

  def content_url
    sprintf(TX_BASE, @project) + sprintf(CONTENT_URL, @resource)
  end

  def get_request(url)
    uri = URI.parse(url)
    start_http(uri) do |http|
      request = Net::HTTP::Get.new(uri.request_uri)
      request.basic_auth(@user, @password)
      response = http.request(request)
      JSON.parse(response.body)
    end
  end

  def put_request(url, body)
    uri = URI.parse(url)
    start_http(uri) do |http|
      request = Net::HTTP::Put.new(uri.request_uri)
      request['Content-Type'] = 'application/json'
      request.basic_auth(@user, @password)
      request.body = body
      response = http.request(request)
      JSON.parse(response.body)
    end
  end

  def start_http(uri)
    options = uri.scheme == 'https' ? { :use_ssl => true, :verify_mode => OpenSSL::SSL::VERIFY_NONE } : {}
    Net::HTTP.start(uri.host, uri.port, options) do |http|
      yield http
    end
  end
end
