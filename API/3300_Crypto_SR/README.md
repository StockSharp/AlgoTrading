# Crypto SR Strategy

The Crypto SR strategy ports the MetaTrader 4 expert advisor "Crypto S&R" to the StockSharp high-level API. The implementation keeps the layered confirmation logic of the original system: a trend filter based on linear weighted moving averages (LWMA), a higher timeframe momentum check, a long-term MACD trend filter and fractal-derived support/resistance levels. Orders are submitted with market execution and the position is managed via fixed stop-loss/take-profit levels, break-even adjustments and a trailing stop measured in pips.

## Trading logic

1. **Primary timeframe analysis** – the strategy subscribes to the configured candle series and feeds two LWMAs with the typical candle price `(high + low + close) / 3`. The fast LWMA must stay above (below) the slow LWMA to enable longs (shorts).
2. **Higher timeframe momentum** – a `Momentum` indicator is evaluated on a second candle series. The absolute distance of the latest three momentum readings from the neutral value (100) must exceed the buy/sell thresholds.
3. **Long-term MACD filter** – the strategy listens to another candle stream where a MACD (12, 26, 9) is calculated. Long positions require the MACD line to remain above its signal, short positions need it below the signal. The default long-term timeframe is daily to approximate the monthly series used by the EA; it can be adjusted if real monthly candles are available.
4. **Fractal support/resistance** – finished candles are stored in a rolling buffer. When the classic Bill Williams fractal pattern (two neighbours on each side) appears, the corresponding high/low becomes the active resistance or support level. A configurable pip buffer is applied around the level to emulate the horizontal lines drawn by the original expert.
5. **Entry rules**:
   - *Buy*: no open long position, fast LWMA above slow LWMA, momentum deviation ≥ buy threshold, MACD bullish, the current candle tests the buffered support and closes above the previous close.
   - *Sell*: mirror conditions with the resistance level, momentum sell threshold and MACD bearish confirmation.
6. **Risk management** – every new position receives an initial stop-loss and take-profit in pips. Break-even logic can shift the stop once the move reaches the trigger distance, while an optional trailing stop follows price using the candle highs/lows. Long/short exposure is closed if the MACD filter flips against the trade.

## Implementation notes

- The monthly MACD filter from the MetaTrader version is approximated with a daily series by default because StockSharp does not provide calendar-month candles out of the box. Users can switch to a custom monthly aggregator if their data source supports it.
- Orders are closed with market requests when protection levels are violated. This mirrors the `OrderClose` calls in MQL and avoids relying on exchange-side stop orders.
- All indicator bindings are performed through the high-level subscription API, and no direct calls to `GetValue` are required.

## Parameters

| Name | Description | Default |
| --- | --- | --- |
| `FastMaPeriod` | Length of the fast LWMA on the primary timeframe. | `6` |
| `SlowMaPeriod` | Length of the slow LWMA on the primary timeframe. | `85` |
| `MomentumPeriod` | Momentum period on the higher timeframe. | `14` |
| `MomentumBuyThreshold` | Minimum absolute deviation of momentum from 100 to enable long entries. | `0.3` |
| `MomentumSellThreshold` | Minimum absolute deviation of momentum from 100 to enable short entries. | `0.3` |
| `MacdFastPeriod` | Fast EMA length for the long-term MACD filter. | `12` |
| `MacdSlowPeriod` | Slow EMA length for the long-term MACD filter. | `26` |
| `MacdSignalPeriod` | Signal EMA length for the long-term MACD filter. | `9` |
| `StopLossPips` | Hard stop-loss distance expressed in pips. | `20` |
| `TakeProfitPips` | Fixed take-profit distance expressed in pips. | `50` |
| `TrailingStopPips` | Trailing stop distance in pips (0 disables the trail). | `40` |
| `UseBreakEven` | Whether to move the stop to break-even after a profit trigger. | `true` |
| `BreakEvenTriggerPips` | Profit in pips required before break-even adjustments are applied. | `30` |
| `BreakEvenOffsetPips` | Offset added when moving the stop to break-even. | `30` |
| `FractalWindowLength` | Number of finished candles retained to confirm fractal highs and lows. | `7` |
| `FractalBufferPips` | Additional buffer around fractal levels in pips. | `10` |
| `TradeVolume` | Volume submitted with each market order. | `1` |
| `CandleType` | Primary candle series for LWMA and fractal logic. | `15m` time frame |
| `HigherCandleType` | Higher timeframe for the momentum filter. | `1h` time frame |
| `LongTermCandleType` | Timeframe for the MACD trend filter. | `1d` time frame |

