# UP3x1 Premium Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The UP3x1 Premium Strategy is a C# port of the MetaTrader expert advisor *up3x1_premium_v2M*. It blends fast/slow EMA crossovers with wide-range candle filters and a daily context filter to capture momentum breakouts while keeping risk managed through fixed targets and trailing stops.

## How It Works

1. **Trend Detection**
   - Calculates two EMAs on the working timeframe (default 12 and 26 periods).
   - Tracks the previous two EMA values to identify bullish or bearish crossovers similar to the MQL logic.
   - Maintains a daily EMA to understand the broader bias.

2. **Entry Logic**
   - **Long setups** trigger when any of the following occurs:
     - The fast EMA crosses above the slow EMA and the previous two candle opens show upward progress.
     - The prior candle forms a bullish wide-range bar whose body exceeds the configured body threshold.
     - At midnight, if the previous daily candle closed notably lower than it opened (capitulation), a bounce signal is allowed.
     - Price trades above the current daily EMA, favouring the long side.
   - **Short setups** trigger when the mirror conditions hold (bearish EMA cross, wide bearish bar, or midnight reversal in the opposite direction).
   - When both long and short triggers fire simultaneously, the strategy follows the prevailing EMA relationship to break the tie.

3. **Exit Management**
   - An open position is closed when:
     - The EMAs converge within ±0.1%, signalling a loss of directional conviction.
     - Price touches the take-profit or stop-loss offsets defined in absolute price units.
     - The trailing stop (if enabled) is pulled behind price and subsequently hit.

4. **Position Handling**
   - Trades are opened only when the strategy is flat, matching the original EA behaviour.
   - Volume is controlled via the `OrderVolume` parameter and applied to every market order.

## Parameters

| Parameter | Description |
|-----------|-------------|
| `OrderVolume` | Order size in lots/contracts for every trade. |
| `FastEmaLength` / `SlowEmaLength` | Periods for the fast and slow EMAs on the working timeframe. |
| `DailyEmaLength` | Period for the EMA computed on the daily candles. |
| `TakeProfit` | Absolute profit target in price units (set to zero to disable). |
| `StopLoss` | Absolute stop distance in price units (set to zero to disable). |
| `TrailingStop` | Trailing distance that follows price once the move exceeds the threshold. |
| `RangeThreshold` | Minimum total range the previous candle must exceed to qualify as a wide bar. |
| `BodyThreshold` | Minimum candle body size that defines bullish/bearish thrust bars. |
| `DailyReversalThreshold` | Size of the previous daily reversal required during the midnight filter. |
| `CandleType` | Working timeframe for the main EMA and price logic. |
| `DailyCandleType` | Higher timeframe used for the daily EMA context. |

## Usage Notes

- Defaults mimic the numeric constants found in the original EA (converted from point values to decimal price offsets).
- Adjust the price-based thresholds (`TakeProfit`, `StopLoss`, `TrailingStop`, range/body thresholds) to match the tick size of the traded instrument.
- The daily EMA filter replaces the unconditional long bias present in the MQL script, keeping trades aligned with the prevailing higher timeframe trend.
- Always backtest on historical data and forward test in a demo environment before enabling live trading.
