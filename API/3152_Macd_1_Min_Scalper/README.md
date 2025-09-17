# MACD 1 MIN SCALPER Strategy

This strategy is a C# port of the MetaTrader expert advisor **"MACD 1 MIN SCALPER"**. It combines weighted moving averages with multi-timeframe MACD confirmations and a momentum filter before opening trades. The goal is to trade in the direction of the trend when both lower and higher timeframe indicators are aligned and price momentum is sufficiently strong.

## Trading Logic

1. **Base timeframe** – configurable (defaults to M1). Two weighted moving averages (WMA) with periods 50 and 200, calculated on the typical price `(High + Low + Close) / 3`, define the short-term trend.
2. **Higher timeframe trend filter** – WMAs with the same periods are computed on the H1 timeframe. Long setups require both fast WMAs to be above their slow counterparts, shorts require the opposite. If the working timeframe already is H1 the base WMAs are reused.
3. **MACD confirmations** – the MACD (12, 26, 9) must have its main line above the signal line on the base timeframe, the H1 timeframe, and a monthly timeframe (approx. 43200 minutes). Short entries require all three MACDs to be below their signals.
4. **Momentum filter** – a momentum indicator with period 14 operates on a higher timeframe derived from the base MetaTrader period (M1→M15, M5→M30, …). The absolute deviation from 100 must exceed a configurable threshold on at least one of the last three completed bars.
5. **Entry rules** – a long position is opened when all bullish conditions are met and the strategy currently has no long exposure. A short position requires the mirrored bearish conditions. If an opposite position is open, the order size automatically includes the quantity needed to close it.
6. **Risk management** – optional stop-loss and take-profit distances are specified in pips and converted to instrument points at startup. Trailing, breakeven and money-management features from the original script are intentionally omitted in this high-level port.

## Parameters

| Parameter | Description |
| --- | --- |
| `CandleType` | Working timeframe for the base indicators. |
| `OrderVolume` | Volume submitted with every market entry. Also used to close/flip positions. |
| `FastMaPeriod` / `SlowMaPeriod` | Lengths of the fast and slow weighted moving averages. |
| `MacdFastPeriod` / `MacdSlowPeriod` / `MacdSignalPeriod` | EMA periods used by the MACD indicator. |
| `MomentumPeriod` | Momentum indicator length on the confirmation timeframe. |
| `MomentumThreshold` | Minimal absolute deviation from 100 required to accept momentum. |
| `TakeProfitPips` / `StopLossPips` | Optional protective levels specified in pips. |

## Implementation Notes

- The strategy relies on StockSharp's high-level candle subscriptions (`SubscribeCandles`) and indicator binding (`Bind` / `BindEx`). No manual indicator calculations or historical buffers are used.
- The momentum timeframe is derived from the MetaTrader mapping: `[1,5,15,30,60,240,1440,10080,43200]`. If a value falls outside this list, a 4× multiplier of the base timeframe is used as a fallback.
- Protective `StartProtection` is launched only when at least one of the risk parameters is greater than zero. There is no trailing stop implementation in this port.
- Chart rendering is enabled for the base candles, both WMAs and the MACD to ease visual inspection during debugging or live trading.

## Usage Tips

- Set the `OrderVolume` parameter according to the instrument's minimum lot size. The helper automatically adjusts the submitted volume to match the symbol's step and min/max constraints.
- Ensure that higher timeframe data (H1 and monthly) are available in the data feed. Without these candles the strategy will not open positions because the confirmation signals remain incomplete.
- Momentum filtering is sensitive to the chosen threshold. Higher values demand stronger momentum bursts while lower values result in more frequent trades.
