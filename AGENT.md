# AGENT.md — AI Agent Integration Guide for ssi-sdk-dotnet

This guide provides AI coding assistants (Claude, Gemini, Cursor, Copilot, etc.) with instructions, code patterns, architectural conventions, and an API cheatsheet for integrating and interacting with the `ssi-sdk-dotnet` (v3.3.0) package. The full method list is in [API_METHODS.md](API_METHODS.md); release notes and breaking changes are in [CHANGELOG.md](CHANGELOG.md).

---

## 1. Overview & Architecture

`ssi-sdk-dotnet` (`SSIDeveloper.FastConnect.Sdk`) is a strongly-typed .NET SDK for SSI's **FastConnect v3 API**. It provides modern C# support (.NET 8+) for:
- Authentication & Token Management (OTP, Refresh Token, Auto-refresh)
- Market Data (OHLC 1min-1day, Indexes, Securities Information & Summary)
- Trading & Portfolio (Order placement/modification/cancellation, batch orders, FCO conditional orders, Account balances, Positions, PPMMR)
- Realtime Streaming (WebSocket market data & trading events, supervised with auto-reconnect and resubscribe)
- Trading WebSocket (`SsiSdk.TradingWs`, order/FCO commands and queries over `/ws/v3/trading`)

### Layering & Structure
```
Clients (AuthClient, DataClient, TradingClient, StreamClient)
   └── Services (TokenManager, MarketDataService, AccountService, PortfolioService, TradingService, StreamingService)
        └── Transport (RestClient / WebSocketClient)
             └── Leaf Modules (Models, Enums, Internal)
```

### Key Architectural Constraints for AI Agents
1. **Modular Clients**: `new AuthClient(config)`, then sub-clients `new DataClient(auth)`, `new TradingClient(auth)`, `new StreamClient(auth)`. Services hang off them: `data.MarketData`, `trading.Account`, `trading.Portfolio`, `trading.Trading`, `stream.Streaming`.
2. **Type Safety with Enums**: All protocol constants (`OrderSide`, `OrderType`, `OrderStatus`, `Board`, `FCOType`, `FCOOperator`, `FCOStatus`, `Timeframe`) are defined as static constants in `SsiSdk.Enums`.
3. **Header User-Agent Parity**: Requests to SSI API include a browser `User-Agent` header by default (`DefaultUserAgent`) to bypass Cloudflare firewall checks.

---

## 2. Authentication & Setup Pattern

### Configuration Initialization
```csharp
using SsiSdk;

var config = new Config
{
    ClientId = "YOUR_CLIENT_ID",
    ApiKey = "YOUR_API_KEY",
    ApiSecret = "YOUR_API_SECRET",
    PrivateKey = "YOUR_PRIVATE_KEY", // Base64 RSA Private Key
};
```

New `Config` options in 3.3.0: `TradingApiDomain`/`TradingApiUrl`, `TradingWsDomain`/`TradingWsUrl`, `UserAgent`, `AutoReconnect`,
`OtpPollIntervalMs`, `OtpPollMaxWaitMs`, `MaxBatchOrders` (default 20). `Config.DeviceId` is gone: the machine id is read from the OS.

All public durations are integer milliseconds with an `Ms` suffix: `TimeoutMs`, `RetryDelayMs`, `OtpPollIntervalMs`, `OtpPollMaxWaitMs`, `RateLimitException.RetryAfterMs`, `WaitAsync(timeoutMs)`, `PingAsync(intervalMs)`, `TradingWsOptions.*Ms`. There is no `TimeSpan` in the public API.

Read secrets (`ApiSecret`, `PrivateKey`) from a vault or environment, never from source. The SDK masks them in logs and exception text.

### Authentication Flow
```csharp
using SsiSdk;

var auth = new AuthClient(config);

// Authenticate with OTP (Required for Trading & Streaming)
// One call, no polling: a Smart OTP that is not approved yet throws SmartOtpPendingException (EnsureAuthenticatedAsync(transactionId: ...) polls).
var token = await auth.AuthenticateAsync("123456");

// Create sub-clients sharing the auth context
var data = new DataClient(auth);
var trading = new TradingClient(auth);
var stream = new StreamClient(auth);
```

---

## 3. Public API Cheatsheet for AI Agents

### 3.1 Market Data (`data.MarketData`)

| Method | Parameters | Return Type | Description |
|--------|------------|-------------|-------------|
| `GetOhlc1MinuteAsync` (also `3Minute`, `5Minute`, `15Minute`, `1Hour`) | `symbol` | `Task<List<OhlcData>>` | Today's candles |
| `GetOhlc1MinuteHistoricalAsync` (also `3Minute`..`1Hour`, `1DayHistoricalAsync`) | `symbol, fromDate, toDate, page, size` | `Task<List<OhlcData>>` | Candles for a range; a date-only bound is completed to `00:00:00` / `23:59:59` |
| `IterOhlcAsync` / `GetOhlcAllAsync` | same as historical | `IAsyncEnumerable<OhlcData>` / `Task<List<OhlcData>>` | Walk every page |
| `GetOhlc1WeekHistoricalAsync` / `GetOhlc1MonthHistoricalAsync` | - | - | Throw `ValidationException`: the server rejects these timeframes |
| `DownloadOhlc1MinuteAsync` / `DownloadOhlc1DayAsync` | - | - | Throw `NotImplementedException`: no bulk download endpoint yet. Do not call them |
| `GetIndexesAsync` / `GetIndexesByBoardAsync` | `board` | `Task<List<MarketIndexes>>` | Market indexes |
| `GetIndexSummaryAsync` | `index` | `Task<MarketIndexSummary?>` | Summary metrics for an index (`...HistoricalAsync` takes a trading date) |
| `GetSecuritiesInfoAsync` / `...ByIndexAsync` / `...ByBoardAsync` | `symbol` / `index` / `board` | `Task<SecuritiesInfo?>` / `Task<List<SecuritiesInfo>>` | Instrument info (`LotSize`/`ListedShares` may be null) |
| `GetSecuritiesSummaryAsync` | `symbol` | `Task<List<SecuritiesSummary>>` | Summary of stock transactions (`...HistoricalAsync(symbol, from, to)`) |
| `GetMasterDataAsync` | None | `Task<List<MasterData>>` | Ceiling, floor, ref prices for today |
| `GetMasterDataHistoricalAsync` | `fromDate, toDate` | `Task<List<MasterData>>` | Same, for a date range |
| `GetCorporateActionsAsync` | `symbol = null, language = "vn"` | `Task<List<CorporateAction>>` | Today's corporate actions (all pages; all-string fields) |
| `GetCorporateActionsHistoricalAsync` | `fromDate, toDate, symbol = null, language = "vn"` | `Task<List<CorporateAction>>` | Same, for a date range of at most 5 days |

All prices, volumes and values in models are `decimal`, not `double`.

### 3.2 Account & Portfolio (`trading.Account` & `trading.Portfolio`)

| Service | Method | Return Type | Description |
|---------|--------|-------------|-------------|
| `Account` | `GetAccountInfoAsync()` | `Task<List<Account>>` | Query list of sub-accounts (`AccountType` is null when the server omits it) |
| `Portfolio` | `GetEquityBalanceAsync(accountNo)` | `Task<EquityAccountBalance>` | Cash balance & debt for cash/margin account |
| `Portfolio` | `GetDerivativeBalanceAsync(accountNo)` | `Task<DerivativeAccountBalance>` | Balance & margin for derivative account |
| `Portfolio` | `GetEquityPositionsAsync(accountNo)` | `Task<List<EquityPosition>?>` | Equity stock holdings (null when none) |
| `Portfolio` | `GetDerivativePositionsAsync(accountNo)` | `Task<AllDerivativePosition?>` | Derivative contract positions (null when none) |
| `Portfolio` | `GetTodayOrdersAsync(accountNo)` | `Task<List<Order>>` | Intraday orders |
| `Portfolio` | `GetHistoricalOrdersAsync(accountNo, fromDate, toDate)` | `Task<List<Order>>` | Historical order book entries |
| `Portfolio` | `GetEquityPpmmrAsync(accountNo)` | `Task<EquityPPMMR>` | Purchasing power & margin ratio (Equity) |
| `Portfolio` | `GetDerivativePpmmrAsync(accountNo)` | `Task<DerivativePPMMR>` | Purchasing power & margin ratio (Derivative) |

### 3.3 Standard Trading (`trading.Trading`)

| Method | Parameters | Return Type | Description |
|--------|------------|-------------|-------------|
| `PlaceLimitOrderAsync` | `accountNo, symbol, side, quantity, price` | `Task<PlaceOrderResponse>` | Place LO order |
| `PlaceMarketOrderAsync` | `accountNo, symbol, side, quantity` | `Task<PlaceOrderResponse>` | Place MTL market order |
| `PlaceAtoOrderAsync` | `accountNo, symbol, side, quantity` | `Task<PlaceOrderResponse>` | Place ATO opening order |
| `PlaceAtcOrderAsync` | `accountNo, symbol, side, quantity` | `Task<PlaceOrderResponse>` | Place ATC closing order |
| `PlaceOrderAsync` | `accountNo, symbol, side, quantity, price (decimal?), orderType, clientRequestId?` | `Task<PlaceOrderResponse>` | Place order with custom OrderType. `clientRequestId` is the idempotency key (generated when null); reuse it when retrying |
| `PlaceBatchOrdersAsync` | `IReadOnlyCollection<BatchPlaceOrderItem>` | `Task<BatchOrderResponse>` | Place many orders in one signed call (max `Config.MaxBatchOrders`, default 20) |
| `CancelBatchOrdersAsync` | `IReadOnlyCollection<BatchCancelOrderItem>` | `Task<BatchOrderResponse>` | Cancel many orders in one call |
| `ModifyOrderPriceByOrderIdAsync` | `accountNo, orderId, price` | `Task<ModifyOrderResponse>` | Modify order price by server order ID |
| `ModifyOrderQuantityByOrderIdAsync` | `accountNo, orderId, quantity` | `Task<ModifyOrderResponse>` | Modify order quantity by server order ID |
| `CancelOrderByOrderIdAsync` | `accountNo, orderId` | `Task<CancelOrderResponse>` | Cancel order by server order ID |
| `GetMaxBuySellAsync` | `accountNo, symbol, price` | `Task<MaxBuySellResponse>` | Max buy/sell qty at given price (`PurchasingPower`) |

### 3.4 Flexible Conditional Orders - FCO (`trading.Trading`)

| Method | Key Parameters | Return Type | Description |
|--------|----------------|-------------|-------------|
| `PlaceFcoGtdAsync` | `accountNo, symbol, side, quantity, price, priceSlip, fromDate, toDate` | `Task<FCOPlaceResponse>` | Good Till Date order |
| `PlaceFcoStopAsync` | `accountNo, symbol, side, quantity, stopPrice, operator, fromDate, toDate` | `Task<FCOPlaceResponse>` | Stop Market order |
| `PlaceFcoStopLimitAsync` | `accountNo, symbol, side, quantity, price, priceSlip, stopPrice, operator, fromDate, toDate` | `Task<FCOPlaceResponse>` | Stop Limit order |
| `PlaceFcoTrailingStopAsync` | `accountNo, symbol, side, quantity, activePrice, trailingAmount, fromDate, toDate` | `Task<FCOPlaceResponse>` | Trailing Stop Market |
| `PlaceFcoTrailingStopLimitAsync` | `accountNo, symbol, side, quantity, activePrice, trailingAmount, priceSlip, fromDate, toDate` | `Task<FCOPlaceResponse>` | Trailing Stop Limit |
| `PlaceFcoOcoAsync` | `accountNo, symbol, side, quantity, tpActivePrice, slActivePrice, tpPrice, slPrice, tpSlip, slSlip, fromDate, toDate` | `Task<FCOPlaceResponse>` | One-Cancels-the-Other |
| `PlaceFcoBullBearAsync` | `accountNo, symbol, side, quantity, price, priceSlip, tpActivePrice, slActivePrice, tpPrice, slPrice, tpSlip, slSlip, fromDate, toDate` | `Task<FCOPlaceResponse>` | Bull Bear order |
| `CancelFcoAsync` | `fcoId` | `Task<FCOCancelResponse>` | Cancel FCO by ID |
| `GetFcoByAccountNoAsync` | `accountNo, pageIndex?, pageSize?` (`int?`, null = server default) | `Task<FCOListResponse>` | List account's FCO orders |
| `GetFcoBySymbolAsync` | `accountNo, symbol, pageIndex, pageSize` | `Task<FCOListResponse>` | Filter FCO orders by symbol |
| `GetFcoByStatusAsync` | `accountNo, processStatus, pageIndex, pageSize` | `Task<FCOListResponse>` | Filter FCO by status (`TRIT`, `WAIT`, etc.) |
| `GetFcoByDateAsync` | `accountNo, fromDate, toDate, pageIndex, pageSize` | `Task<FCOListResponse>` | Filter FCO by date range |
| `GetFcoByIdAsync` | `accountNo, fcoId` | `Task<FCOInfo?>` | Single FCO order details |
| `GetFcoOrderBookAsync` | `fcoId, pageIndex, pageSize` | `Task<FCOOrderBookResponse>` | Execution logs of FCO |
| `GetFcoStatusHistoryAsync` | `fcoId` | `Task<List<FCOStatusHistoryItem>>` | Status history of an FCO |

FCO prices are `FcoPrice`: pass a `decimal` or a market word (`"MTL"`, `"ATO"`, `"ATC"`, `"MP"`). Every `PlaceFco*Async` and `CancelFcoAsync` takes an optional OTP `code`.

---

## 4. Code Examples for Common AI Tasks

### Example 1: Placing & Instantly Cancelling a GTD FCO Order
```csharp
using SsiSdk;

var auth = new AuthClient(config);
await auth.AuthenticateAsync("123456");
var trading = new TradingClient(auth);

var accountNo = "1234561";
var fromDate = DateTime.Now.ToString("yyyy/MM/dd 00:00:00");
var toDate = DateTime.Now.AddDays(7).ToString("yyyy/MM/dd 23:00:00");

// 1. Place GTD order
var gtdRes = await trading.Trading.PlaceFcoGtdAsync(
    accountNo, "SSI", OrderSide.Buy, 100, "MTL", 0.5m, fromDate, toDate
);
Console.WriteLine($"Placed FCO ID: {gtdRes.FCOID}");

// 2. Cancel FCO order
var cancelRes = await trading.Trading.CancelFcoAsync(gtdRes.FCOID);
Console.WriteLine($"Cancelled FCO ID: {cancelRes.FCOID}");
```

### Example 2: Realtime Streaming Callbacks
```csharp
using SsiSdk;
using SsiSdk.Models;

var stream = new StreamClient(auth);

stream.Streaming.OnData = msg =>
{
    if (msg is TradeMessage t) Console.WriteLine($"[Trade] {t.Symbol} @ {t.Price}");
};

stream.Streaming.OnTrading = msg =>
{
    if (msg is FCOOrderUpdateMessage fco) Console.WriteLine($"[FCO] {fco.FCOID} Status: {fco.ProcessStatus}");
};

await stream.Streaming.ConnectAsync();
await stream.Streaming.SubscribeSymbolTradeAsync(new[] { "SSI" });
await stream.Streaming.SubscribeOrderStatusAsync("1234561");
await stream.Streaming.WaitAsync();
```

The stream reconnects and resubscribes by itself. Use `OnConnection`, `IsConnected` and `ReconnectCount` to observe it. Typed handlers
(`OnMarketFlag`, `OnIndexSummary`, `OnOrder`, `OnOrderMatch`, `OnPortfolio`) exist next to `OnData`/`OnTrading`. Handlers are plain `Action<T>`.
Interval topics accept only `tick`, `1m`, `5m`.

### Trading WebSocket
`SsiSdk.TradingWs.ConnectAsync(auth)` returns a `TradingWsService` for signed order/FCO commands and queries over
`Config.TradingWsUrl`. It needs `Config.PrivateKey` for commands that change state.
Prefer `TradingClient` (REST) unless the user asks for the WebSocket.

---

## 5. Rules & Guidelines for AI Code Generators

1. **Date Format for FCO**: FCO date arguments (`fromDate`, `toDate`) **must** be formatted as `"YYYY/MM/DD HH:MM:SS"`.
2. **Never hardcode secrets**: Access tokens, private keys, API secrets, and OTPs should never be committed to git.
3. **Use Enum constants**: Pass Enum constants (`OrderSide.Buy`, `OrderType.LO`, `FCOOperator.GreaterOrEqual`) instead of raw strings.
4. **Clean Builds**: Maintain C# strict typing and run `dotnet build src/SsiSdk/SsiSdk.csproj` to verify changes, and `dotnet test tests/SsiSdk.Tests/SsiSdk.Tests.csproj` for the unit tests.
5. **Money is `decimal`**: never use `double`/`float` for prices, quantities or balances; write literals as `0.5m`.
6. **Idempotency**: keep and reuse `clientRequestId` when retrying an order. The SDK itself only retries `GET`; it never replays an order.
7. **Never log secrets**: do not log tokens, OTP, `ApiSecret`, `PrivateKey`, signatures, request bodies or account numbers. The SDK masks them, your code must too.
8. **Do not call `DownloadOhlc1*`** (throws `NotImplementedException`) and do not request 1w/1M candles (throws `ValidationException`).
