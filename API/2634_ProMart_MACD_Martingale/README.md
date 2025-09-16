# ProMart MACD Martingale Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy is a StockSharp port of the historical MQL expert **MartGreg_1 / ProMart**. It combines two MACD configurations with a controlled martingale position sizing model. The primary MACD searches for local lows and highs in momentum, while the secondary MACD confirms the direction of the recent slope. After every closed trade the strategy either follows the indicator pattern again (when the last trade was profitable) or immediately flips direction (after a loss) while potentially doubling the next order size.

## Trading Logic

- **Signals**
  - Build two MACD indicators on the selected candle series:
    - `MACD1` (fast=5, slow=20, signal=3) acts as the pattern detector.
    - `MACD2` (fast=10, slow=15, signal=3) confirms the short-term slope.
  - Evaluate signals only on completed candles using the previous three MACD1 values and the previous two MACD2 values (mirroring the MQL logic that looked one bar back).
  - **Long setup**: MACD1 forms a local valley (`MACD1[t-1] > MACD1[t-2] < MACD1[t-3]`) and MACD2 is rising (`MACD2[t-2] > MACD2[t-1]`).
  - **Short setup**: MACD1 forms a local peak while MACD2 is falling.
  - If the most recent closed trade was profitable, the strategy waits for the next valid setup. After a losing trade it opens the opposite direction immediately, regardless of the current MACD shape, replicating the original martingale reversal.
- **Position management**
  - Trades are opened with market orders and monitored on every finished candle.
  - Stop-loss and take-profit levels are calculated in price points from the entry price. If the candle’s high/low reaches either level, the position is closed at market and the trade result is recorded.
  - No new trade is opened on the same candle that closed a position; the strategy waits for the next bar, just like the MQL expert that acted on the first tick of a new bar.
- **Martingale sizing**
  - A base volume is derived from the portfolio equity divided by `BalanceDivider` and aligned to the instrument volume step (falling back to the `Volume` property or the instrument minimum volume when necessary).
  - After a losing trade the next position can double the previous order volume, up to `MaxDoublingCount` consecutive times. Profit resets the doubling counter.
  - Volume is always capped by the instrument maximum volume to avoid oversizing.

## Parameters

| Parameter | Description | Default |
|-----------|-------------|---------|
| `BalanceDivider` | Divider applied to portfolio equity to compute the base order volume. | `1000` |
| `MaxDoublingCount` | Maximum number of consecutive volume doublings after losses. | `1` |
| `StopLossPoints` | Stop-loss distance measured in price points (`PriceStep * StopLossPoints`). | `500` |
| `TakeProfitPoints` | Take-profit distance measured in price points. | `1500` |
| `Macd1Fast` / `Macd1Slow` / `Macd1Signal` | Periods for the primary MACD that detects valleys/peaks. | `5 / 20 / 3` |
| `Macd2Fast` / `Macd2Slow` / `Macd2Signal` | Periods for the secondary MACD slope filter. | `10 / 15 / 3` |
| `CandleType` | Data type of the candle series (default: 1-minute time frame). | `TimeSpan.FromMinutes(1).TimeFrame()` |

## Notes

- The implementation approximates intrabar stop-loss and take-profit fills using candle highs and lows because the StockSharp example operates on finished candles.
- Position volume falls back to the strategy `Volume` or the instrument minimum volume whenever portfolio data is not available.
- No Python version is provided yet; only the C# strategy is included.
- Always validate the configuration on historical data before enabling real trading. The martingale component significantly increases risk.
