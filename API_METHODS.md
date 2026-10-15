# API methods - ssi-sdk-dotnet 3.3.0

Every public method of the package `SSIDeveloper.FastConnect.Sdk` (namespace `SsiSdk`). All I/O methods are
`async` and take an optional trailing `CancellationToken ct` (not repeated below). There is one async API; the
Python sync/async pairs collapse into it. Money and prices are `decimal`.

Clients: `new AuthClient(config)`, then `new DataClient(auth)`, `new TradingClient(auth)`, `new StreamClient(auth)`.

## AuthClient / TokenManager

| Member | Description |
|--------|-------------|
| `AuthenticateAsync(otp?, transactionId?)` | One call to the token endpoint. Smart OTP not yet approved throws `SmartOtpPendingException`; this method never polls (use `EnsureAuthenticatedAsync(transactionId: ...)` to poll) |
| `RefreshAsync()` | Refresh with the refresh token (concurrent callers share one server call) |
| `EnsureAuthenticatedAsync(...)` | A valid access token: current, refresh, OTP login, or Smart OTP polling (`pollIntervalMs`, `pollMaxRetries`, default `Config.OtpPollIntervalMs`/`OtpPollMaxWaitMs`); `ReauthenticationRequiredException` when the session is gone |
| `RequestOtpAsync()` / `RequestOtpTypedAsync()` | Ask for an OTP; the typed form returns `OtpResponse` (`TransactionId` only for Smart OTP) |
| `SetToken(token)` | Use a token you already hold |
| `Token`, `AccessToken`, `IsTokenExpired`, `HasRefreshToken`, `CanRefresh`, `IsRefreshTokenExpired` | State. Unknown expiry counts as expired; tokens expire 30 s early |
| `RateLimit` | Last `X-RateLimit-*` numbers seen by the REST client |

## DataClient.MarketData (`MarketDataService`)

| Method | Description |
|--------|-------------|
| `GetOhlc1MinuteAsync` / `3Minute` / `5Minute` / `15Minute` / `1Hour` (`symbol`) | Today's candles |
| `GetOhlc1MinuteHistoricalAsync` / `3Minute..` / `5Minute..` / `15Minute..` / `1Hour..` / `1DayHistoricalAsync` (`symbol, fromDate, toDate, page, size`) | Candles for a range; a date-only bound becomes `00:00:00` / `23:59:59`; an intraday `to` in the future is cut to today |
| `GetOhlc1WeekHistoricalAsync` / `GetOhlc1MonthHistoricalAsync` | Throw `ValidationException`: the server rejects these timeframes (400210) |
| `IterOhlcAsync(...)` | `IAsyncEnumerable<OhlcData>` over every page |
| `GetOhlcAllAsync(...)` | All pages into one list |
| `DownloadOhlc1MinuteAsync` / `DownloadOhlc1DayAsync` | Throw `NotImplementedException`: no `data/file` endpoint on the server yet |
| `GetIndexesAsync()` / `GetIndexesByBoardAsync(board)` | Market indexes |
| `GetIndexSummaryAsync(index)` / `GetIndexSummaryHistoricalAsync(index, tradingDate)` | Index summary |
| `GetBoardSummaryAsync(board)` / `GetBoardSummaryHistoricalAsync(board, tradingDate)` | Board summary |
| `GetSecuritiesInfoAsync(symbol)` / `GetSecuritiesInfoByIndexAsync(index)` / `GetSecuritiesInfoByBoardAsync(board)` | Instrument info (`StockType`, nullable `LotSize`/`ListedShares`) |
| `GetSecuritiesSummaryAsync(symbol)` / `GetSecuritiesSummaryHistoricalAsync(symbol, from, to)` | Securities summary (follows `pagesCount`) |
| `GetSecuritiesSummaryByIndexAsync(index)` / `GetSecuritiesSummaryByIndexHistoricalAsync(index, from, to)` | Summary of an index's constituents |
| `GetMasterDataAsync()` / `GetMasterDataHistoricalAsync(from, to)` | Ceiling / floor / reference prices (follows `pagesCount`) |

## TradingClient.Account (`AccountService`)

| Method | Description |
|--------|-------------|
| `GetAccountInfoAsync()` | Accounts of the user; `AccountType` is null when the server omits it |

## TradingClient.Portfolio (`PortfolioService`)

| Method | Description |
|--------|-------------|
| `GetEquityBalanceAsync(accountNo)` / `GetDerivativeBalanceAsync(accountNo)` | Balances (`WithdrawableSSI/VSDC`; `CashWithdrawable*` are `[Obsolete]` aliases) |
| `GetEquityPositionsAsync(accountNo)` | Positions (sends `querySummary=true`); `List<EquityPosition>?`, null when there are none |
| `GetOpenDerivativePositionsAsync` / `GetClosedDerivativePositionsAsync` (`accountNo`) | Derivative positions (empty list when none) |
| `GetDerivativePositionsAsync(accountNo)` | Open and closed; `AllDerivativePosition?`, null when there are none |
| `GetTodayOrdersAsync(accountNo)` / `GetHistoricalOrdersAsync(accountNo, from, to)` | Order book (`OsQuantity` is null: the server never sends it) |
| `GetEquityPpmmrAsync(accountNo)` / `GetDerivativePpmmrAsync(accountNo)` | PPMMR |

## TradingClient.Trading (`TradingService`)

| Method | Description |
|--------|-------------|
| `PlaceOrderAsync(accountNo, symbol, side, quantity, price?, orderType, clientRequestId?)` | Place an order. `clientRequestId` is the idempotency key, generated when null |
| `PlaceLimitOrderAsync` / `PlaceMarketOrderAsync` / `PlaceAtoOrderAsync` / `PlaceAtcOrderAsync` | Shortcuts; market-priced types send price `"0"` |
| `ModifyOrderPriceAsync` / `ModifyOrderPriceByOrderIdAsync` / `ModifyOrderQuantityAsync` / `ModifyOrderQuantityByOrderIdAsync` | Amend |
| `CancelOrderAsync(accountNo, clientRequestId)` / `CancelOrderByOrderIdAsync(accountNo, orderId)` | Cancel |
| `PlaceBatchOrdersAsync(IReadOnlyCollection<BatchPlaceOrderItem>)` | `POST /order/batch`, at most `Config.MaxBatchOrders` (default 20), signed over the whole body; one bad order rejects the batch before sending |
| `CancelBatchOrdersAsync(IReadOnlyCollection<BatchCancelOrderItem>)` | `DELETE /order/batch` |
| `GetMaxBuySellAsync(accountNo, symbol, price)` / `GetMaxBuySellAtMarketPriceAsync(accountNo, symbol)` | Max buy/sell (`PurchasingPower`) |
| `PlaceFcoGtdAsync` / `PlaceFcoStopAsync` / `PlaceFcoStopLimitAsync` / `PlaceFcoTrailingStopAsync` / `PlaceFcoTrailingStopLimitAsync` / `PlaceFcoOcoAsync` / `PlaceFcoBullBearAsync` | FCO placement; prices are `FcoPrice` (a `decimal` or a market word such as `"MTL"`); optional OTP `code`; dates `yyyy/MM/dd HH:mm:ss`, `from <= to`, window <= 31 days |
| `CancelFcoAsync(fcoId, code?)` | Cancel an FCO |
| `GetFcoByAccountNoAsync` / `GetFcoBySymbolAsync` / `GetFcoByStatusAsync` / `GetFcoByDateAsync` | FCO lists (`int?` paging; the server's own default applies when null) |
| `GetFcoByIdAsync(accountNo, fcoId)` | One FCO |
| `GetFcoOrderBookAsync(fcoId, pageIndex, pageSize)` | Orders spawned by an FCO |
| `GetFcoStatusHistoryAsync(fcoId)` | Status history of an FCO |

## StreamClient / StreamingService

Handlers are plain `Action<T>` and run on the receive thread: keep them short. Frames the SDK cannot read are dropped.

| Member | Description |
|--------|-------------|
| `ConnectAsync()` / `DisconnectAsync()` / `Disconnect()` / `WaitAsync(timeoutMs?)` | Lifecycle. The connection is supervised: capped exponential backoff with jitter, fresh token per connect, proactive reconnect before token expiry, 401/403 refresh once, 429 wait >= 30 s, automatic resubscribe |
| `IsConnected`, `ReconnectCount`, `LastError` | State |
| `OnData`, `OnTrading`, `OnHeartbeat` | Catch-all handlers (still receive everything) |
| `OnMarketFlag`, `OnIndexSummary`, `OnOrder`, `OnOrderMatch`, `OnPortfolio` | Typed handlers |
| `OnConnection` (`ConnectionEvent`) | connected / reconnected / disconnected / failed |
| `PingAsync(intervalMs?)` / `StopPingLoop()` | Keep-alive |
| `SubscribeSymbolTradeAsync` / `Quote` / `Room` / `PutThrough` / `OddLot` / `Ohlcv(interval)` | Per-symbol topics (aliases: `SubscribeTradesAsync`, `SubscribeQuotesAsync`, `SubscribeForeignRoomAsync`, `SubscribePutThroughAsync`, `SubscribeOddLotAsync`, `SubscribeCandlesAsync`) |
| `SubscribeMarketAsync`, `SubscribeMarketFlagAsync`, `SubscribeIndexTradeAsync(indices, interval?)`, `SubscribeIndexSummaryAsync` | Market / index topics (aliases: `SubscribeMasterDataAsync`, `SubscribeSessionFlagAsync`, `SubscribeIndexTicksAsync`) |
| `SubscribeSymbolAsync`, `SubscribeBoardAsync`, `SubscribeIndexAsync`, `SubscribeExchangeTradesAsync`, `SubscribeIndexConstituentsAsync` | Combined / board / index-constituent topics; board and index send `trade.<code>@*`, `quote.<code>@*`, `room.<code>@*` (one request per type, unsubscribe uses the same topics) |
| `SubscribeOrderStatusAsync(accountNo)` / `SubscribeFcoOrderStatusAsync` / `SubscribePortfolioAsync` | Account topics (aliases: `SubscribeOrdersAsync`, `SubscribePositionsAsync`) |
| `Unsubscribe...Async` for every subscribe above | Mirror of each subscribe |
| `ListSubscriptionAsync(timeoutMs = 5000)` | Topics the server holds for this connection |
| `UnsubscribeAllAsync()` | Drops every subscription and clears the resubscribe book |

Interval topics accept only `tick`, `1m`, `5m`; `trade.index` with an interval needs one concrete index.
The interval `*` (`Topic.StreamIntervalAll`) is for `trade`/`quote`/`room`/`put`/`oddlot` with one concrete index or exchange code
(`trade.VN30@*`, `quote.HOSE@*`). A topic filter is one of `*`, a code or a range `A-B`; a comma list (`VNM,FPT`) is rejected,
pass several filters instead.
Subscribe requests are split into frames under 4000 bytes.

## SsiSdk.Experimental (trading WebSocket) - EXPERIMENTAL

May change in any release. Needs `Config.PrivateKey` for commands that change state. Not used by `TradingClient` / `StreamClient`.

| Type / member | Description |
|---------------|-------------|
| `TradingWs.ConnectAsync(auth, options?)` | Connect and return a `TradingWsService` |
| `TradingWsOptions` | `RequestTimeoutMs` (`int?` ms, null takes `Config.TimeoutMs`), `SignatureEncoding` (`auto`/`base64`/`hex`), `HandshakeGraceMs`, `RespectRateLimit`, `MaxRateLimitWaitMs`, `PingIntervalMs`, `UserAgent` |
| `TradingWsClient` | `ConnectAsync`, `CloseAsync`, `RequestAsync(method, params)`, `SubscribeAsync`, `UnsubscribeAsync`, `PingAsync`, `OnEvent`, `RateLimit`, `Rejection`, `IsConnected`. No reconnect: create a new one after a drop |
| `TradingWsService` (also `Client`, `CloseAsync`, `DisposeAsync`) orders | `PlaceOrderAsync`, `AmendOrderAsync`, `CancelOrderAsync`, `BatchNewOrdersAsync`, `BatchCancelOrdersAsync`, `CommandAsync` |
| `TradingWsService` FCO | `PlaceFcoGtd/Stop/StopLimit/TrailingStop/TrailingStopLimit/Oco/BullBearAsync`, `CancelFcoAsync`, `ListFcoAsync` (one `processStatus`), `FcoOrderBookAsync`, `FcoStatusHistoryAsync` |
| `TradingWsService` events | `SubscribeOrderEventsAsync(accountNo)`, `SubscribePortfolioEventsAsync`, `Unsubscribe...`, `UnsubscribeAllEventsAsync` (one explicit account: no `*`, no `,`) |
| `TradingWsService` queries | `QueryPositionAsync`, `QueryMaxBuySellAsync`, `QueryAccountBalanceAsync`, `QueryPpmmrAccountAsync`, `QueryOrderBookAsync` |
| `SsiSdk.TradingWsError` (a `WebSocketException`) | Carries `Status`, `ErrorCode`, `ServerMessage`; never the signed params |

Not mapped from Python: `request_stream` / `stream` / `next_response` (multi-frame replies); only the first-frame `RequestAsync` exists.
