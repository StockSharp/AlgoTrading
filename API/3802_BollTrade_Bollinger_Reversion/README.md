# BollTrade Bollinger Reversion Strategy

## Overview

The **BollTrade Bollinger Reversion Strategy** is a high-level StockSharp strategy converted from the classic BollTrade MetaTrader expert advisor. It trades a single instrument using Bollinger Bands and waits for price excursions beyond the bands plus an additional pip buffer. When a candle closes above the upper band the strategy opens a short position, and when a candle closes below the lower band it opens a long position. All decisions are made on finished candles to avoid reacting to incomplete data.

## Trading Logic

1. Subscribe to the configured candle type and calculate Bollinger Bands with the selected period and deviation.
2. Compute an additional price offset expressed in pip units to mimic the original buffer that forced trades deeper into overbought/oversold territory.
3. When the closing price of a completed candle is below the lower band minus the offset, open a long position. When it is above the upper band plus the offset, open a short position.
4. For each opened trade the strategy stores stop-loss and take-profit levels that are defined in pip units. These exits emulate the original expert advisor that closed positions when floating profit or loss crossed predefined pip distances.
5. Positions are closed when the candle range crosses either the stop-loss or take-profit threshold. No additional scaling or pyramiding is performed.

## Money Management

* The `Lots` parameter defines the base position size.
* When `LotIncrease` is enabled the volume scales proportionally with the current portfolio value relative to the value observed at strategy start, up to a safety cap of 500 lots. This reproduces the balance-linked sizing logic from the MetaTrader version.

## Parameters

| Parameter | Description |
|-----------|-------------|
| **Take Profit (pips)** | Distance in pips used to calculate the take-profit level from the entry price. Set to zero to disable the take-profit exit. |
| **Stop Loss (pips)** | Distance in pips used to calculate the stop-loss level from the entry price. Set to zero to disable the stop-loss exit. |
| **Band Offset** | Additional pip distance added beyond the Bollinger Band before opening a trade. |
| **Bollinger Period** | Number of candles used for the Bollinger Bands moving average. |
| **Bollinger Deviation** | Standard deviation multiplier for the Bollinger Bands width. |
| **Base Volume** | Base trade volume in lots. |
| **Scale Volume** | When enabled, increases the order volume based on the growth of portfolio value. |
| **Candle Type** | Candle type (timeframe) used for signal generation. |

## Notes

* The strategy works with finished candles only and therefore needs historical data for warm-up before live trading.
* Stop-loss and take-profit levels are evaluated on candle ranges, which approximates the original tick-based logic while remaining compatible with the high-level API.
* Protective features from the StockSharp framework (`StartProtection`) are enabled to guard against accidental position exposure when the strategy stops unexpectedly.
