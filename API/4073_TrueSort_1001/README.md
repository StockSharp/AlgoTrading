# TrueSort 1001 Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

TrueSort 1001 is a strict trend-following system that mirrors the original MQL expert advisor. The strategy watches five simple moving averages and only acts when they stay perfectly ordered for three consecutive completed candles. A rising Average Directional Index (ADX) confirms momentum before any trade is opened. Once in the market the position is protected by an adaptive trailing stop measured in price steps and the trade is closed as soon as the moving averages lose their alignment.

## Logic

### Trend and momentum filter
- Five SMAs (10, 20, 50, 100 and 200 periods by default) are calculated on the selected timeframe.
- For long setups the fast SMAs must be strictly above the slower ones on each of the last three finished candles: `SMA10 > SMA20 > SMA50 > SMA100 > SMA200`.
- For short setups the opposite ordering is required on the same three candles.
- ADX with period `AdxPeriod` must stay above `AdxThreshold` and the current value has to be higher than the previous candle, ensuring that trend strength is increasing.

### Entry conditions
1. No position is open.
2. Three historical candles satisfy the ordering rule described above.
3. The ADX filter passes.
4. A market order of `Volume` lots is sent immediately on the close of the current candle.

### Exit conditions
- **Moving average de-synchronization:** when the current candle closes and the MA stack is no longer strictly ordered in the direction of the trade the position is liquidated.
- **Trailing protection:** `StopLossPoints` are converted into absolute price distance by multiplying with the instrument `PriceStep`. For long trades the stop is initialized at the maximum between `SMA100` and `Close - distance`. For shorts it is the minimum between `SMA100` and `Close + distance`. After every candle the stop is tightened towards the price but never loosened. If price crosses the stop the position is closed at market.

### Additional notes
- All decisions are made on finished candles only; unfinished candles are ignored.
- The algorithm stores the last three SMA values internally to replicate the `shift` logic from the original MQL script without requesting indicator history.
- ADX values are processed via `BindEx`, and trading is attempted only when the strategy is online and data is fully formed.

## Parameters
| Name | Default | Description |
| --- | --- | --- |
| `Volume` | `0.1` | Order size in lots for every market entry. |
| `StopLossPoints` | `100` | Trailing-stop distance expressed in instrument price steps. `0` disables trailing. |
| `Sma10Length` | `10` | Period of the fastest SMA. |
| `Sma20Length` | `20` | Period of the second SMA. |
| `Sma50Length` | `50` | Period of the medium SMA. |
| `Sma100Length` | `100` | Period used both for alignment and the initial stop reference. |
| `Sma200Length` | `200` | Slowest SMA confirming long-term trend. |
| `AdxPeriod` | `14` | Period of the ADX indicator. |
| `AdxThreshold` | `25` | Minimum ADX level and rising condition needed before entries. |
| `CandleType` | `TimeSpan.FromHours(1).TimeFrame()` | Candle series used for all indicator calculations. |

## Implementation details
- The code relies on the high-level StockSharp candle subscription and binds six indicators (five SMAs and ADX) in a single pipeline.
- History buffers with length three store the latest SMA values, avoiding calls to `GetValue()` while keeping exact parity with the MQL shifts.
- Trailing stops are managed manually; `StartProtection()` is still activated so the standard infrastructure is ready if further protections are needed.
- Comments inside the code explain each step in English for easier maintenance.
