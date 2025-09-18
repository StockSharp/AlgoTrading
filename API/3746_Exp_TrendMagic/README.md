# Exp TrendMagic Strategy

## Overview
The Exp TrendMagic strategy is a direct port of the MetaTrader 5 expert advisor "Exp_TrendMagic". It monitors the color changes of the TrendMagic indicator, which combines a Commodity Channel Index (CCI) with an Average True Range (ATR) channel. When the indicator switches color, the strategy closes positions from the opposite side and optionally opens a new trade in the direction of the fresh trend.

The conversion keeps the original money management options, configurable signal offset (`Signal Bar`), and the same permission toggles for entering or exiting long and short trades.

## Trading Logic
1. **Indicator inputs**
   - `CCI` (Commodity Channel Index) with configurable period and applied price.
   - `ATR` (Average True Range) with configurable period.
   - The TrendMagic value is computed as:
     - When CCI ≥ 0: `TrendMagic = Low - ATR`, clamped to avoid decreasing the support line.
     - When CCI < 0: `TrendMagic = High + ATR`, clamped to avoid increasing the resistance line.
   - The resulting line color is **0** for bullish (support below price) and **1** for bearish (resistance above price).

2. **Signal evaluation**
   - The strategy stores the indicator colors in chronological order to emulate the MetaTrader buffer and uses the `Signal Bar` offset to read the most recent completed bar.
   - If the previous color (`Signal Bar + 1`) is **0** and the current color (`Signal Bar`) is **1**, the algorithm treats this as a bullish switch: it closes any short position and, if allowed, opens a long trade.
   - If the previous color is **1** and the current color is **0**, the algorithm closes any open long position and, if permitted, enters a short trade.
   - The trade-permission flags (`Allow Buy Entry`, `Allow Sell Entry`, `Allow Buy Exit`, `Allow Sell Exit`) follow the exact semantics of the MT5 version.

3. **Money management**
   - `Money Management` determines how much capital should be allocated per trade. Negative values are interpreted as a fixed lot size.
   - `Margin Mode` selects the interpretation of the money-management value:
     - `FreeMargin` / `Balance`: invest a share of account equity divided by price.
     - `LossFreeMargin` / `LossBalance`: risk a share of capital relative to the stop-loss distance.
     - `Lot`: treat the value as a fixed volume.
   - Volumes are aligned with `VolumeStep`, `MinVolume`, and `MaxVolume` of the selected security.

4. **Risk management**
   - When a new trade is placed, the strategy records the entry price and enforces the original stop-loss and take-profit distances (expressed in points, i.e., multiples of `PriceStep`).
   - Hitting the stop-loss or take-profit immediately closes the position and clears the saved entry price.
   - A throttle prevents reopening a position of the same direction before the next candle opens, reproducing the MQL "time level" guard.

## Parameters
| Parameter | Description |
|-----------|-------------|
| `Money Management` | Fraction of capital used for sizing (negative values become fixed volume). |
| `Margin Mode` | Conversion mode for money management into volume. |
| `Stop Loss` | Protective stop distance in price points. |
| `Take Profit` | Profit target in price points. |
| `Deviation` | Reserved for compatibility with the MT5 input (not used directly). |
| `Allow Buy Entry` / `Allow Sell Entry` | Toggle long/short entries. |
| `Allow Buy Exit` / `Allow Sell Exit` | Toggle closing short/long trades. |
| `Candle Type` | Main timeframe used for indicators and signal evaluation. |
| `CCI Period` / `CCI Price` | CCI length and applied price source. |
| `ATR Period` | ATR length. |
| `Signal Bar` | Index of the finished bar used for signals (0 = current, 1 = previous, etc.). |

## Notes
- The strategy relies on finished candles only (`CandleStates.Finished`) to mimic the MT5 tick-based implementation.
- All indicator values and state variables reset when the strategy is restarted, ensuring deterministic optimisation runs.
- The `Deviation` parameter is provided for full compatibility, even though StockSharp market orders do not use an explicit slippage parameter.
