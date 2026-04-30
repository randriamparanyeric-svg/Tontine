using System.Net.Http.Headers;

namespace Tontine.Services {
    public interface ISmsService { Task<bool> EnvoyerSmsAsync(string mobile, string message); }

    public class SmsService : ISmsService {
        private readonly HttpClient _http;
        private readonly IConfiguration _conf;

        public SmsService(HttpClient http, IConfiguration conf) { _http = http; _conf = conf; }

        public async Task<bool> EnvoyerSmsAsync(string mobile, string message) {
            //var token = _conf["Traccar:Token"];
            var token = "c3c9b551-0c60-43c3-8f79-47ea99827f5e";
            var payload = new { to = mobile, message = message };
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
           // var response = await _http.PostAsJsonAsync("https://www.traccar.org/sms/", payload);
            
var response = await _http.PostAsJsonAsync("http://192.168.43.1:8082/", payload);
            return response.IsSuccessStatusCode;
        }
    }
}