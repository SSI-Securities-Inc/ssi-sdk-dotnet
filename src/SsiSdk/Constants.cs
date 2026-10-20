namespace SsiSdk;

public static class Constants
{
    public const int DefaultTimeoutMs = 60_000;
    public const int DefaultMaxRetries = 5;
    public const int DefaultRetryDelayMs = 2_000;
    public const int DefaultRateLimitPerSecond = 10;
    public const int DefaultSize = 1000;
    public const int DefaultPage = 1;
    public const int WsThreadJoinTimeoutMs = 5000;

    public const string HeaderContentType = "Content-Type";
    public const string HeaderAccept = "Accept";
    public const string HeaderAuthorization = "Authorization";
    public const string HeaderRetryAfter = "Retry-After";
    public const string HeaderRateLimitLimit = "X-RateLimit-Limit";
    public const string HeaderRateLimitRemaining = "X-RateLimit-Remaining";
    public const string HeaderRateLimitReset = "X-RateLimit-Reset";
    public const string HeaderSignature = "X-Signature";
    public const string HeaderUserAgent = "User-Agent";
    public const string DefaultUserAgent = "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";
    public const string ContentTypeJson = "application/json";
    public const string AuthSchemeBearer = "Bearer ";

    public const string DefaultApiUrl = "https://api.ssi.com.vn";
    public const string DefaultStreamingUrl = "wss://stream.ssi.com.vn/ws/v3";
    public const string DefaultTradingWsPath = "/ws/v3/trading";
    public const string DefaultTradingWsUrl = "wss://api.ssi.com.vn/ws/v3/trading";

    public static readonly string[] TradingApiPathPrefixes = ["/api/v3/trading", "/api/v3/account"];

    public const string EpAccessToken = "/api/v3/auth/token";
    public const string EpRefreshToken = "/api/v3/auth/refresh";
    public const string EpRequestOtp = "/api/v3/auth/requestOtp";

    public const int SmartOtpPendingStatus = 202;
    public const int SmartOtpPendingCode = 401114;
    public const int HttpStatusConflict = 409;
    public const int HttpStatusTooManyRequests = 429;

    public static readonly int[] SmartOtpTerminalCodes = [401113, 401115, 401116];
    public static readonly int[] RefreshReauthCodes = [401101, 401102, 401103, 401104, 401106, 401107, 401108];

    public const int TokenExpirySkewMs = 30_000;
    public const int DefaultOtpPollIntervalMs = 5_000;
    public const int DefaultOtpPollMaxWaitMs = 60_000;

    public const int RateLimitMaxRetries = 3;
    public const int RateLimitDefaultWaitMs = 1_000;
    public const int RateLimitMaxWaitMs = 30_000;

    public const int MaxClientRequestIdLength = 20;
    public const int MaxBatchOrders = 20;
    public const int MaxFcoWindowDays = 31;

    public const int WsReconnectMaxDelayMs = 60_000;
    public const int WsRateLimitBackoffMs = 30_000;
    public const int WsTokenRefreshLeadMs = 60_000;
    public const int WsServerMaxConnections = 10;
    public const int WsMaxFrameBytes = 4000;
    public const int WsStableMs = 10_000;

    public const string WsRouteAck = "@ack";
    public const string WsRouteError = "@error";
    public const string WsRouteListSubscription = "@list_subscription";
    public const string WsRouteId = "@id";
    public const string WsKeyChannel = "channel";

    public const string TopicMarketFlag = "market.flag";

    public const string EpDataOhlc = "/api/v3/data/ohlc";
    public const string EpDataOhlcDownload = "/api/v3/data/ohlc/download";
    public const string EpDataIndexList = "/api/v3/data/indexList";
    public const string EpDataIndexSummary = "/api/v3/data/indexSummary";
    public const string EpDataSecuritiesByBoard = "/api/v3/data/securitiesByBoard";
    public const string EpDataSecuritiesSummary = "/api/v3/data/securitiesSummary";
    public const string EpDataMasterData = "/api/v3/data/masterdata";
    public const string EpDataCorporateActions = "/api/v3/data/corporateActions";

    // corporateActions: the span of one request is capped at 5 days, pageSize must be one of these, language is vn or en.
    public const int CorporateActionsMaxDays = 5;
    public static readonly int[] CorporateActionsPageSizes = [10, 20, 50, 100];
    public static readonly string[] CorporateActionsLanguages = ["vn", "en"];

    public const string EpTradingOrder = "/api/v3/trading/order";
    public const string EpTradingOrderBatch = "/api/v3/trading/order/batch";
    public const string EpTradingMaxBuySell = "/api/v3/trading/maxBuySell";
    public const string EpTradingFcoOrder = "/api/v3/trading/fco/order";
    public const string EpTradingFcoList = "/api/v3/trading/fco/list";
    public const string EpTradingFcoOrderBook = "/api/v3/trading/fco/orderbook";
    public const string EpTradingFcoStatusHistory = "/api/v3/trading/fco/statusHistory";

    public const string EpAccountInfo = "/api/v3/account/info";
    public const string EpAccountBalance = "/api/v3/trading/accountBalance";
    public const string EpAccountPpmmr = "/api/v3/trading/ppmmrAccount";
    public const string EpPositions = "/api/v3/trading/position";
    public const string EpOrderHistory = "/api/v3/trading/orderBook";
}
