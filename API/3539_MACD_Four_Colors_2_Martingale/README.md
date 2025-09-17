[Русский](README_ru.md) | [中文](README_cn.md)

# MACD Four Colors 2 Martingale

The strategy ports the "MACD Four Colors 2 Martingale" expert advisor from MetaTrader to StockSharp. It keeps the original logic built around the MACD "color" interpretation and a martingale position sizing model.

## Overview

The underlying indicator paints the MACD histogram with five colors. In the default "new" color scheme the histogram changes color depending on whether the MACD line rises or falls above/below the zero line. The Expert Advisor opens a position whenever the colors transition from silver to yellow (negative MACD turning down again) or from red to blue (positive MACD rolling over). The StockSharp version reproduces this sequence by reconstructing the colors from MACD values.

Only one directional basket of trades is active at any time. A new trade is allowed only if its price improves the average entry of the current basket (lower price for longs, higher price for shorts). Each new entry multiplies the last filled volume by a configurable lot coefficient, implementing the martingale averaging from the original EA.

## Trading rules

- **Indicator logic**: A `MovingAverageConvergenceDivergenceSignal` indicator with the classic 12/26/9 configuration generates MACD values.
- **Color reconstruction**: The strategy compares the latest two MACD values. Rising negative MACD maps to color 1 (silver), rising positive to color 2 (red), falling positive to color 3 (blue), and falling negative to color 4 (yellow).
- **Long entry**: Triggered when the reconstructed colors move from 1 to 4 while the MACD on the previous bar remains below zero. The trade is executed only if there is no short exposure and the new price is lower than any existing long entry.
- **Short entry**: Triggered when the colors move from 2 to 3 while the MACD on the previous bar stays above zero. The trade fires only if there is no long exposure and the new price is higher than any existing short entry.
- **Volume management**: The first order uses `InitialVolume`. Every subsequent order inside the same basket multiplies the last executed volume by `LotCoefficient`. Setting the coefficient ≤ 0 disables the multiplier.
- **Profit and loss control**: Floating PnL is evaluated on every finished candle. Hitting `TargetProfit` closes all positions and resets the martingale cycle. Breaching `MaxDrawdown` (interpreted as a loss threshold) also closes everything and restarts the cycle. Negative thresholds are supported just like in the original code.
- **Position exit**: Apart from the money targets there are no automatic stops. Positions remain open until a risk threshold is met or the user intervenes manually.

## Parameters

- `CandleType` *(DataType, default 1h)* – timeframe for the MACD calculation.
- `InitialVolume` *(decimal, default 1)* – volume of the first order in a basket.
- `LotCoefficient` *(decimal, default 2)* – multiplier applied to the previous volume when martingale is active.
- `MaxDrawdown` *(decimal, default 50)* – floating loss threshold (in money) that forces liquidation. Positive values watch `-MaxDrawdown`, negative values use the exact value.
- `TargetProfit` *(decimal, default 150)* – floating profit target (in money) that closes the basket. Negative values invert the comparison as in the MQL version.
- `FastEmaPeriod` *(int, default 12)* – length of the fast EMA for MACD.
- `SlowEmaPeriod` *(int, default 26)* – length of the slow EMA for MACD.
- `SignalPeriod` *(int, default 9)* – length of the signal EMA for MACD smoothing.

## Usage notes

- Works on any instrument that defines `PriceStep` and `StepPrice`, because unrealized PnL is computed from the exchange specifications.
- The martingale sizing can grow the position quickly. Validate risk limits before enabling trading on a live account.
- For visual analysis attach the chart area created by the strategy. It plots candles, the MACD indicator and executed trades.

## Catalog filters

- **Category**: Trend / Momentum averaging
- **Direction**: Both (long and short baskets)
- **Indicators**: MACD
- **Stops**: Money-based exit only
- **Timeframe**: Configurable (default 1h)
- **Complexity**: Intermediate
- **Risk**: High due to martingale scaling
- **Automation**: Fully automated once started

