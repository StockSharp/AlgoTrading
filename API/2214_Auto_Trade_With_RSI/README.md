# Auto Trade With RSI Strategy

This strategy averages the last RSI values to generate trading signals. It calculates a standard Relative Strength Index (RSI) over a configurable period and then applies a simple moving average to the RSI itself. Trades are opened when the averaged RSI crosses predefined thresholds and closed when the opposite threshold is reached.

## Trading Logic

1. **RSI Calculation**
   - The indicator uses `RsiPeriod` to compute RSI based on candle close prices.
2. **RSI Averaging**
   - The last `AveragePeriod` RSI values are smoothed by a simple moving average.
3. **Entry Rules**
   - If `BuyEnabled` is `true` and no position is open, a **buy** order is sent when the averaged RSI exceeds `BuyThreshold` (default 55).
   - If `SellEnabled` is `true` and no position is open, a **sell** order is sent when the averaged RSI drops below `SellThreshold` (default 45).
4. **Exit Rules**
   - When `CloseBySignal` is `true`, open positions are closed on opposite signals:
     - Long positions close when averaged RSI falls below `CloseBuyThreshold` (default 47).
     - Short positions close when averaged RSI rises above `CloseSellThreshold` (default 52).

## Parameters

- `BuyEnabled` – enable or disable long entries.
- `SellEnabled` – enable or disable short entries.
- `CloseBySignal` – allow exits on opposite RSI signals.
- `RsiPeriod` – RSI calculation length.
- `AveragePeriod` – number of RSI values used for averaging.
- `BuyThreshold` – averaged RSI value above which a long position is opened.
- `SellThreshold` – averaged RSI value below which a short position is opened.
- `CloseBuyThreshold` – averaged RSI value below which a long position is closed.
- `CloseSellThreshold` – averaged RSI value above which a short position is closed.
- `CandleType` – candle type for subscriptions.

## Notes

This strategy demonstrates how indicator values can be combined through binding in the StockSharp high level API. Trailing stops and money management features from the original MQL version are omitted for simplicity.

