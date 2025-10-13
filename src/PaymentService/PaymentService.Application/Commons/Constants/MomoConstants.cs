namespace PaymentService.Application.Commons.Constants;

/// <summary>
/// MoMo constants and result codes from official documentation
/// Reference: https://developers.momo.vn/v2/#/docs/aiov2/
/// 
/// Note: IP whitelist is now loaded from environment variables (MOMO_IPN_WHITELIST)
/// See EnvironmentConfiguration.cs for configuration details
/// </summary>
public static class MomoConstants
{

    /// <summary>
    /// MoMo result codes
    /// </summary>
    public static class ResultCodes
    {
        public const int Success = 0;
        public const int InvalidSignature = 97;
        public const int InvalidAmount = 4001;
        public const int TransactionNotFound = 9000;
        public const int SystemError = 1000;
    }

    /// <summary>
    /// MoMo payment statuses
    /// </summary>
    public static class PaymentStatus
    {
        public const string Pending = "pending";
        public const string Success = "success";
        public const string Failed = "failed";
    }
}

