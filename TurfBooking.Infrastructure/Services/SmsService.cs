using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services
{
    public class SmsService : ISmsService
    {
        private readonly ILogger<SmsService> _logger;
        private readonly IConfiguration _configuration;
        private static readonly HttpClient _httpClient = new();

        public SmsService(ILogger<SmsService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public async Task SendSmsAsync(string phoneNumber, string message)
        {
            var accountSid = _configuration["TwilioSettings:AccountSid"];
            var authToken = _configuration["TwilioSettings:AuthToken"];
            var fromNumber = _configuration["TwilioSettings:FromPhoneNumber"];

            if (!string.IsNullOrWhiteSpace(accountSid) && 
                !string.IsNullOrWhiteSpace(authToken) && 
                !string.IsNullOrWhiteSpace(fromNumber) && 
                !accountSid.Contains("YOUR_TWILIO_ACCOUNT_SID"))
            {
                try
                {
                    if (!phoneNumber.StartsWith("+91"))
                    {
                        phoneNumber = $"+91{phoneNumber}";
                    }

                    _logger.LogInformation("Attempting to send real SMS to {PhoneNumber} via Twilio API...", phoneNumber);
                    
                    var requestUrl = $"https://api.twilio.com/2010-04-01/Accounts/{accountSid}/Messages.json";
                    
                    using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
                    
                    var authBytes = Encoding.ASCII.GetBytes($"{accountSid}:{authToken}");
                    request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authBytes));

                    var postParams = new List<KeyValuePair<string, string>>
                    {
                        new("To", phoneNumber),
                        new("From", fromNumber),
                        new("Body", message)
                    };

                    request.Content = new FormUrlEncodedContent(postParams);

                    var response = await _httpClient.SendAsync(request);
                    var responseBody = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        _logger.LogInformation("SMS successfully sent to {PhoneNumber} via Twilio.", phoneNumber);
                    }
                    else
                    {
                        _logger.LogError("Twilio SMS send failed with status code {StatusCode}. Response: {Response}", response.StatusCode, responseBody);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Exception occurred while sending SMS via Twilio to {PhoneNumber}.", phoneNumber);
                }
            }
            else
            {
                _logger.LogWarning("Twilio settings are not fully configured in appsettings.json. Falling back to local console mock.");
                _logger.LogInformation("=========================================");
                _logger.LogInformation("SMS GATEWAY (Twilio/MSG91 Mock)");
                _logger.LogInformation("To: {PhoneNumber}", phoneNumber);
                _logger.LogInformation("Message: {Message}", message);
                _logger.LogInformation("=========================================");
            }
        }
    }
}
