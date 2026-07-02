# SSI .NET SDK

.NET SDK cho nền tảng giao dịch chứng khoán SSI FastConnect. Hỗ trợ đầy đủ REST API và WebSocket streaming bất đồng bộ (async) với hiệu năng cao.

**Yêu cầu:** .NET 8.0 LTS trở lên.

---

## Mục lục

- [Cài đặt](#cài-đặt)
- [Cấu hình](#cấu-hình)
- [Kiến trúc Client](#kiến-trúc-client)
- [1. Xác thực](#1-xác-thực)
- [2. Tài khoản](#2-tài-khoản)
- [3. Dữ liệu thị trường](#3-dữ-liệu-thị-trường)
- [4. Danh mục đầu tư](#4-danh-mục-đầu-tư)
- [5. Giao dịch](#5-giao-dịch)
- [6. Streaming realtime](#6-streaming-realtime)
- [7. Xử lý lỗi](#7-xử-lý-lỗi)
- [8. Cấu hình nâng cao](#8-cấu-hình-nâng-cao)
- [API Reference](#api-reference)
  - [Enums / Constants](#enums--constants)
  - [Models](#models)

---

## Cài đặt

Cài đặt package thông qua dotnet CLI:
```bash
dotnet add package SSIDeveloper.FastConnect.Sdk
```

Hoặc qua Package Manager Console:
```powershell
Install-Package SSIDeveloper.FastConnect.Sdk
```

---

## Cấu hình

Khởi tạo đối tượng `Config` để cấu hình SDK:

```csharp
using SsiSdk;

var config = new Config
{
    ClientId = "YOUR_CLIENT_ID",
    ApiKey = "YOUR_API_KEY",
    ApiSecret = "YOUR_API_SECRET",
    PrivateKey = "YOUR_PRIVATE_KEY" // Private key dùng để ký lệnh giao dịch
};
```

**Các tùy chọn cấu hình chi tiết:**

| Thuộc tính | Kiểu dữ liệu | Mặc định | Mô tả |
| :--- | :--- | :--- | :--- |
| `ClientId` | `string` | `""` | Client ID xác thực hệ thống |
| `ApiKey` | `string` | `""` | API Key được SSI cấp |
| `ApiSecret` | `string` | `""` | API Secret được SSI cấp |
| `PrivateKey` | `string` | `""` | Private Key cho chữ ký số lệnh giao dịch |
| `ApiUrl` | `string` | `"https://api.ssi.com.vn"` | Base URL của REST API |
| `StreamingUrl` | `string` | `"wss://stream.ssi.com.vn/ws/v3"` | URL kết nối WebSocket streaming |
| `TimeoutSeconds` | `int` | `60` | Thời gian timeout của request (giây) |
| `MaxRetries` | `int` | `5` | Số lần thử lại tối đa khi request lỗi |
| `RetryDelay` | `double` | `2.0` | Khoảng thời gian cơ sở giữa các lần retry (giây) |
| `RateLimitPerSecond` | `int` | `10` | Số request tối đa trên giây (0 = không giới hạn) |
| `LogLevel` | `string` | `"INFO"` | Mức độ log (`DEBUG`, `INFO`, `WARN`, `ERROR`) |
| `Proxy` | `string?` | `null` | Cấu hình proxy nếu cần đi qua proxy |

---

## Kiến trúc Client

SDK được thiết kế dạng **modular** với các client chuyên biệt:

| Client | Mô tả | Yêu cầu khởi tạo |
| :--- | :--- | :--- |
| `AuthClient` | Xác thực, lấy OTP, quản lý và tự động làm mới access token. | Cần truyền đối tượng `Config`. |
| `DataClient` | Lấy dữ liệu thị trường (OHLC, chỉ số, thông tin chứng khoán). | Yêu cầu truyền `AuthClient` (không cần OTP). |
| `TradingClient` | Đặt lệnh, xem danh mục đầu tư, truy vấn số dư và vị thế tài khoản. | Yêu cầu truyền `AuthClient` (đã xác thực bằng OTP). |
| `StreamClient` | Kết nối WebSocket realtime để nhận luồng dữ liệu thị trường hoặc sự kiện lệnh. | Yêu cầu truyền `AuthClient` (đã xác thực bằng OTP). |

**Luồng khởi tạo:**

```
Config → AuthClient → AuthenticateAsync(otp) → DataClient / TradingClient / StreamClient
```

`AuthClient` là client gốc — quản lý REST client và token. Các client `DataClient`, `TradingClient`, `StreamClient` đều nhận `AuthClient` làm tham số và chia sẻ chung kết nối HTTP.

**Services được cung cấp bởi mỗi client:**

| Client | Service | Thuộc tính truy cập | Mô tả |
| :--- | :--- | :--- | :--- |
| `AuthClient` | `TokenManager` | `auth.TokenManager` | Xác thực, OTP, refresh token |
| `DataClient` | `MarketDataService` | `data.MarketData` | OHLC, chỉ số, chứng khoán, securities summary |
| `TradingClient` | `TradingService` | `trading.Trading` | Đặt/sửa/huỷ lệnh, sức mua/bán |
| `TradingClient` | `AccountService` | `trading.Account` | Thông tin tài khoản |
| `TradingClient` | `PortfolioService` | `trading.Portfolio` | Số dư, vị thế, sổ lệnh, PPMMR |
| `StreamClient` | `StreamingService` | `stream.Streaming` | Đăng ký nhận/hủy nhận dữ liệu realtime qua WS |

### Ví dụ sử dụng nhanh (Bất đồng bộ)

```csharp
using System;
using System.Threading.Tasks;
using SsiSdk;
using SsiSdk.Models;

class Program
{
    static async Task Main(string[] args)
    {
        var config = new Config
        {
            ClientId = "YOUR_CLIENT_ID",
            ApiKey = "YOUR_API_KEY",
            ApiSecret = "YOUR_API_SECRET",
            PrivateKey = "YOUR_PRIVATE_KEY"
        };

        // 1. Khởi tạo Auth
        using var auth = new AuthClient(config);

        // Xác thực không cần OTP (cho phép gọi Market Data)
        await auth.AuthenticateAsync();

        // 2. Lấy dữ liệu thị trường
        var dataClient = new DataClient(auth);
        var ohlcList = await dataClient.MarketData.GetOhlc1MinuteAsync("SSI");
        foreach (var bar in ohlcList)
        {
            Console.WriteLine($"Date: {bar.TradingDate}, Close: {bar.ClosePrice}");
        }

        // 3. Xác thực bằng OTP để giao dịch
        Console.Write("Nhập mã OTP: ");
        string otp = Console.ReadLine() ?? "";
        await auth.AuthenticateAsync(otp);

        // 4. Đặt lệnh giao dịch & Xem danh mục
        var tradingClient = new TradingClient(auth);
        var orderResponse = await tradingClient.Trading.PlaceLimitOrderAsync(
            accountNo: "0001234567",
            symbol: "SSI",
            side: OrderSide.Buy,
            quantity: 100,
            price: 35200
        );
        Console.WriteLine($"Đặt lệnh thành công. Order ID: {orderResponse.OrderId}");

        // Xem số dư tài khoản cơ sở (Equity)
        var balance = await tradingClient.Portfolio.GetEquityBalanceAsync("0001234567");
        Console.WriteLine($"Tiền có thể rút: {balance?.AvailableCash}");

        // 5. Kết nối WebSocket streaming
        using var streamClient = new StreamClient(auth);
        streamClient.Streaming.SetOnData(msg =>
        {
            if (msg is TradeMessage trade)
            {
                Console.WriteLine($"[Realtime Trade] {trade.Symbol}: Giá {trade.Price}, Khối lượng {trade.Quantity}");
            }
        });

        await streamClient.ConnectAsync();
        // Đăng ký nhận lịch sử khớp lệnh khớp thời gian thực mã SSI
        await streamClient.Streaming.SubscribeSymbolTradeAsync(new[] { "SSI" });

        // Đợi kết nối hoạt động
        await streamClient.WaitAsync();
    }
}
```

---

## 1. Xác thực

Sử dụng `AuthClient` hoặc thông qua `TokenManager` bên trong nó. Các phương thức của `TokenManager` được uỷ quyền (delegate) trực tiếp lên `AuthClient`:

### 1.1. Xác thực không cần OTP (Dùng cho Market Data)
```csharp
Token token = await auth.AuthenticateAsync();
```

### 1.2. Xác thực đầy đủ với OTP (Dùng cho Trading/Streaming)
```csharp
Token token = await auth.AuthenticateAsync("123456");
```

### 1.3. Yêu cầu gửi mã OTP về điện thoại/email
```csharp
await auth.RequestOtpAsync();
```

### 1.4. Làm mới Token thủ công
```csharp
Token token = await auth.RefreshAsync();
```

### 1.5. Đảm bảo tự động xác thực và làm mới Token
```csharp
// Tự động làm mới nếu token hết hạn, hoặc yêu cầu xác thực bằng OTP nếu cần
string accessToken = await auth.EnsureAuthenticatedAsync(otp: "123456");
```

### 1.6. Kiểm tra trạng thái token
```csharp
Token? currentToken = auth.TokenManager.Token;
string accessToken = auth.AccessToken;
bool isExpired = auth.TokenManager.IsTokenExpired;
bool hasRefreshToken = auth.TokenManager.HasRefreshToken;
bool isRefreshTokenExpired = auth.TokenManager.IsRefreshTokenExpired;
```

---

## 2. Tài khoản

Sử dụng `AccountService` thông qua `TradingClient.Account` để lấy danh sách tài khoản liên kết:

```csharp
var trading = new TradingClient(auth);
List<Account> accounts = await trading.Account.GetAccountInfoAsync();

foreach (var acc in accounts)
{
    Console.WriteLine($"Tài khoản: {acc.AccountNo}, Loại: {acc.AccountType}");
}
```

---

## 3. Dữ liệu thị trường

Truy cập qua `data.MarketData` (client `DataClient`).

> **Lưu ý:** `DataClient` chỉ yêu cầu xác thực cơ bản `auth.AuthenticateAsync()` (không cần OTP).

### Tổng quan các phương thức

| Nhóm | Phương thức | Mô tả |
| :--- | :--- | :--- |
| **OHLC trong ngày** | `GetOhlc1MinuteAsync(symbol)` | Nến 1 phút trong ngày |
| | `GetOhlc3MinuteAsync(symbol)` | Nến 3 phút trong ngày |
| | `GetOhlc5MinuteAsync(symbol)` | Nến 5 phút trong ngày |
| | `GetOhlc15MinuteAsync(symbol)` | Nến 15 phút trong ngày |
| | `GetOhlc1HourAsync(symbol)` | Nến 1 giờ trong ngày |
| **OHLC lịch sử** | `GetOhlc1MinuteHistoricalAsync(symbol, fromDate, toDate, page, size)` | Nến 1 phút lịch sử |
| | `GetOhlc3MinuteHistoricalAsync(...)` | Nến 3 phút lịch sử |
| | `GetOhlc5MinuteHistoricalAsync(...)` | Nến 5 phút lịch sử |
| | `GetOhlc15MinuteHistoricalAsync(...)` | Nến 15 phút lịch sử |
| | `GetOhlc1HourHistoricalAsync(...)` | Nến 1 giờ lịch sử |
| | `GetOhlc1DayHistoricalAsync(...)` | Nến 1 ngày lịch sử |
| | `GetOhlc1WeekHistoricalAsync(...)` | Nến 1 tuần lịch sử |
| | `GetOhlc1MonthHistoricalAsync(...)` | Nến 1 tháng lịch sử |
| **Chỉ số (Index)** | `GetIndexesAsync()` | Danh sách tất cả chỉ số |
| | `GetIndexesByBoardAsync(board)` | Chỉ số theo sàn (HOSE, HNX, UPCOM) |
| **Tổng hợp chỉ số** | `GetIndexSummaryAsync(index)` | Summary chỉ số hiện tại |
| | `GetIndexSummaryHistoricalAsync(index, tradingDate)` | Summary chỉ số lịch sử |
| | `GetBoardSummaryAsync(board)` | Summary sàn hiện tại |
| | `GetBoardSummaryHistoricalAsync(board, tradingDate)` | Summary sàn lịch sử |
| **Chứng khoán** | `GetSecuritiesInfoAsync(symbol)` | Thông tin chi tiết một mã |
| | `GetSecuritiesInfoByIndexAsync(index)` | Lấy mã chứng khoán theo chỉ số |
| | `GetSecuritiesInfoByBoardAsync(board)` | Lấy mã chứng khoán theo sàn |
| **Tổng hợp mã** | `GetSecuritiesSummaryAsync(symbol)` | Tổng hợp thông tin giao dịch mã hiện tại |
| | `GetSecuritiesSummaryHistoricalAsync(symbol, fromDate, toDate)` | Tổng hợp giao dịch mã lịch sử |
| | `GetSecuritiesSummaryByIndexAsync(index)` | Tổng hợp mã theo chỉ số |
| | `GetSecuritiesSummaryByIndexHistoricalAsync(index, fromDate, toDate)` | Tổng hợp mã theo chỉ số lịch sử |

### 3.1. Dữ liệu OHLC (Nến giao dịch)

Nhận dữ liệu nến cho phiên hiện tại hoặc dữ liệu lịch sử có phân trang (`page`, `size`).

```csharp
// OHLC 1 phút trong ngày
List<OhlcData> ohlcToday = await data.MarketData.GetOhlc1MinuteAsync("SSI");

// OHLC 1 ngày lịch sử
List<OhlcData> ohlcHistory = await data.MarketData.GetOhlc1DayHistoricalAsync(
    symbol: "SSI",
    fromDate: "2026/06/01",
    toDate: "2026/06/30",
    page: 1,
    size: 100
);
```

### 3.2. Danh sách chỉ số thị trường
```csharp
// Lấy tất cả chỉ số
List<MarketIndexes> indices = await data.MarketData.GetIndexesAsync();

// Lấy theo sàn
List<MarketIndexes> hoseIndices = await data.MarketData.GetIndexesByBoardAsync(Board.HOSE);
```

### 3.3. Tổng hợp chỉ số (Index Summary)
```csharp
// Summary hiện tại
MarketIndexSummary? summary = await data.MarketData.GetIndexSummaryAsync("VNINDEX");

// Summary lịch sử
MarketIndexSummary? histSummary = await data.MarketData.GetIndexSummaryHistoricalAsync("VNINDEX", "2026/06/15");
```

### 3.4. Thông tin chứng khoán
```csharp
// Thông tin mã SSI
SecuritiesInfo? info = await data.MarketData.GetSecuritiesInfoAsync("SSI");

// Danh sách mã trong rổ VN30
List<SecuritiesInfo> vn30Stocks = await data.MarketData.GetSecuritiesInfoByIndexAsync("VN30");
```

---

## 4. Danh mục đầu tư

Sử dụng `PortfolioService` thông qua `TradingClient.Portfolio` để quản lý tài sản, số dư và vị thế:

### Tổng quan các phương thức

| Nhóm | Phương thức | Kiểu trả về |
| :--- | :--- | :--- |
| **Số dư** | `GetEquityBalanceAsync(accountNo)` | `Task<EquityAccountBalance?>` |
| | `GetDerivativeBalanceAsync(accountNo)` | `Task<DerivativeAccountBalance?>` |
| **Vị thế** | `GetEquityPositionsAsync(accountNo)` | `Task<List<EquityPosition>>` |
| | `GetDerivativePositionsAsync(accountNo)` | `Task<AllDerivativePosition>` (Bao gồm cả Open và Closed) |
| | `GetOpenDerivativePositionsAsync(accountNo)` | `Task<List<DerivativePosition>>` |
| | `GetClosedDerivativePositionsAsync(accountNo)` | `Task<List<DerivativePosition>>` |
| **Sổ lệnh** | `GetTodayOrdersAsync(accountNo)` | `Task<List<Order>>` |
| | `GetHistoricalOrdersAsync(accountNo, fromDate, toDate)` | `Task<List<Order>>` |
| **Sức mua (PPMMR)** | `GetEquityPpmmrAsync(accountNo)` | `Task<EquityPPMMR?>` |
| | `GetDerivativePpmmrAsync(accountNo)` | `Task<DerivativePPMMR?>` |

### 4.1. Số dư tài khoản
```csharp
// Số dư tài khoản cơ sở
EquityAccountBalance? eqBalance = await trading.Portfolio.GetEquityBalanceAsync("0001234567");
Console.WriteLine($"Tiền mặt khả dụng: {eqBalance?.AvailableCash}");

// Số dư tài khoản phái sinh
DerivativeAccountBalance? derBalance = await trading.Portfolio.GetDerivativeBalanceAsync("0001234568");
Console.WriteLine($"Số dư tài khoản phái sinh: {derBalance?.AccountBalance}");
```

### 4.2. Vị thế nắm giữ (Positions)
```csharp
// Vị thế chứng khoán cơ sở
List<EquityPosition> positions = await trading.Portfolio.GetEquityPositionsAsync("0001234567");

// Chỉ vị thế mở phái sinh
List<DerivativePosition> openDerPositions = await trading.Portfolio.GetOpenDerivativePositionsAsync("0001234568");
```

### 4.3. Truy vấn sổ lệnh
```csharp
// Lệnh trong ngày
List<Order> todayOrders = await trading.Portfolio.GetTodayOrdersAsync("0001234567");

// Lệnh lịch sử
List<Order> historicalOrders = await trading.Portfolio.GetHistoricalOrdersAsync("0001234567", "2026/06/01", "2026/06/30");
```

---

## 5. Giao dịch

Truy cập thông qua `TradingService` tại `TradingClient.Trading`. Các lệnh gửi đi sẽ được tự động ký số bằng thuộc tính `PrivateKey` đã được cấu hình trong `Config`.

### Tổng quan các phương thức

| Nhóm | Phương thức | Kiểu trả về |
| :--- | :--- | :--- |
| **Đặt lệnh** | `PlaceOrderAsync(accountNo, symbol, side, quantity, price, orderType)` | `Task<PlaceOrderResponse>` |
| | `PlaceLimitOrderAsync(accountNo, symbol, side, quantity, price)` | `Task<PlaceOrderResponse>` |
| | `PlaceMarketOrderAsync(accountNo, symbol, side, quantity)` | `Task<PlaceOrderResponse>` |
| | `PlaceAtoOrderAsync(accountNo, symbol, side, quantity)` | `Task<PlaceOrderResponse>` |
| | `PlaceAtcOrderAsync(accountNo, symbol, side, quantity)` | `Task<PlaceOrderResponse>` |
| **Sửa lệnh** | `ModifyOrderPriceAsync(accountNo, clientRequestId, price)` | `Task<ModifyOrderResponse>` |
| | `ModifyOrderPriceByOrderIdAsync(accountNo, orderId, price)` | `Task<ModifyOrderResponse>` |
| | `ModifyOrderQuantityAsync(accountNo, clientRequestId, quantity)` | `Task<ModifyOrderResponse>` |
| | `ModifyOrderQuantityByOrderIdAsync(accountNo, orderId, quantity)` | `Task<ModifyOrderResponse>` |
| **Huỷ lệnh** | `CancelOrderAsync(accountNo, clientRequestId)` | `Task<CancelOrderResponse>` |
| | `CancelOrderByOrderIdAsync(accountNo, orderId)` | `Task<CancelOrderResponse>` |
| **Sức mua tối đa**| `GetMaxBuySellAsync(accountNo, symbol, price)` | `Task<MaxBuySellResponse>` |
| | `GetMaxBuySellAtMarketPriceAsync(accountNo, symbol)` | `Task<MaxBuySellResponse>` |

### 5.1. Đặt lệnh mới
```csharp
// Đặt lệnh giới hạn (LO)
PlaceOrderResponse limitOrder = await trading.Trading.PlaceLimitOrderAsync(
    accountNo: "0001234567",
    symbol: "SSI",
    side: OrderSide.Buy,
    quantity: 100,
    price: 35000
);

// Đặt lệnh thị trường (MTL)
PlaceOrderResponse marketOrder = await trading.Trading.PlaceMarketOrderAsync(
    accountNo: "0001234567",
    symbol: "SSI",
    side: OrderSide.Sell,
    quantity: 100
);
```

### 5.2. Sửa lệnh
*Lưu ý: Bạn chỉ được sửa đổi hoặc giá, hoặc khối lượng của lệnh cũ trong một thời điểm.*
```csharp
// Sửa giá lệnh theo Client Request ID
ModifyOrderResponse modPrice = await trading.Trading.ModifyOrderPriceAsync(
    accountNo: "0001234567",
    clientRequestId: "orig-client-req-id",
    price: 35100
);

// Sửa khối lượng lệnh theo Order ID của sàn
ModifyOrderResponse modQty = await trading.Trading.ModifyOrderQuantityByOrderIdAsync(
    accountNo: "0001234567",
    orderId: "exchange-order-id",
    quantity: 200
);
```

### 5.3. Huỷ lệnh
```csharp
// Huỷ lệnh bằng Client Request ID
CancelOrderResponse cancelResponse = await trading.Trading.CancelOrderAsync(
    accountNo: "0001234567",
    clientRequestId: "orig-client-req-id"
);
```

### 5.4. Sức mua/bán tối đa
```csharp
MaxBuySellResponse maxInfo = await trading.Trading.GetMaxBuySellAsync("0001234567", "SSI", 35000);
Console.WriteLine($"Khối lượng mua tối đa: {maxInfo.MaxBuyQuantity}");
```

---

## 6. Streaming realtime

Sử dụng `StreamClient` để đăng ký kết nối và nhận luồng dữ liệu cập nhật từ WebSocket (Live Market Data & Order Event).

### Tổng quan các phương thức và Callbacks

| Nhóm | Phương thức | Mô tả |
| :--- | :--- | :--- |
| **Kết nối** | `ConnectAsync()` | Thiết lập kết nối WebSocket |
| | `Disconnect()` | Ngắt kết nối và đóng loop |
| | `WaitAsync(timeout)` | Giữ thread chạy liên tục để đợi nhận dữ liệu |
| **Heartbeat** | `PingAsync(intervalSeconds)` | Ping để kiểm tra và duy trì kết nối |
| | `StopPingLoop()` | Dừng ping loop tự động |
| **Đăng ký mã** | `SubscribeSymbolAsync(symbols)` | Đăng ký nhận toàn bộ (Trade, Quote, Room) cho danh sách mã |
| | `SubscribeSymbolTradeAsync(symbols)` | Chỉ đăng ký khớp lệnh realtime |
| | `SubscribeSymbolQuoteAsync(symbols)` | Chỉ đăng ký bảng giá (Best Bid/Ask) realtime |
| | `SubscribeSymbolRoomAsync(symbols)` | Chỉ đăng ký Room nước ngoài |
| | `SubscribeSymbolPutThroughAsync(symbols)`| Chỉ đăng ký lệnh thỏa thuận |
| | `SubscribeSymbolOddLotAsync(symbols)` | Chỉ đăng ký lô lẻ |
| | `SubscribeSymbolOhlcvAsync(symbols, interval)` | Đăng ký nến realtime theo timeframe |
| **Đăng ký sàn/chỉ số**| `SubscribeBoardAsync(boards)` | Đăng ký nhận toàn bộ dữ liệu theo sàn |
| | `SubscribeIndexAsync(indices)` | Đăng ký nhận toàn bộ dữ liệu theo chỉ số |
| **Đăng ký tài khoản**| `SubscribeOrderStatusAsync(accountNo)` | Lắng nghe sự kiện lệnh (đặt, sửa, khớp, hủy) |
| | `SubscribePortfolioAsync(accountNo)` | Lắng nghe sự kiện thay đổi tài sản/danh mục |

#### Callbacks hỗ trợ cấu hình qua `stream.Streaming`:
- `SetOnData(Action<object> callback)`: Nhận các thông báo dữ liệu thị trường đã phân tích (như `TradeMessage`, `QuoteMessage`, `IntervalMessage`, v.v.)
- `SetOnTrading(Action<object> callback)`: Nhận thông báo sự kiện giao dịch tài khoản (`OrderStatusMessage` hoặc `PortfolioMessage`).
- `SetOnHeartbeat(Action<HeartbeatMessage> callback)`: Nhận sự kiện phản hồi Ping/Pong duy trì kết nối.

### 6.1. Thiết lập Callback và kết nối
```csharp
using var stream = new StreamClient(auth);

// Đăng ký callback nhận dữ liệu thị trường
stream.Streaming.SetOnData(msg =>
{
    switch (msg)
    {
        case TradeMessage trade:
            Console.WriteLine($"[Khớp lệnh] {trade.Symbol}: Giá {trade.Price}, KL {trade.Quantity}");
            break;
        case QuoteMessage quote:
            Console.WriteLine($"[Bảng giá] {quote.Symbol}: Giá mua tốt nhất {quote.BidPrices[0]}");
            break;
    }
});

// Đăng ký callback nhận sự kiện tài khoản
stream.Streaming.SetOnTrading(msg =>
{
    if (msg is OrderStatusMessage orderUpdate)
    {
        Console.WriteLine($"[Sự kiện lệnh] Lệnh {orderUpdate.OrderId} cập nhật trạng thái: {orderUpdate.Status}");
    }
});

// Kết nối WebSocket
await stream.ConnectAsync();

// Đăng ký nhận thông tin
await stream.Streaming.SubscribeSymbolAsync(new[] { "SSI", "FPT" });
await stream.Streaming.SubscribeOrderStatusAsync("0001234567");

// Giữ kết nối hoạt động
await stream.WaitAsync();
```

---

## 7. Xử lý lỗi

Mọi Exception phát sinh từ SDK đều được kế thừa từ lớp `SsiException`:

```csharp
try
{
    var response = await trading.Trading.PlaceLimitOrderAsync("0001234567", "SSI", OrderSide.Buy, 100, 35000);
}
catch (ValidationException ex)
{
    Console.WriteLine($"Dữ liệu đầu vào không hợp lệ: {ex.Message}");
}
catch (AuthenticationException ex)
{
    Console.WriteLine($"Lỗi xác thực: {ex.Message} (Mã: {ex.Code})");
}
catch (ApiException ex)
{
    Console.WriteLine($"API SSI báo lỗi: {ex.Message} (HTTP status: {ex.StatusCode})");
}
catch (RateLimitException ex)
{
    Console.WriteLine($"Bị giới hạn tần suất. Thử lại sau {ex.RetryAfter} giây.");
}
catch (SsiException ex)
{
    Console.WriteLine($"Lỗi SDK SSI: {ex.Message}");
}
```

---

## 8. Cấu hình nâng cao

Tất cả các tham số nâng cao như Retry, RateLimit hay Proxy đều có thể điều chỉnh qua đối tượng `Config` khi khởi tạo:

```csharp
var config = new Config
{
    ClientId = "YOUR_CLIENT_ID",
    ApiKey = "YOUR_API_KEY",
    ApiSecret = "YOUR_API_SECRET",
    TimeoutSeconds = 30,
    MaxRetries = 3,
    RetryDelay = 1.5,
    RateLimitPerSecond = 5,
    LogLevel = "DEBUG",
    Proxy = "http://127.0.0.1:8888" // Cấu hình Proxy
};
```

---

## API Reference

### Enums / Constants

Các lớp chứa hằng số (constants) được cung cấp trong namespace `SsiSdk`:

#### `Board`
- `Board.HOSE` = `"HOSE"`
- `Board.HNX` = `"HNX"`
- `Board.UPCOM` = `"UPCOM"`

#### `Timeframe`
- `Timeframe.Minute1` = `"1m"`
- `Timeframe.Minute3` = `"3m"`
- `Timeframe.Minute5` = `"5m"`
- `Timeframe.Minute15` = `"15m"`
- `Timeframe.Hour1` = `"1h"`
- `Timeframe.Day1` = `"1d"`
- `Timeframe.Week1` = `"1w"`
- `Timeframe.Month1` = `"1M"`

#### `OrderSide`
- `OrderSide.Buy` = `"B"`
- `OrderSide.Sell` = `"S"`

#### `OrderType`
- `OrderType.ATO` = `"ATO"`
- `OrderType.ATC` = `"ATC"`
- `OrderType.LO` = `"LO"`
- `OrderType.MTL` = `"MTL"`
- `OrderType.MP` = `"MP"`
- `OrderType.MOK` = `"MOK"`
- `OrderType.MAK` = `"MAK"`
- `OrderType.PLO` = `"PLO"`

#### `OrderStatus`
- `OrderStatus.Pending` = `"PD"`
- `OrderStatus.PendingApproval` = `"WA"`
- `OrderStatus.Ready` = `"RS"`
- `OrderStatus.Sent` = `"SD"`
- `OrderStatus.Queued` = `"QU"`
- `OrderStatus.Filled` = `"FF"`
- `OrderStatus.PartialFilled` = `"PF"`
- `OrderStatus.PartialCancelled` = `"FFPC"`
- `OrderStatus.PendingModify` = `"WM"`
- `OrderStatus.PendingCancel` = `"WC"`
- `OrderStatus.Cancelled` = `"CL"`
- `OrderStatus.Rejected` = `"RJ"`
- `OrderStatus.Expired` = `"EX"`
- `OrderStatus.PreSession` = `"IAV"`

#### `AccountType`
- `AccountType.Equity` = `"Cash"`
- `AccountType.EquityMargin` = `"Margin"`
- `AccountType.Derivative` = `"Derivative"`

#### `StreamingType`
- `StreamingType.Order` = `"orderEvent"`
- `StreamingType.OrderMatch` = `"orderMatchEvent"`
- `StreamingType.Portfolio` = `"clientPortfolioEvent"`

#### `DataType`
- `DataType.Quote` = `"quote"`
- `DataType.Trade` = `"trade"`
- `DataType.OddLot` = `"oddlot"`
- `DataType.Market` = `"market"`
- `DataType.Room` = `"room"`
- `DataType.Put` = `"put"`

---

### Models

Các lớp dữ liệu định kiểu (strongly typed models) nằm trong namespace `SsiSdk.Models`:

#### `Token`
| Thuộc tính | Kiểu dữ liệu | Mô tả |
| :--- | :--- | :--- |
| `AccessToken` | `string` | Access token dùng cho các request |
| `TokenType` | `string` | Loại token (Thường là `"Bearer"`) |
| `ExpiresAt` | `long` | Thời điểm hết hạn (Unix timestamp) |
| `RefreshToken` | `string` | Token dùng để làm mới access token |
| `RefreshTokenExpiresAt` | `long` | Thời điểm hết hạn của refresh token (Unix timestamp) |

#### `Account`
| Thuộc tính | Kiểu dữ liệu | Mô tả |
| :--- | :--- | :--- |
| `AccountNo` | `string` | Số tài khoản giao dịch |
| `AccountType` | `string` | Loại tài khoản (`"Cash"`, `"Margin"`, `"Derivative"`) |

#### `OhlcData`
| Thuộc tính | Kiểu dữ liệu | Mô tả |
| :--- | :--- | :--- |
| `Symbol` | `string` | Mã chứng khoán |
| `TradingDate` | `string` | Ngày giao dịch |
| `OpenPrice` | `double` | Giá mở cửa |
| `HighPrice` | `double` | Giá cao nhất |
| `LowPrice` | `double` | Giá thấp nhất |
| `ClosePrice` | `double` | Giá đóng cửa |
| `Volume` | `int` | Khối lượng giao dịch |
| `Value` | `double` | Giá trị giao dịch |

#### `MarketIndexes`
| Thuộc tính | Kiểu dữ liệu | Mô tả |
| :--- | :--- | :--- |
| `Index` | `string` | Mã chỉ số (ví dụ: `"VNINDEX"`) |
| `IndexName` | `string` | Tên đầy đủ của chỉ số |
| `Board` | `string?` | Sàn giao dịch liên kết |

#### `MarketIndexSummary`
| Thuộc tính | Kiểu dữ liệu | Mô tả |
| :--- | :--- | :--- |
| `Index` | `string` | Mã chỉ số |
| `Board` | `string` | Sàn giao dịch |
| `TradingDate` | `string` | Ngày giao dịch |
| `TotalTrade` | `int` | Tổng số lượng giao dịch |
| `TotalTradeValue` | `double` | Tổng giá trị giao dịch |
| `TotalMatch` | `int` | Tổng khối lượng khớp lệnh |
| `TotalMatchValue` | `double` | Tổng giá trị khớp lệnh |
| `TotalDeal` | `int` | Tổng khối lượng thỏa thuận |
| `TotalDealValue` | `double` | Tổng giá trị thỏa thuận |
| `IndexChange` | `double` | Điểm thay đổi |
| `IndexChangePercent` | `double` | Tỷ lệ phần trăm thay đổi |
| `IndexValue` | `double` | Điểm chỉ số hiện tại |
| `TotalAdvanceStock` | `int` | Số mã tăng |
| `TotalDeclineStock` | `int` | Số mã giảm |
| `TotalSteadyStock` | `int` | Số mã đứng giá |
| `TotalCeilingStock` | `int` | Số mã tăng trần |
| `TotalFloorStock` | `int` | Số mã giảm sàn |
| `TotalPropBuy` | `int` | Khối lượng mua tự doanh |
| `TotalPropBuyValue` | `double` | Giá trị mua tự doanh |
| `TotalPropSell` | `int` | Khối lượng bán tự doanh |
| `TotalPropSellValue` | `double` | Giá trị bán tự doanh |

#### `SecuritiesInfo`
| Thuộc tính | Kiểu dữ liệu | Mô tả |
| :--- | :--- | :--- |
| `Symbol` | `string` | Mã chứng khoán |
| `Board` | `string?` | Sàn giao dịch |
| `Index` | `string` | Mã chỉ số liên kết |
| `SymbolNameVi` | `string` | Tên tiếng Việt |
| `SymbolNameEn` | `string` | Tên tiếng Anh |
| `LotSize` | `int` | Kích thước lô giao dịch tối thiểu |
| `MaturityDate` | `string?` | Ngày đáo hạn (CW/Phái sinh) |
| `FirstTradingDate` | `string?` | Ngày bắt đầu giao dịch |
| `LastTradingDate` | `string?` | Ngày cuối cùng giao dịch |
| `CwUnderlyingSymbol` | `string?` | Mã chứng khoán cơ sở của chứng quyền |
| `CwExercisePrice` | `double?` | Giá thực hiện chứng quyền |
| `CwExecutionRatio` | `double?` | Tỷ lệ chuyển đổi chứng quyền |
| `ListedShares` | `int` | Số lượng cổ phiếu niêm yết |
| `IcbCode` | `string?` | Mã ngành ICB |
| `IcbName` | `string?` | Tên ngành ICB |
| `IIndex` | `double?` | Chỉ số I |
| `INav` | `double?` | Giá trị tài sản ròng NAV |

#### `SecuritiesSummary`
| Thuộc tính | Kiểu dữ liệu | Mô tả |
| :--- | :--- | :--- |
| `Symbol` | `string` | Mã chứng khoán |
| `TradingDate` | `string` | Ngày giao dịch |
| `PriceChange` | `double` | Thay đổi giá |
| `PriceChangePercent` | `double` | % thay đổi giá |
| `OpenPrice` | `double` | Giá mở cửa |
| `HighPrice` | `double` | Giá cao nhất |
| `LowPrice` | `double` | Giá thấp nhất |
| `ClosePrice` | `double` | Giá đóng cửa |
| `AveragePrice` | `double` | Giá trung bình |
| `TotalMatch` | `int` | Tổng khối lượng khớp lệnh |
| `TotalMatchValue` | `double` | Tổng giá trị khớp lệnh |
| `TotalBuy` | `int` | Tổng khối lượng mua |
| `TotalTradeBuy` | `double` | Tổng giá trị mua |
| `TotalSell` | `int` | Tổng khối lượng bán |
| `TotalTradeSell` | `double` | Tổng giá trị bán |

#### `EquityAccountBalance`
| Thuộc tính | Kiểu dữ liệu | Mô tả |
| :--- | :--- | :--- |
| `AccountNo` | `string` | Số tài khoản |
| `AvailableCash` | `double` | Tiền mặt khả dụng |
| `TotalDebt` | `double` | Tổng dư nợ |
| `InterestLoan` | `double` | Tiền lãi vay phát sinh |
| `OverdueFeeLoan` | `double` | Phí vay quá hạn |
| `Withdrawal` | `double` | Tiền mặt có thể rút |
| `OnHoldCash` | `double` | Tiền đang tạm giữ |
| `SellUnmatched` | `double` | Giá trị lệnh bán chưa khớp |
| `SellT0` / `SellT1` / `SellT2` | `double` | Tiền bán chờ về T+0, T+1, T+2 |
| `BuyUnmatched` | `double` | Giá trị lệnh mua chưa khớp |
| `BuyT0` / `BuyT1` / `BuyT2` | `double` | Tiền mua chờ thanh toán T+0, T+1, T+2 |
| `AdvanceCashT0` / `AdvanceCashT1` | `double` | Giá trị ứng trước tiền bán T+0, T+1 |
| `HoldSubscription` | `double` | Tiền phong toả đăng ký quyền mua |
| `BankBalance` | `double` | Số dư tài khoản liên kết ngân hàng |
| `Dividend` | `double` | Cổ tức bằng tiền mặt |
| `DividendMargin` | `double` | Cổ tức ký quỹ |
| `BlockCash` | `double` | Tiền bị phong toả |
| `InterestCash` | `double` | Tiền lãi tiền gửi nhận được |
| `LimitT0` | `double` | Hạn mức mua T+0 |
| `TermDeposit` | `double` | Tiền gửi có kỳ hạn tại SSI |

#### `DerivativeAccountBalance`
| Thuộc tính | Kiểu dữ liệu | Mô tả |
| :--- | :--- | :--- |
| `AccountNo` | `string` | Số tài khoản |
| `AccountBalance` | `double` | Số dư tài khoản |
| `Fee` | `double` | Phí giao dịch |
| `Commission` | `double` | Hoa hồng |
| `Interest` | `double` | Lãi vay phát sinh |
| `ExtInterest` | `double` | Lãi ngoài |
| `Loan` | `double` | Số tiền vay |
| `DeliveryAmount` | `double` | Tiền thanh toán chuyển giao |
| `FloatingPL` | `double` | Lãi/lỗ tạm tính |
| `TradingPL` | `double` | Lãi/lỗ thực tế |
| `TotalPL` | `double` | Tổng lãi/lỗ |
| `Withdrawable` | `double` | Số tiền có thể rút |
| `CashSSI` | `double` | Tiền đang lưu giữ tại SSI |
| `ValidNonCashSSI` | `double` | Giá trị tài sản thế chấp phi tiền mặt tại SSI |
| `CashWithdrawableSSI` | `double` | Tiền mặt có thể rút tại SSI |
| `CashVSDC` | `double` | Tiền mặt lưu ký tại VSDC |
| `ValidNonCashVSDC` | `double` | Giá trị tài sản phi tiền mặt thế chấp tại VSDC |
| `CashWithdrawableVSDC` | `double` | Tiền mặt có thể rút từ VSDC |

#### `EquityPosition`
| Thuộc tính | Kiểu dữ liệu | Mô tả |
| :--- | :--- | :--- |
| `AccountNo` | `string` | Số tài khoản |
| `Symbol` | `string` | Mã chứng khoán |
| `Quantity` | `int` | Tổng khối lượng nắm giữ |
| `BlockQuantity` | `int` | Khối lượng bị phong toả |
| `DividendQuantity` | `int` | Khối lượng cổ tức chờ về |
| `BuyingQuantity` / `BoughtQuantity` | `int` | Khối lượng đang mua / đã mua trong ngày |
| `SellingQuantity` / `SoldQuantity` | `int` | Khối lượng đang bán / đã bán trong ngày |
| `T1SellQuantity` / `T2SellQuantity` | `int` | Khối lượng bán khớp chờ về T+1, T+2 |
| `CostPrice` | `double` | Giá vốn bình quân |
| `MortgageQuantity` | `int` | Khối lượng cầm cố |
| `SellableQuantity` | `int` | Khối lượng có thể đặt bán |
| `RestrictedQuantity` | `int` | Khối lượng hạn chế chuyển nhượng |

#### `DerivativePosition`
| Thuộc tính | Kiểu dữ liệu | Mô tả |
| :--- | :--- | :--- |
| `AccountNo` | `string` | Số tài khoản |
| `Symbol` | `string` | Mã hợp đồng tương lai |
| `Long` | `int` | Vị thế Mua (Long) |
| `Short` | `int` | Vị thế Bán (Short) |
| `Net` | `int` | Vị thế ròng |
| `BidAvgPrice` | `double` | Giá mua trung bình |
| `AskAvgPrice` | `double` | Giá bán trung bình |
| `TradePrice` | `double` | Giá khớp giao dịch hiện tại |
| `FloatingPL` | `double` | Lãi/lỗ tạm tính |
| `TradingPL` | `double` | Lãi/lỗ thực tế |

#### `Order`
| Thuộc tính | Kiểu dữ liệu | Mô tả |
| :--- | :--- | :--- |
| `AccountNo` | `string` | Số tài khoản |
| `ClientRequestId` | `string` | Request ID từ client tự tạo |
| `OrderId` | `string` | Order ID trả về từ sàn |
| `Symbol` | `string` | Mã chứng khoán |
| `Side` | `string` | Chiều giao dịch (`"B"` hoặc `"S"`) |
| `OrderType` | `string` | Loại lệnh (`"LO"`, `"MTL"`, v.v.) |
| `Price` | `double` | Giá đặt lệnh |
| `AvgPrice` | `double` | Giá khớp trung bình |
| `Quantity` | `int` | Khối lượng đặt |
| `OsQuantity` | `int` | Khối lượng còn lại chưa khớp |
| `FilledQuantity` | `int` | Khối lượng đã khớp thành công |
| `CancelQuantity` | `int` | Khối lượng đã bị huỷ |
| `Status` | `string` | Trạng thái lệnh hiện tại |
| `InputTime` | `string` | Thời gian đặt lệnh |
| `ModifyTime` | `string` | Thời gian sửa lệnh gần nhất |
| `Message` | `string` | Lý do lỗi hoặc thông báo đi kèm |

#### `PlaceOrderResponse` / `ModifyOrderResponse` / `CancelOrderResponse`
| Thuộc tính | Kiểu dữ liệu | Mô tả |
| :--- | :--- | :--- |
| `OrderId` | `string` | ID của lệnh giao dịch |
| `ClientRequestId` | `string` | ID yêu cầu của Client |
| `ClientModifyId` / `ClientCancelId` | `string` | ID yêu cầu sửa / huỷ của Client |
| `Status` | `string` | Trạng thái của lệnh |

#### `MaxBuySellResponse`
| Thuộc tính | Kiểu dữ liệu | Mô tả |
| :--- | :--- | :--- |
| `AccountNo` | `string` | Số tài khoản |
| `Symbol` | `string` | Mã chứng khoán |
| `MaxBuyQuantity` | `int` | Số lượng mua tối đa có thể đặt |
| `MaxSellQuantity` | `int` | Số lượng bán tối đa có thể đặt |
| `MarginRatio` | `string` | Tỷ lệ ký quỹ hiện tại |
| `PurchasePower` | `string` | Sức mua hiện tại |

#### `TradeMessage` (Streaming)
| Thuộc tính | Kiểu dữ liệu | Mô tả |
| :--- | :--- | :--- |
| `Type` | `string` | `"trade"` |
| `TradingTime` | `string` | Thời gian giao dịch phát sinh khớp |
| `Symbol` | `string` | Mã chứng khoán |
| `Price` | `double` | Giá khớp lệnh |
| `Quantity` | `int` | Khối lượng khớp |
| `Side` | `string` | Bên chủ động khớp (`"B"`, `"S"`, hoặc `"U"`) |
| `TotalVolume` | `int` | Tổng khối lượng khớp từ đầu phiên |

#### `QuoteMessage` (Streaming)
| Thuộc tính | Kiểu dữ liệu | Mô tả |
| :--- | :--- | :--- |
| `Type` | `string` | `"quote"` |
| `TradingTime` | `string` | Thời gian cập nhật giá |
| `Symbol` | `string` | Mã chứng khoán |
| `BidPrices` | `List<double>` | Danh sách các mức giá mua tốt nhất |
| `BidVolumes` | `List<int>` | Khối lượng mua tương ứng |
| `AskPrices` | `List<double>` | Danh sách các mức giá bán tốt nhất |
| `AskVolumes` | `List<int>` | Khối lượng bán tương ứng |

#### `OrderStatusMessage` (Streaming)
| Thuộc tính | Kiểu dữ liệu | Mô tả |
| :--- | :--- | :--- |
| `Type` | `string` | `"orderEvent"` |
| `AccountNo` | `string` | Số tài khoản phát sinh sự kiện |
| `ClientRequestId` | `string` | ID yêu cầu phía Client |
| `OrderId` | `string` | ID lệnh của hệ thống |
| `Symbol` | `string` | Mã chứng khoán |
| `Side` | `string` | Chiều đặt (`"B"` / `"S"`) |
| `OrderType` | `string` | Loại lệnh (`"LO"`, v.v.) |
| `Price` | `double` | Giá đặt lệnh |
| `Quantity` | `int` | Khối lượng đặt ban đầu |
| `OsQuantity` | `int` | Khối lượng chờ khớp còn lại |
| `FilledQuantity` | `int` | Khối lượng đã khớp |
| `CancelQuantity` | `int` | Khối lượng đã huỷ |
| `Status` | `string` | Trạng thái hiện tại của lệnh |
| `InputTime` | `string` | Thời điểm đặt lệnh |
| `ModifyTime` | `string` | Thời điểm sửa lệnh gần nhất |
| `Message` | `string` | Mô tả chi tiết trạng thái hoặc lý do lỗi |

#### `PortfolioMessage` (Streaming)
| Thuộc tính | Kiểu dữ liệu | Mô tả |
| :--- | :--- | :--- |
| `Type` | `string` | `"clientPortfolioEvent"` |
| `AccountNo` | `string` | Số tài khoản phát sinh sự kiện |
| `TotalAsset` | `double` | Tổng tài sản khả dụng |
| `CashBalance` | `double` | Số dư tiền mặt thay đổi |
| `StockValue` | `double` | Tổng giá trị danh mục cổ phiếu nắm giữ |

#### `HeartbeatMessage` (Streaming)
| Thuộc tính | Kiểu dữ liệu | Mô tả |
| :--- | :--- | :--- |
| `Method` | `string` | Phương thức gửi (`"ping_pong"`) |
| `Channel` | `string` | Kênh WebSocket (`"HEARTBEAT"`) |
| `Status` | `string` | Trạng thái (thường là `"success"`) |
| `Message` | `string` | Chi tiết phản hồi tin nhắn từ server |
