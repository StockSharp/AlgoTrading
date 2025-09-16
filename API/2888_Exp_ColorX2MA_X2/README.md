# Exp ColorX2MA X2 Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The strategy recreates the dual timeframe "Exp_ColorX2MA_X2" expert for StockSharp. It layers two ColorX2MA filters: a higher timeframe trend map and a lower timeframe entry trigger. Both ColorX2MA values are built by cascading two configurable moving averages and then coloring the result according to the current slope. Trading decisions are performed when the lower timeframe color changes in the direction of the higher timeframe trend.

The implementation supports the original applied price options and the most common smoothing modes (SMA, EMA, SMMA, LWMA, Jurik). When the Jurik indicator exposes a `Phase` property it is updated with the configured phase value.

## Trading Rules
- **Long entry**
  - Higher timeframe ColorX2MA color is bullish (trend direction > 0).
  - Lower timeframe ColorX2MA changed from bullish color on the previous bar to a neutral or bearish color on the latest completed bar (`Clr[1] == 1` and `Clr[0] != 1`).
  - Long trading is enabled.
- **Short entry**
  - Higher timeframe ColorX2MA color is bearish (trend direction < 0).
  - Lower timeframe ColorX2MA changed from bearish color on the previous bar to a neutral or bullish color on the latest completed bar (`Clr[1] == 2` and `Clr[0] != 2`).
  - Short trading is enabled.
- **Long exit**
  - When a bearish lower timeframe color appears (`Clr[1] == 2`) and secondary long closing permission is enabled, **or** the higher timeframe trend flips bearish while primary long closing permission is enabled.
- **Short exit**
  - When a bullish lower timeframe color appears (`Clr[1] == 1`) and secondary short closing permission is enabled, **or** the higher timeframe trend flips bullish while primary short closing permission is enabled.
- **Stops**
  - Optional stop loss and take profit distances are specified in points (multiplied by the instrument price step). They are evaluated on every finished signal candle by comparing the candle extremes with the average position price.

## Default Parameters
- **Trend timeframe**: 6-hour candles.
- **Signal timeframe**: 30-minute candles.
- **Trend smoothing**: SMA(12) feeding Jurik(5, phase 15).
- **Signal smoothing**: SMA(12) feeding Jurik(5, phase 15).
- **Applied price**: Close.
- **Signal shift**: 1 bar on both timeframes.
- **Permissions**: long/short entries and exits are enabled.
- **Stop loss**: 1000 points (converted using the price step).
- **Take profit**: 2000 points (converted using the price step).

## Filters & Notes
- Direction: trades both long and short, controlled via permission flags.
- Timeframe: dual timeframe (trend on HTF, entries on LTF).
- Indicators: two-level ColorX2MA with configurable smoothing methods.
- Smoothing support: `Sma`, `Ema`, `Smma`, `Lwma`, `Jurik`. Other modes from the original library are not implemented.
- Applied prices: all 12 original formulas including TrendFollow and Demark prices.
- Stops: optional fixed-distance stop loss and take profit.
- Complexity: intermediate because it synchronizes two timeframes and color buffers.
- Suitable for: trend-following setups on FX, indices or crypto where the ColorX2MA indicator is preferred.

## Usage Tips
- Keep the higher timeframe significantly larger than the signal timeframe to avoid frequent whipsaws.
- Tighten the signal shift parameter (`SignalSignalBar`) to react faster or increase it to smooth more.
- If the instrument does not provide `PriceStep`, the stop/take distances are interpreted directly in price units.
- Jurik smoothing requires a licensed StockSharp indicator package; when unavailable the strategy still runs with the other smoothing options.
