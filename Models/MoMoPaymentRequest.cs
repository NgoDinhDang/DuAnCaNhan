namespace STOREBOOKS.Models
{
    public class MoMoPaymentRequest
    {
        public string partnerCode { get; set; } = string.Empty;
        public string partnerName { get; set; } = string.Empty;
        public string storeId { get; set; } = string.Empty;
        public string requestType { get; set; } = "captureWallet"; // Chỉ ví MoMo (QR)
        public string ipnUrl { get; set; } = string.Empty;
        public string redirectUrl { get; set; } = string.Empty;
        public string orderId { get; set; } = string.Empty;
        public long amount { get; set; }
        public string lang { get; set; } = "vi";
        public string orderInfo { get; set; } = string.Empty;
        public string requestId { get; set; } = string.Empty;
        public string extraData { get; set; } = string.Empty;
        public string signature { get; set; } = string.Empty;
        public bool autoCapture { get; set; } = true;
    }

    public class MoMoPaymentResponse
    {
        public string partnerCode { get; set; } = string.Empty;
        public string orderId { get; set; } = string.Empty;
        public string requestId { get; set; } = string.Empty;
        public long amount { get; set; }
        public long responseTime { get; set; }
        public string message { get; set; } = string.Empty;
        public int resultCode { get; set; }
        public string payUrl { get; set; } = string.Empty;
        public string deeplink { get; set; } = string.Empty;
        public string qrCodeUrl { get; set; } = string.Empty;
    }

    public class MoMoExecuteResponse
    {
        public string partnerCode { get; set; } = string.Empty;
        public string orderId { get; set; } = string.Empty;
        public string requestId { get; set; } = string.Empty;
        public long amount { get; set; }
        public string orderInfo { get; set; } = string.Empty;
        public string orderType { get; set; } = string.Empty;
        public long transId { get; set; }
        public int resultCode { get; set; }
        public string message { get; set; } = string.Empty;
        public string payType { get; set; } = string.Empty;
        public long responseTime { get; set; }
        public string extraData { get; set; } = string.Empty;
        public string signature { get; set; } = string.Empty;
    }

    public class MoMoConfig
    {
        public string PartnerCode { get; set; } = "MOMO";
        public string AccessKey { get; set; } = "F8BBA842ECF85";
        public string SecretKey { get; set; } = "K951B6PE1waDMi640xX08PD3vg6EkVlz";
        public string Endpoint { get; set; } = "https://test-payment.momo.vn/v2/gateway/api/create";
        public string ReturnUrl { get; set; } = string.Empty;
        public string IpnUrl { get; set; } = string.Empty;
    }
}

