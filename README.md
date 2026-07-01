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

### Ví dụ sử dụng nhanh (Bất đồng bộ)

```csharp
using System;
using System   .Threading.Tasks;
using SsiSdk;

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
            Console.WriteLine($"Nhận dữ liệu realtime: {msg}");
        });

        await streamClient.ConnectAsync();
        // Đăng ký nhận lịch sử khớp lệnh khớp thời gian thực mã SSI
        await streamClient.Streaming.SubscribeSymbolTradeAsync(new[] { "SSI" });
        
        // Đợi kết nối
        await streamClient.WaitAsync();
    }
}
```

---

## 1. Xác thực

`AuthClient` cung cấp các phương thức thông qua `TokenManager`:

### Xác thực cơ bản (Không OTP - Dùng cho Market Data)
```csharp
await auth.AuthenticateAsync();
```

### Xác thực đầy đủ (Có OTP - Dùng cho Trading/Streaming)
```csharp
await auth.AuthenticateAsync("123456");
```

### Yêu cầu gửi mã OTP về điện thoại/email
```csharp
await auth.RequestOtpAsync();
```

### Kiểm tra tự động xác thực và làm mới Token
```csharp
// Kiểm tra token cũ còn hạn không, nếu hết hạn sẽ tự refresh hoặc yêu cầu xác thực lại
await auth.EnsureAuthenticatedAsync();
```

---

## 2. Tài khoản

Sử dụng `AccountService` thông qua `TradingClient.Account` để lấy danh sách tài khoản liên kết:

```csharp
var trading = new TradingClient(auth);
var accounts = await trading.Account.GetAccountInfoAsync();
foreach (var acc in accounts)
{
    Console.WriteLine($"Tài khoản: {acc.AccountNo}, Loại: {acc.AccountType}");
}
```

---

## 3. Dữ liệu thị trường

Sử dụng `MarketDataService` thông qua `DataClient.MarketData`:

### Lấy danh sách OHLC (Nến giao dịch)
```csharp
// Lấy nến 1 phút trong ngày của mã SSI
var ohlcToday = await data.MarketData.GetOhlc1MinuteAsync("SSI");

// Lấy nến 1 ngày lịch sử trong khoảng thời gian cụ thể
var ohlcHistory = await data.MarketData.GetOhlc1DayHistoricalAsync("SSI", "2026/06/01", "2026/06/30");
```

### Lấy danh sách chỉ số (Index) và tóm tắt chỉ số
```csharp
// Danh sách chỉ số
var indexes = await data.MarketData.GetIndexesAsync();

// Tóm tắt chỉ số VN30
var vn30Summary = await data.MarketData.GetIndexSummaryAsync("VN30");
```

### Lấy thông tin mã chứng khoán (Securities Info)
```csharp
var info = await data.MarketData.GetSecuritiesInfoAsync("SSI");
```

---

## 4. Danh mục đầu tư

Sử dụng `PortfolioService` thông qua `TradingClient.Portfolio`:

### Lấy số dư tài khoản
```csharp
var equityBalance = await trading.Portfolio.GetEquityBalanceAsync("0001234567");
var derivativeBalance = await trading.Portfolio.GetDerivativeBalanceAsync("0001234567");
```

### Lấy vị thế nắm giữ (Positions)
```csharp
// Vị thế chứng khoán cơ sở
var positions = await trading.Portfolio.GetEquityPositionsAsync("0001234567");

// Vị thế phái sinh mở
var openDerivatives = await trading.Portfolio.GetOpenDerivativePositionsAsync("0001234567");
```

### Truy vấn lịch sử sổ lệnh (Order History)
```csharp
// Các lệnh đặt trong ngày hôm nay
var todayOrders = await trading.Portfolio.GetTodayOrdersAsync("0001234567");

// Các lệnh đặt lịch sử
var historicalOrders = await trading.Portfolio.GetHistoricalOrdersAsync("0001234567", "2026/06/01", "2026/06/30");
```

---

## 5. Giao dịch

Sử dụng `TradingService` thông qua `TradingClient.Trading`. Các lệnh yêu cầu chữ ký số tự động ký bằng `PrivateKey` đã được cấu hình.

### Đặt lệnh mới
```csharp
// Đặt lệnh giới hạn (LO)
var response = await trading.Trading.PlaceLimitOrderAsync("0001234567", "SSI", OrderSide.Buy, 100, 35000);

// Đặt lệnh thị trường (MTL)
var responseMtl = await trading.Trading.PlaceMarketOrderAsync("0001234567", "SSI", OrderSide.Sell, 100);
```

### Sửa lệnh
```csharp
// Sửa giá lệnh theo Client Request ID
var modifyResponse = await trading.Trading.ModifyOrderPriceAsync("0001234567", "client-req-id-123", 35500);

// Sửa khối lượng theo Order ID trả về từ sàn
var modifyResponse2 = await trading.Trading.ModifyOrderQuantityByOrderIdAsync("0001234567", "order-id-999", 200);
```

### Hủy lệnh
```csharp
var cancelResponse = await trading.Trading.CancelOrderAsync("0001234567", "client-req-id-123");
```

### Kiểm tra sức mua/bán tối đa (Max Buy/Sell)
```csharp
var maxQty = await trading.Trading.GetMaxBuySellAsync("0001234567", "SSI", 35000);
Console.WriteLine($"Khối lượng mua tối đa: {maxQty.MaxBuyQuantity}");
```

---

## 6. Streaming realtime

Sử dụng `StreamClient` để đăng ký lắng nghe dữ liệu thị trường và sự kiện lệnh realtime qua WebSocket.

```csharp
using var stream = new StreamClient(auth);

// Đăng ký callback khi nhận dữ liệu thị trường
stream.Streaming.SetOnData(msg =>
{
    if (msg is TradeMessage trade)
    {
        Console.WriteLine($"[Khớp lệnh] {trade.Symbol}: Giá {trade.Price}, Khối lượng {trade.Quantity}");
    }
    else if (msg is QuoteMessage quote)
    {
        Console.WriteLine($"[Bảng giá] {quote.Symbol}: Giá mua tốt nhất {quote.BidPrices[0]}");
    }
});

// Đăng ký callback sự kiện liên quan tới trạng thái lệnh & tài khoản
stream.Streaming.SetOnTrading(msg =>
{
    if (msg is OrderStatusMessage orderUpdate)
    {
        Console.WriteLine($"[Cập nhật lệnh] Lệnh {orderUpdate.OrderId} trạng thái: {orderUpdate.Status}");
    }
});

// Kết nối
await stream.ConnectAsync();

// Đăng ký nhận thông tin khớp lệnh và bảng giá của SSI, FPT
await stream.Streaming.SubscribeSymbolTradeAsync(new[] { "SSI", "FPT" });
await stream.Streaming.SubscribeSymbolQuoteAsync(new[] { "SSI", "FPT" });

// Đăng ký sự kiện lệnh của tài khoản
await stream.Streaming.SubscribeOrderStatusAsync("0001234567");

// Giữ kết nối hoạt động liên tục
await stream.WaitAsync();
```

---

## 7. Xử lý lỗi

Các ngoại lệ ném ra từ SDK đều kế thừa từ lớp `SsiException`:

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
    Console.WriteLine($"Lỗi xác thực: {ex.Message} (Mã lỗi: {ex.Code})");
}
catch (ApiException ex)
{
    Console.WriteLine($"API SSI trả về lỗi: {ex.Message} (HTTP Status: {ex.StatusCode})");
}
catch (RateLimitException ex)
{
    Console.WriteLine($"Bị giới hạn tần suất request. Vui lòng thử lại sau {ex.RetryAfter} giây.");
}
catch (SsiException ex)
{
    Console.WriteLine($"Lỗi SDK SSI chung: {ex.Message}");
}
```
