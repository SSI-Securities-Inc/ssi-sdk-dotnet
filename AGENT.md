# AGENT.md — AI Agent Integration Guide for ssi-sdk-dotnet

This guide provides AI coding assistants (Claude, Gemini, Cursor, Copilot, etc.) with instructions, code patterns, architectural conventions, and an API cheatsheet for integrating and interacting with the `ssi-sdk-dotnet` (v3.1.1) package.

---

## 1. Overview & Architecture

`ssi-sdk-dotnet` (`SSIDeveloper.FastConnect.Sdk`) is a strongly-typed .NET SDK for SSI's **FastConnect v3 API**. It provides modern C# support (.NET 8+) for:
- Authentication & Token Management (OTP, Refresh Token, Auto-refresh)
- Market Data (OHLC 1min/1day, Indexes, Securities Information & Summary)
- Trading & Portfolio (Order placement/modification/cancellation, FCO conditional orders, Account balances, Positions, PPMMR)
- Realtime Streaming (WebSocket market data & trading events)

### Layering & Structure
```
Facade Client (SsiClient - Auth / Data / Trading / Stream)
   └── Services (TokenManager, MarketDataService, AccountService, PortfolioService, TradingService, StreamingService)
        └── Transport (RestClient / WebSocketClient)
             └── Leaf Modules (Models, Enums, Internal)
```

### Key Architectural Constraints for AI Agents
1. **Modular Facade Pattern**: Created via `new SsiClient(config)` or sub-clients `new DataClient(auth)`, `new TradingClient(auth)`, `new StreamClient(auth)`.
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

### Authentication Flow
```csharp
using SsiSdk;

var auth = new AuthClient(config);

// Authenticate with OTP (Required for Trading & Streaming)
var token = await auth.AuthenticateAsync("123456");

// Create sub-clients sharing the auth context
var data = new DataClient(auth);
var trading = new TradingClient(auth);
var stream = new StreamClient(auth);
```

---

## 3. Public API Cheatsheet for AI Agents

### 3.1 Market Data (`data.Market`)

| Method | Parameters | Return Type | Description |
|--------|------------|-------------|-------------|
| `GetOhlc1MinuteAsync` | `symbol` | `Task<List<OhlcData>>` | Query 1-minute candle OHLCV data |
| `GetOhlc1DayAsync` | `symbol` | `Task<List<OhlcData>>` | Query 1-day candle OHLCV data |
| `DownloadOhlc1MinuteAsync` | `symbol` | `Task<List<OhlcData>>` | Download full 1-minute OHLC history |
| `DownloadOhlc1DayAsync` | `symbol` | `Task<List<OhlcData>>` | Download full 1-day OHLC history |
| `GetMarketIndexesAsync` | `indexId` | `Task<List<MarketIndex>>` | Get list of market indexes |
| `GetMarketIndexSummaryAsync` | `indexId, fromDate, toDate, page, size` | `Task<MarketIndexSummary>` | Summary metrics for an index |
| `GetSecuritiesInfoAsync` | `symbol, market, page, size` | `Task<SecuritiesInfo>` | Security details |
| `GetSecuritiesSummaryAsync` | `symbol, market, page, size` | `Task<SecuritiesSummary>` | Summary of stock transactions |

### 3.2 Account & Portfolio (`trading.Account` & `trading.Portfolio`)

| Service | Method | Return Type | Description |
|---------|--------|-------------|-------------|
| `Account` | `GetAccountInfoAsync()` | `Task<List<AccountInfo>>` | Query list of sub-accounts |
| `Portfolio` | `GetEquityBalanceAsync(accountNo)` | `Task<EquityAccountBalance>` | Cash balance & debt for cash/margin account |
| `Portfolio` | `GetDerivativeBalanceAsync(accountNo)` | `Task<DerivativeAccountBalance>` | Balance & margin for derivative account |
| `Portfolio` | `GetEquityPositionsAsync(accountNo)` | `Task<List<EquityPosition>>` | Equity stock holdings |
| `Portfolio` | `GetDerivativePositionsAsync(accountNo)` | `Task<AllDerivativePosition>` | Derivative contract positions |
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
| `PlaceOrderAsync` | `accountNo, symbol, side, quantity, price, orderType` | `Task<PlaceOrderResponse>` | Place order with custom OrderType |
| `ModifyOrderPriceByOrderIdAsync` | `accountNo, orderId, price` | `Task<ModifyOrderResponse>` | Modify order price by server order ID |
| `ModifyOrderQuantityByOrderIdAsync` | `accountNo, orderId, quantity` | `Task<ModifyOrderResponse>` | Modify order quantity by server order ID |
| `CancelOrderByOrderIdAsync` | `accountNo, orderId` | `Task<CancelOrderResponse>` | Cancel order by server order ID |
| `GetMaxBuySellAsync` | `accountNo, symbol, price` | `Task<MaxBuySellResponse>` | Max buy/sell qty at given price |

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
| `GetFcoByAccountNoAsync` | `accountNo, pageIndex, pageSize` | `Task<FCOListResponse>` | List account's FCO orders |
| `GetFcoBySymbolAsync` | `accountNo, symbol, pageIndex, pageSize` | `Task<FCOListResponse>` | Filter FCO orders by symbol |
| `GetFcoByStatusAsync` | `accountNo, processStatus, pageIndex, pageSize` | `Task<FCOListResponse>` | Filter FCO by status (`TRIT`, `WAIT`, etc.) |
| `GetFcoByDateAsync` | `accountNo, fromDate, toDate, pageIndex, pageSize` | `Task<FCOListResponse>` | Filter FCO by date range |
| `GetFcoByIdAsync` | `accountNo, fcoId` | `Task<FCOInfo?>` | Single FCO order details |
| `GetFcoOrderBookAsync` | `fcoId, pageIndex, pageSize` | `Task<FCOOrderBookResponse>` | Execution logs of FCO |

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
    accountNo, "SSI", OrderSide.Buy, 100, OrderType.MTL, 0.5, fromDate, toDate
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

---

## 5. Rules & Guidelines for AI Code Generators

1. **Date Format for FCO**: FCO date arguments (`fromDate`, `toDate`) **must** be formatted as `"YYYY/MM/DD HH:MM:SS"`.
2. **Never hardcode secrets**: Access tokens, private keys, API secrets, and OTPs should never be committed to git.
3. **Use Enum constants**: Pass Enum constants (`OrderSide.Buy`, `OrderType.LO`, `FCOOperator.GreaterOrEqual`) instead of raw strings.
4. **Clean Builds**: Maintain C# strict typing and run `dotnet build src/SsiSdk/SsiSdk.csproj` to verify changes.
