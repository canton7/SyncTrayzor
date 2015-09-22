require 'open-uri'
require 'json'
require 'openssl'

class TxClient
  TX_BASE = 'https://www.transifex.com/api/2/project/%s'
  STATS_URL = TX_BASE + '/resource/strings/stats'
  TRANSLATION_URL = TX_BASE + '/resource/strings/translation/%s'

  attr_reader :language_exceptions

  def initialize(project, user, password)
    @project, @user, @password = project, user, password
  end

  def list_translations(completion_percent = 75)
    request(sprintf(STATS_URL, @project)).select do |lang, stats|
      stats['translated_entities'].fdiv(stats['translated_entities'] + stats['untranslated_entities']) * 100 > completion_percent
    end.keys
  end

  def download_translation(language, dest)
    content = request(sprintf(TRANSLATION_URL, @project, language))["content"]
    File.open(dest, 'w') do |f|
      f.write(content)
    end
  end

  private

  def request(uri)
    open(uri, ssl_verify_mode: OpenSSL::SSL::VERIFY_NONE, http_basic_authentication: [@user, @password]) do |f|
      JSON.parse(f.read)
    end
  end
end
