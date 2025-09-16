# BykovTrend + ColorX2MA Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy combines the BykovTrend V2 color trend indicator with the ColorX2MA double-smoothed moving average slope filter. Both logic blocks operate on the same symbol and can issue orders independently, which allows the net position to reflect the latest agreement between the two signal sources.

## Overview

- **Market bias**: Works on any instrument that supports candlestick data. The default timeframe for both blocks is 4 hours (H4), mirroring the original Expert Advisor.
- **Indicators**:
  - *BykovTrend V2*: Uses Williams %R to color candles according to the prevailing trend.
  - *ColorX2MA*: Applies two consecutive moving averages to a configurable price source and classifies the slope direction.
- **Signals**: Entries and exits are generated separately by each block. The final position is the sum of all executed trades.

## BykovTrend Block

1. Williams %R is calculated using the configured period (default 9).
2. Thresholds are shifted by `33 - Risk`. When %R rises above `-Risk` the local trend turns bullish; when it drops below `-100 + (33 - Risk)` the trend becomes bearish.
3. Candle colors:
   - Green/teal (codes 0, 1): bullish trend.
   - Gray (code 2): neutral, no trend change.
   - Chocolate/gold (codes 3, 4): bearish trend.
4. Signals are evaluated on the candle that is `SignalBar` steps behind the most recent closed bar. A value of 1 means the previous completed candle, which matches the MetaTrader implementation.
5. Trading logic:
   - **Long entry**: Current color < 2 (bullish) and previous color > 1 (was neutral/bearish). Optional via *Bykov Allow Long Entries*.
   - **Short exit**: Current color < 2. Optional via *Bykov Allow Short Exits*.
   - **Short entry**: Current color > 2 (bearish) and previous color < 3 (was neutral/bullish). Optional via *Bykov Allow Short Entries*.
   - **Long exit**: Current color > 2. Optional via *Bykov Allow Long Exits*.

## ColorX2MA Block

1. A first moving average smooths the selected applied price (close by default) using the chosen method and length.
2. A second moving average smooths the first MA output, again with a configurable method and length.
3. The slope of the second smoothing defines the color stream:
   - 1 (magenta): value increased since the previous candle.
   - 2 (violet): value decreased.
   - 0 (gray): unchanged.
4. Signals are evaluated on the candle that is `SignalBar` steps behind the latest close.
5. Trading logic:
   - **Long entry**: Current color = 1 and previous color ≠ 1. Optional via *Color Allow Long Entries*.
   - **Short exit**: Current color = 1. Optional via *Color Allow Short Exits*.
   - **Short entry**: Current color = 2 and previous color ≠ 2. Optional via *Color Allow Short Entries*.
   - **Long exit**: Current color = 2. Optional via *Color Allow Long Exits*.

## Position Management

- Orders are market orders. When flipping direction the strategy buys/sells enough contracts to neutralize the existing position and establish a new one of size `Volume`.
- Each block can trigger an exit even if the other block still favors the current side; the net effect is a gradual tug-of-war between the two modules.
- No automatic stop-loss or take-profit is applied. Risk management should be handled externally or by tuning the permission flags.

## Parameters

| Parameter | Description |
|-----------|-------------|
| **BykovTrend Candle** | Data type (timeframe) for the BykovTrend calculation. |
| **Williams %R Period** | Lookback for Williams %R. |
| **Risk Offset** | Shifts the Williams %R thresholds (`33 - Risk`). Larger values tighten bullish thresholds and loosen bearish ones. |
| **Signal Bar** | Delay (number of completed candles) before acting on a BykovTrend color. |
| **Allow Long/Short Entries** | Enable or disable BykovTrend-driven entries. |
| **Allow Long/Short Exits** | Enable or disable BykovTrend-driven exits. |
| **ColorX2MA Candle** | Data type (timeframe) for the ColorX2MA block. |
| **First/Second MA Method** | Smoothing method for each stage (SMA, EMA, SMMA, LWMA, Jurik). |
| **First/Second MA Length** | Period length for each smoothing stage. |
| **First/Second MA Phase** | Compatibility parameter retained from the original EA; current implementation keeps it for documentation but Jurik smoothing uses its internal defaults. |
| **Applied Price** | Price source for ColorX2MA (close, open, high, low, median, typical, weighted, simple, quarted, trend-follow variations, DeMark). |
| **Color Signal Bar** | Delay before acting on ColorX2MA colors. |
| **Allow Long/Short Entries/Exits** | Enable or disable ColorX2MA-driven actions. |

## Notes and Limitations

- Only the moving-average types available in StockSharp are supported. Exotic smoothings from the MetaTrader library (JurX, Parabolic, T3, VIDYA, AMA) are not reproduced; choose among SMA, EMA, SMMA, LWMA, or Jurik.
- Phase parameters are preserved for reference but do not alter the built-in StockSharp indicators.
- The strategy assumes the `Volume` property is configured; otherwise entries will not place orders.
- Because both modules can trade independently, the resulting order flow may differ from MetaTrader installations that segregate trades by magic numbers.
