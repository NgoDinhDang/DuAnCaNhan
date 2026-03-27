using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using STOREBOOKS.Models;

namespace STOREBOOKS.Services
{
    public class MoMoService
    {
        private readonly MoMoConfig _config;
        private readonly ILogger<MoMoService> _logger;

        public MoMoService(MoMoConfig config, ILogger<MoMoService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task<MoMoPaymentResponse?> CreatePaymentAsync(string orderId, long amount, string orderInfo)
        {
            try
            {
                var requestId = Guid.NewGuid().ToString();
                var extraData = "";
                var requestType = "captureWallet"; // Chỉ dùng ví MoMo (QR code)
                var autoCapture = true;

                // Create raw signature theo thứ tự alphabet
                var rawSignature = $"accessKey={_config.AccessKey}" +
                                 $"&amount={amount}" +
                                 $"&extraData={extraData}" +
                                 $"&ipnUrl={_config.IpnUrl}" +
                                 $"&orderId={orderId}" +
                                 $"&orderInfo={orderInfo}" +
                                 $"&partnerCode={_config.PartnerCode}" +
                                 $"&redirectUrl={_config.ReturnUrl}" +
                                 $"&requestId={requestId}" +
                                 $"&requestType={requestType}";

                _logger.LogInformation($"Raw signature: {rawSignature}");

                var signature = ComputeHmacSha256(rawSignature, _config.SecretKey);

                var request = new MoMoPaymentRequest
                {
                    partnerCode = _config.PartnerCode,
                    partnerName = "STOREBOOKS",
                    storeId = "STOREBOOKS_STORE",
                    requestType = requestType, // captureWallet = Chỉ ví MoMo
                    ipnUrl = _config.IpnUrl,
                    redirectUrl = _config.ReturnUrl,
                    orderId = orderId,
                    amount = amount,
                    lang = "vi",
                    orderInfo = orderInfo,
                    requestId = requestId,
                    extraData = extraData,
                    signature = signature,
                    autoCapture = autoCapture
                };

                var jsonRequest = JsonConvert.SerializeObject(request, new JsonSerializerSettings 
                { 
                    NullValueHandling = NullValueHandling.Ignore 
                });
                
                _logger.LogInformation($"MoMo Request: {jsonRequest}");

                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(30);
                var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync(_config.Endpoint, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogInformation($"MoMo Response: {responseContent}");
                _logger.LogInformation($"HTTP Status Code: {response.StatusCode}");

                var paymentResponse = JsonConvert.DeserializeObject<MoMoPaymentResponse>(responseContent);
                return paymentResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating MoMo payment");
                return null;
            }
        }

        public bool ValidateSignature(MoMoExecuteResponse response)
        {
            try
            {
                var rawSignature = $"accessKey={_config.AccessKey}" +
                                 $"&amount={response.amount}" +
                                 $"&extraData={response.extraData}" +
                                 $"&message={response.message}" +
                                 $"&orderId={response.orderId}" +
                                 $"&orderInfo={response.orderInfo}" +
                                 $"&orderType={response.orderType}" +
                                 $"&partnerCode={response.partnerCode}" +
                                 $"&payType={response.payType}" +
                                 $"&requestId={response.requestId}" +
                                 $"&responseTime={response.responseTime}" +
                                 $"&resultCode={response.resultCode}" +
                                 $"&transId={response.transId}";

                var signature = ComputeHmacSha256(rawSignature, _config.SecretKey);
                return signature.Equals(response.signature, StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating MoMo signature");
                return false;
            }
        }

        private string ComputeHmacSha256(string message, string secretKey)
        {
            var keyBytes = Encoding.UTF8.GetBytes(secretKey);
            var messageBytes = Encoding.UTF8.GetBytes(message);

            using var hmac = new HMACSHA256(keyBytes);
            var hashBytes = hmac.ComputeHash(messageBytes);
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        }
    }
}

