# MSL EA Strategy

## Overview

MSL EA is a breakout strategy that builds dynamic support and resistance lines from recent local extremes. The strategy detects short-term fractal highs and lows, adjusts them by a specified distance in ticks, and opens positions when price closes beyond these levels. It was converted from the original MQL4 implementation.

## How It Works

1. The algorithm tracks candle highs and lows to determine local extremes.
2. The highest high and lowest low among the last *Level* detected extremes are stored as resistance and support lines.
3. Each line is shifted by *Distance* ticks to account for market noise.
4. When the close price breaks above the upper line, a long position is opened; when it breaks below the lower line, a short position is opened.
5. The number of simultaneous trades is limited by *Max Trades*.

## Parameters

- **Max Trades** – maximum allowed open positions.
- **Level** – number of local extremes used to build levels.
- **Distance** – offset from extreme in ticks when placing lines.
- **Candle Type** – timeframe of candles processed by the strategy.

## Notes

This C# version uses the high-level StockSharp API and includes English comments. Risk management functions from the original MQL4 helper library are simplified to basic position checks.

