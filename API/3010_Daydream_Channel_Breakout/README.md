# Daydream Channel Breakout
[Русский](README_ru.md) | [中文](README_cn.md)

Daydream Channel Breakout is a direct conversion of the original MetaTrader "Daydream" expert advisor into the StockSharp high-level strategy framework. The logic trades against extreme moves: when price pierces the lower Donchian channel band the algorithm buys expecting a rebound, and when price extends above the upper band it opens short exposure. All exits are handled through a "virtual" take profit expressed in pips, so no native exchange orders remain in the book.

## Strategy Logic

- Build a Donchian channel from the previous `ChannelPeriod` completed candles (the current bar is excluded, matching the MT5 implementation).
- Enter **long** when the closing price drops below the prior lower band. Existing short exposure is flattened implicitly because the order volume adds the absolute position size.
- Enter **short** when the closing price breaks above the prior upper band. Existing long exposure is closed in the same way.
- Only one entry per candle is allowed. After a trade is sent, the strategy waits for the next bar open to generate a new signal.
- Every open position is monitored for a virtual profit target. When unrealized profit exceeds `TakeProfitPips` (converted to price distance through the pip size heuristic) the position is closed at market.

## Parameters

| Name | Description | Default | Notes |
| --- | --- | --- | --- |
| `OrderVolume` | Lot size sent with every new trade. The actual order amount also includes the absolute value of the opposite position to flatten before reversing. | `0.1` | Matches the MT5 default lot size. |
| `TakeProfitPips` | Virtual take profit distance expressed in pips. | `50` | The pip size is derived from `Security.PriceStep`; 3- or 5-digit instruments are automatically multiplied by 10. |
| `ChannelPeriod` | Number of completed candles used to compute the Donchian channel. | `25` | Uses the same lookback as the original EA. |
| `CandleType` | Candle type subscribed for calculations. | `TimeSpan.FromHours(1).TimeFrame()` | Can be changed to any StockSharp candle type. |

## Signal Flow

1. **Data subscription**: the strategy subscribes to the candle type provided via the `CandleType` parameter and binds a Donchian channel indicator using `BindEx`.
2. **Virtual take profit check**: the very first action on each finished candle is to measure the distance between the close price and the average entry price. If the threshold is met, the position is closed and no new entry is evaluated for that bar.
3. **Channel update**: once both upper and lower bands are available, the prior values are cached to mirror the "shift=1" logic from MQL. Signals use the previous band, not the one updated with the current candle.
4. **Entry decision**:
   - Price < previous lower band → buy `OrderVolume + Math.Max(0, -Position)`.
   - Price > previous upper band → sell `OrderVolume + Math.Max(0, Position)`.
5. **Logging & visualization**: informative log messages are produced for every entry and take-profit exit. If a chart area is available in Designer or other StockSharp products, candles, the Donchian channel and trades are drawn automatically.

## Risk Management

- Only a virtual take profit is implemented. No stop-loss or trailing exit exists in the original algorithm, so risk must be controlled externally (for example, with portfolio-level protections).
- Because orders reverse by adding the absolute position, the strategy can pyramid in the same direction if consecutive signals appear across different candles.
- The pip-size helper multiplies the price step by ten for 3- or 5-digit symbols to emulate the MT5 `Point()` to pip conversion. For instruments with unconventional tick sizes you can override the logic or use a custom distance by adjusting `TakeProfitPips`.

## Usage Notes

- The strategy is meant for mean-reversion behavior. It performs best on range-bound markets where overextended moves tend to revert.
- Backtests should include realistic spread and commission settings, because entries occur on market orders after channel breaks.
- Consider coupling the strategy with session filters or volatility-based stops when running on live exchanges.
- The implementation relies exclusively on StockSharp high-level API (no manual indicator collections or historical downloads), so it is compatible with Designer, Shell, and Runner out of the box.
