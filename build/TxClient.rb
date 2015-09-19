require 'open-uri'
require 'json'
require 'openssl'

class TxClient
  TX_BASE = 'https://www.transifex.com/api/2/project/synctrayzor'
  STATS_URL = TX_BASE + '/resource/strings/stats'
  TRANSLATION_URL = TX_BASE + '/resource/strings/translation/%s'

  def initialize(user, password)
    @user, @password = user, password
  end

  def request(uri)
    open(uri, ssl_verify_mode: OpenSSL::SSL::VERIFY_NONE, http_basic_authentication: [@user, @password]) do |f|
      JSON.parse(f.read)
    end
  end

  def list_translations(completion_percent = 75)
    request(STATS_URL).select do |lang, stats|
      stats['translated_entities'].fdiv(stats['translated_entities'] + stats['untranslated_entities']) * 100 > completion_percent
    end.keys
  end
end

p TxClient.new('canton7', '').list_translations
