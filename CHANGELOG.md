# Changelog

All notable changes to `SSIDeveloper.FastConnect.Sdk` (.NET). The version lives in `src/SsiSdk/SsiSdk.csproj`
(`<Version>`) and `src/SsiSdk/Version.cs` (`SdkVersion.Value`); the NuGet publish workflow takes the package version from the git tag.

Items marked **BREAKING** change behaviour or signatures callers may depend on; the rest are additive or fixes.
Renamed members keep their old name as an `[Obsolete]` alias (except the 3.3.0 millisecond renames below, which have none).

3.3.0 brings the .NET SDK level with the Python SDK 3.3.0. Python's sync/async pairs collapse into the single async API
(`Task` + `CancellationToken`) .NET already had; numbers that were `double` are now `decimal`.

## 3.3.0 - 2026-10-08

### Security
- Secrets no longer reach logs or exception text: the REST client logs only method and path (before 3.3.0 it also logged the
  request body and the signature); the WebSocket client no longer logs the `Authorization` header, raw frames or URL
  query strings; `Config.ToString()`, `Token` and the request models hide `ApiSecret`, `PrivateKey`, tokens, OTP and
  `TransactionId`; the body kept on an exception (`ResponseBody`) is masked (`Redact`).
- `AuthClient(config)` / `DataClient(...)` etc. no longer mutate the caller's `Config`.
- **Action for the owner:** older versions (3.2.x and below) wrote the Bearer token, the request body and the signature to the
  log. Revoke/rotate any token that was logged, and review log retention for those files.

### Durations are milliseconds
- **BREAKING:** every public duration is an integer number of milliseconds with an `Ms` suffix (no `TimeSpan`, no seconds).
  The old names are removed, there are no aliases: a compile error tells you where to change. Convert old values
  (seconds) by multiplying by 1000. `Retry-After` is still read as seconds on the wire and exposed in ms.

| Before (3.2.x / earlier 3.3.0 drafts) | Now | Type, default |
|---|---|---|
| `Config.TimeoutSeconds` | `Config.TimeoutMs` | `int`, `60000` |
| `Config.RetryDelay` (`double`, s) | `Config.RetryDelayMs` | `int`, `2000` |
| `Config.OtpPollInterval` (`double`, s) | `Config.OtpPollIntervalMs` | `int`, `5000` |
| `Config.OtpPollMaxWait` (`double`, s) | `Config.OtpPollMaxWaitMs` | `int`, `60000` |
| `RateLimitException.RetryAfter` (`double?`, s) | `RateLimitException.RetryAfterMs` | `long?` |
| `EnsureAuthenticatedAsync(pollInterval: TimeSpan?)` | `EnsureAuthenticatedAsync(pollIntervalMs: int?)` | |
| `WaitAsync(TimeSpan? timeout)` (client and streaming) | `WaitAsync(int? timeoutMs)` | |
| `PingAsync(double? intervalSeconds)` | `PingAsync(int? intervalMs)` | |
| `ListSubscriptionAsync(double timeoutSeconds = 5.0)` | `ListSubscriptionAsync(int timeoutMs = 5000)` | |
| `TradingWsOptions.RequestTimeout` (`TimeSpan?`) | `TradingWsOptions.RequestTimeoutMs` | `int?`, null takes `Config.TimeoutMs` |
| `TradingWsOptions.HandshakeGrace` (`TimeSpan`) | `TradingWsOptions.HandshakeGraceMs` | `int`, `500` |
| `TradingWsOptions.MaxRateLimitWait` (`TimeSpan`) | `TradingWsOptions.MaxRateLimitWaitMs` | `int`, `2000` |
| `TradingWsOptions.PingInterval` (`TimeSpan?`) | `TradingWsOptions.PingIntervalMs` | `int?`, `30000` |
| `Constants.DefaultTimeout` | `Constants.DefaultTimeoutMs` | `60000` |
| `Constants.DefaultRetryDelay` | `Constants.DefaultRetryDelayMs` | `2000` |
| `Constants.DefaultOtpPollInterval` / `DefaultOtpPollMaxWait` | `Constants.DefaultOtpPollIntervalMs` / `DefaultOtpPollMaxWaitMs` | `5000` / `60000` |
| `Constants.TokenExpirySkew` | `Constants.TokenExpirySkewMs` | `30000` |
| `Constants.RateLimitDefaultWait` / `RateLimitMaxWait` | `Constants.RateLimitDefaultWaitMs` / `RateLimitMaxWaitMs` | `1000` / `30000` |
| `Constants.WsReconnectMaxDelay` | `Constants.WsReconnectMaxDelayMs` | `60000` |
| `Constants.WsRateLimitBackoff` | `Constants.WsRateLimitBackoffMs` | `30000` |
| `Constants.WsTokenRefreshLead` | `Constants.WsTokenRefreshLeadMs` | `60000` |
| `Constants.WsStableSeconds` | `Constants.WsStableMs` | `10000` |

### Parity with Python 3.3.0
- **BREAKING:** `TradingWsError` moved from `SsiSdk.Experimental` to `SsiSdk` and derives from `WebSocketException`
  (`Code` / `StatusCode` are filled from the server frame).
- **BREAKING:** `TradingWs.ConnectAsync` fills a null `TradingWsOptions.RequestTimeoutMs`
  from `Config.TimeoutMs` (10000 when it is 0) and a blank `UserAgent` from `Config.UserAgent`, also when you pass your own
  options (the caller's object is not changed).
- **BREAKING:** `PortfolioService.GetEquityPositionsAsync` returns `List<EquityPosition>?` and `GetDerivativePositionsAsync`
  returns `AllDerivativePosition?`: null when the account has no such positions (before: an empty list / an empty object), same
  as the balance and PPMMR calls. The Open/Closed derivative calls still return a list.
- **BREAKING:** `Redact.SafeUrl` output (used in log lines) now keeps the path: `scheme://host[:port]/path`; userinfo, query and
  fragment are still dropped.
- `StreamingMethod`, `StreamingChannel`, `DataTopic`, `Constants` and the `Topic` builder (now in namespace `SsiSdk`, was
  `SsiSdk.Internal`) are public.
- Const-string enums (`Board`, `Timeframe`, `OrderSide`, `OrderType`, `OrderStatus`, `AccountType`, `StreamingType`, `FCOType`,
  `FCOOperator`, `FCOStatus`, `DataType`, ...) gained `FromValue(string?)`: matches a value or member name in any case and
  returns the canonical value, or null (Python `from_value`).
- `FCOParams.ToDict()` gives the wire-name dictionary; null and `""` are omitted, `0` is kept.
- `TradingWsService.FcoOrderBookAsync` defaults to `pageIndex = 1`, `pageSize = 10`.
- `Account.AccountType` keeps `""` when the server sends `""` (null only when absent).
- `WebSocketException` accepts `code`, `statusCode`, `responseBody` (masked) and `headers`.
- The streaming handler-error log line carries only the exception type name, not its message.

### Added
- `Config`: `TradingApiDomain` / `TradingApiUrl` (trading and account REST; default `https://api.ssi.com.vn`),
  `TradingWsDomain` / `TradingWsUrl` (default `wss://api.ssi.com.vn/ws/v3/trading`), `UserAgent`, `AutoReconnect`,
  `OtpPollIntervalMs`, `OtpPollMaxWaitMs`, `MaxBatchOrders` (default **20**). Blank or null domains fall back to the defaults.
- Exceptions: `SmartOtpPendingException`, `SmartOtpRejectedException`, `ReauthenticationRequiredException`,
  `DuplicateRequestException`; `RateLimitException` gained `StatusCode`, `RetryAfterMs`, `Limit`, `Remaining`, `Reset`;
  enum `ServerErrorCode` covers the auth/OTP/refresh/signature/trading/data codes the server documents. Any unusable
  refresh-token code (401101-401104, 401106-401108) forces re-authentication.
- `OtpResponse` and `RequestOtpTypedAsync()` (`TransactionId` exists only for Smart OTP).
- Market data: `IterOhlcAsync()` / `GetOhlcAllAsync()`; `securitiesSummary` and `masterdata` follow `pagesCount`;
  foreign-trade fields on `MarketIndexSummary`; `StockType` on `SecuritiesInfo`; optional `Ceiling`/`Floor`/`RefPrice` on
  `SecuritiesSummary`; `Timeframe.Allowed`.
- Trading: `PlaceBatchOrdersAsync()` / `CancelBatchOrdersAsync()` (`POST`/`DELETE /order/batch`, at most
  `Config.MaxBatchOrders`, signed over the whole body; `BatchPlaceOrderItem`, `BatchCancelOrderItem`,
  `BatchOrderResponse`, `BatchOrderResult`); `GetFcoStatusHistoryAsync()`; optional OTP `code` on every `PlaceFco*Async` and `CancelFcoAsync`; `OrderStatus` lists every status the server emits.
- The REST client follows the server's rate-limit headers: `X-RateLimit-Limit` caps the request rate (never above
  `Config.RateLimitPerSecond`; 0 stays unlimited), `X-RateLimit-Remaining: 0` rests one second, and a `429` on a `GET` is waited
  out (`Retry-After`, else 1 s doubling; at most 3 retries, never a wait over 30 s) before `RateLimitException` is thrown.
  Orders, `POST`, `PUT` and `DELETE` are never replayed. The last numbers are on `AuthClient.RateLimit`.
- Streaming: `UnsubscribeOrderStatusAsync` / `UnsubscribePortfolioAsync` / `UnsubscribeAllAsync`; typed handlers `OnMarketFlag`,
  `OnIndexSummary`, `OnOrder`, `OnOrderMatch`, `OnPortfolio` (alongside `OnData`/`OnTrading`, which still receive everything);
  `IsConnected`, `ReconnectCount`, `LastError` and `OnConnection` (`ConnectionEvent`); `ListSubscriptionAsync`,
  `SubscribeMarketAsync`, `SubscribeIndexTradeAsync`, `SubscribeIndexSummaryAsync`; `OrderMatchMessage`, `MarketDataMessage`,
  `IndexSummaryMessage`, `IndexTickMessage`; position fields on `PortfolioMessage`; clearer aliases (`SubscribeOrdersAsync`,
  `SubscribeIndexTicksAsync`, `SubscribeMasterDataAsync`, `SubscribeSessionFlagAsync`, `SubscribeTradesAsync`, ...).
- Streaming connections are supervised: reconnect with capped exponential backoff and jitter, a fresh token on every connect,
  a proactive reconnect before the token expires, the server's `{"code","msg"}` refusal frames honoured (401/403: refresh once;
  429: wait >= 30 s), automatic resubscribe, and the old socket always closed before a new one opens. Subscribe/unsubscribe
  requests are split into frames under 4000 bytes. Topics are validated before sending and upper-cased (accounts untouched).
- EXPERIMENTAL trading WebSocket in `SsiSdk.Experimental` (`TradingWs`, `TradingWsClient`, `TradingWsService`,
  `TradingWsOptions`, `TradingWsError`): signed order/FCO commands over `Config.TradingWsUrl`. Not used by `TradingClient`
  or `StreamClient`; the API may change in any release. Signature encoding defaults to `auto` (base64 first; a refused
  signature, 401006/401008/401013, means the command was not executed and is re-sent once as hex and remembered).
  The refusal frame sent after HTTP 101 throws `TradingWsError` from `ConnectAsync`; the command waits for `resetMs`
  when `remaining == 0` (at most 2 s); a keep-alive ping runs every 30 s. A browser-like `User-Agent` is sent (the gateway
  answers 403 to unknown ones); `Config.UserAgent` wins.
- Tests: a new xUnit project, `tests/SsiSdk.Tests` (redaction, topics, number/price parsing, rate limit, token expiry, batch
  validation, market-data helpers, stream dispatch/reconnect/resubscribe, trading WS protocol and client with a fake socket).
  The package does not include it.
- `ParseDate` also reads `dd/MM/yyyy HH:mm:ss` (FCO status history times).

### Changed
- The token-refresh request now carries the `Authorization` header like every other request (Python does the same; it used to be omitted).
- A response whose JSON body is a bare array is returned as that array, not rewrapped as `{"data": [...]}` (matches Python).
  `GetAccountInfoAsync` reads the bare array (or the `{"code": 204}` marker); an enveloped `{"data": [...]}` is now an `ApiException`.
- **BREAKING** Money, prices and quantities in every model, request and streaming message are `decimal` (were `double`).
- **BREAKING** `AuthenticateAsync(otp, transactionId)` makes one call to the token endpoint and no longer polls; a Smart OTP
  that is not approved yet throws `SmartOtpPendingException` (use `EnsureAuthenticatedAsync(transactionId: ...)`, which polls using `OtpPollIntervalMs`/`OtpPollMaxWaitMs`).
- **BREAKING** `clientRequestId` moved to the end of the parameter list of `PlaceOrderAsync` and the shortcut order methods and is
  optional (generated when null).
- **BREAKING** Only `GET` is retried on timeout. `POST`/`PUT`/`DELETE` (auth, OTP, orders, FCO) are sent exactly once.
- **BREAKING** `ApiException.Code` is the server's error code when the body has one (the HTTP status stays in `StatusCode`).
  HTTP 200 carrying a `code` other than 200/204 now throws `ApiException`; the empty-result marker `{"code": 204}` yields empty
  results instead of parse errors.
- **BREAKING** `EnsureAuthenticatedAsync` treats a token with unknown expiry (`ExpiresAt <= 0`) as expired, expires tokens 30 s
  early, and throws `ReauthenticationRequiredException` when the session cannot be refreshed (401101/401103 clear the stored
  token). Concurrent refreshes make one server call.
- **BREAKING** `Account.AccountType` is null when the server omits it (no default `Cash`) and the raw string for an unknown type.
- **BREAKING** `Order.OsQuantity` is null (the server never sends it); `FilledQuantity` and `CancelQuantity` read `filledQty` /
  `cancelQty`.
- **BREAKING** `SecuritiesInfo.LotSize` and `ListedShares` are nullable; the `availableCash` / `withdrawal` fallbacks are gone
  (balances read `advancedCashT0/T1`, `withdrawableSSI/VSDC`).
- **BREAKING** `FcoPrice` and the order price take `decimal?`; the market words (ATO/ATC/MTL/MP) go out as price `"0"`.
  Prices are plain decimal strings (never an exponent); `quantity` must be a positive integer; a missing price on a limit
  order is rejected instead of being sent as text.
- **BREAKING** FCO list methods take `int?` paging (null: the server's default) and a `from`/`to` date pair; `GetFcoByStatusAsync`
  has a single-status overload and a collection overload. Placing needs dates `yyyy/MM/dd HH:mm:ss` with `from <= to` and a window of
  at most 31 days.
- **BREAKING** `Config.DeviceId` is removed: every order/FCO sends this machine's id read from the OS (raw machine id: Windows
  `MachineGuid`, Linux machine-id, macOS `IOPlatformUUID`, else the MAC address / host name). A missing id can no longer fail an order.
- **BREAKING** `Config.MaxBatchOrders` defaults to 20 (the server's default; it was hard-coded to 50).
- **BREAKING** Streaming: callbacks receive typed messages; `OrderStatusMessage.Price` is `decimal?` (text for ATO/ATC/MP) and
  `ModifyTime` is now `ModifiedTime`; interval topics accept only `tick`/`1m`/`5m`; `trade.index.<code>` ticks are
  `IndexTickMessage`; large values are `long`; `onResponse` callbacks receive acks and no longer replace `OnData`/`OnTrading`/
  `OnHeartbeat`; topic types are matched without regard to case (the server sends `market.FLAG`).
- **BREAKING** Positions send `querySummary=true` explicitly and parse the derivative object; `maxBuySell` reads `purchasingPower`.
- **BREAKING** `GetOhlc1WeekHistoricalAsync` / `GetOhlc1MonthHistoricalAsync` throw `ValidationException` (the server rejects
  those timeframes, 400210). `DownloadOhlc1MinuteAsync` / `DownloadOhlc1DayAsync` throw `NotImplementedException` (no
  `data/file` endpoint yet) and `DownloadData` is removed.
- Validation: `RequireNonEmpty` treats `0`/`false` as present (it still rejects null, blank strings and empty collections).
- Numeric parsing accepts `"1234.0"` and `"2,493,089"` and rejects inf/nan.
- OHLC: a date-only `from`/`to` is completed to `00:00:00` / `23:59:59` (the server answers 400213 to a bare date); minute/hour
  OHLC with a `to` after today is cut to the end of today; `page`/`size` < 1 are refused locally.
- FCO: `fco/cancel` sends `deviceId`; operators given as `0..4` in responses are parsed; response models tolerate unknown
  enum values; `place_fco_stop` operators accept the string form.

### Deprecated
- `MaxBuySellResponse.PurchasePower` -> `PurchasingPower`;
  `DerivativeAccountBalance.CashWithdrawableSSI/VSDC` -> `WithdrawableSSI/VSDC`;
  `OrderStatusMessage.ModifyTime` -> `ModifiedTime`.

### Fixed
- `EquityPosition` now carries `T1BuyQuantity` / `T2BuyQuantity` (the server's `t1BuyQuantity` / `t2BuyQuantity`, added to REST and
  trading-WS `position` next to `t1SellQuantity` / `t2SellQuantity`); they were silently dropped.
- `SubscribeBoardAsync`/`SubscribeIndexAsync` (and their `Unsubscribe*` and the `*ExchangeTrades`/`*IndexConstituents` aliases) now
  send `trade.<code>@*`, `quote.<code>@*` and `room.<code>@*`, one request per type. By the server's routing
  (`ConnectionManager.IsBasketIndexPattern`/`IsExchangePattern`) a basket index or exchange is only fanned out to its symbols
  with the `@*` interval; the bare `trade.VN30`/`trade.HOSE` was acked "ok" but never matched a tick. The topic builder accepts
  `@*` (`Topic.StreamIntervalAll`) for `trade`/`quote`/`room`/`put`/`oddlot` with one concrete code. Based on reading the server
  code (its public docs still show the bare form); not yet confirmed against a live server.
- A comma list in one topic (`VNM,FPT`) is rejected with `ValidationException`: the server never splits on commas, so it was
  acked "ok" and matched nothing. Pass a list of filters instead (one topic each).
- `Sign` raised raw XML/base64 errors for a missing or malformed `PrivateKey`; it now throws `ValidationException` (fail closed,
  no key material in the message).
- A server refusal of a subscription (`Denied (account)`, `Denied (scope)`, `Invalid`) is logged as a warning (category only,
  never the topic or account); a failing user handler is logged with a masked message.
- A WebSocket frame that is not a JSON object no longer crashes the receive loop; the `PING_PONG` ack no longer raises in the
  heartbeat parser.
- The keep-alive loop stops with `Disconnect()`.
- A negative `MaxRetries` no longer breaks the retry loop.
- Malformed token responses are no longer marked with HTTP 202 (which made Smart OTP polling treat them as "pending").
- FCO responses also read the spec's casing (`price_operator`, `Username`, `IsPlaceOrder`, `matchedQuality`, `uniquedId`) as a fallback.

### Not ported from Python 3.3.0 (no .NET counterpart, or by design)
- Python's `TypeError` on an unknown `Config` option: C# object initialisers fail at compile time instead.
- Sync clients and `typing` helpers (`PriceLike`, `NumberLike`, `TypedDict` items, `mypy`): C# has the single async API and real types.
- `MarketIndexes.board_raw` and `DownloadData`/`DownloadDataRequest` (deprecated in Python).
- Awaitable stream callbacks: .NET handlers are plain `Action<T>`.
- `request_stream` / `stream` / `next_response` of the trading WebSocket client (multi-frame replies): only the first-frame `RequestAsync`.
- The Python integration suite (`tests/integration`) and `tools/gen_api_methods.py`; `API_METHODS.md` is maintained by hand.

### Not implemented (by design, pending decisions)
- Bulk OHLC download (`DownloadOhlc*`: the server has no `data/file` endpoint yet).
- `fcoEvent` streaming.
