namespace SsiSdk;

internal static class Constants
{
    public const int DefaultTimeout = 60;
    public const int DefaultMaxRetries = 5;
    public const double DefaultRetryDelay = 2.0;
    public const int DefaultRateLimitPerSecond = 10;
    public const int DefaultSize = 1000;
    public const int DefaultPage = 1;
    public const int WsThreadJoinTimeoutMs = 5000;

    public const string HeaderContentType = "Content-Type";
    public const string HeaderAccept = "Accept";
    public const string HeaderAuthorization = "Authorization";
    public const string HeaderRetryAfter = "Retry-After";
    public const string HeaderSignature = "X-Signature";
    public const string ContentTypeJson = "application/json";
    public const string AuthSchemeBearer = "Bearer ";

    public const string DefaultApiUrl = "https://api.ssi.com.vn";
    public const string DefaultStreamingUrl = "wss://stream.ssi.com.vn/ws/v3";

    public const string EpAccessToken = "/api/v3/auth/token";
    public const string EpRefreshToken = "/api/v3/auth/refresh";
    public const string EpRequestOtp = "/api/v3/auth/requestOtp";

    public const string EpDataOhlc = "/api/v3/data/ohlc";
    public const string EpDataOhlcDownload = "/api/v3/data/ohlc/download";
    public const string EpDataIndexList = "/api/v3/data/indexList";
    public const string EpDataIndexSummary = "/api/v3/data/indexSummary";
    public const string EpDataSecuritiesByBoard = "/api/v3/data/securitiesByBoard";
    public const string EpDataSecuritiesSummary = "/api/v3/data/securitiesSummary";

    public const string EpTradingOrder = "/api/v3/trading/order";
    public const string EpTradingMaxBuySell = "/api/v3/trading/maxBuySell";

    public const string EpAccountInfo = "/api/v3/account/info";
    public const string EpAccountBalance = "/api/v3/trading/accountBalance";
    public const string EpAccountPpmmr = "/api/v3/trading/ppmmrAccount";
    public const string EpPositions = "/api/v3/trading/position";
    public const string EpOrderHistory = "/api/v3/trading/orderBook";
}
