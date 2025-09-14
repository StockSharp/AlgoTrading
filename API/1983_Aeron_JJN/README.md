# Aeron JJN Breakout Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy replicates the logic of the original Aeron JJN expert advisor. It watches for a strong reversal candle and places a stop order at the open of the last opposite candle. The stop and target are set one ATR away, and an optional trailing stop protects open positions.

Testing shows the idea works best on major Forex pairs using 1-minute candles.

A long stop order is placed when the previous candle is bearish with body larger than **DojiDiff1** and the current candle is bullish but still below the last significant bearish open. A short stop order uses the mirror conditions. Pending orders are removed after **ResetTime** minutes if they remain unfilled.

## Details

- **Entry Criteria**:
  - **Long**: Previous candle bearish, current candle bullish and closes below last bearish open.
  - **Short**: Previous candle bullish, current candle bearish and closes above last bullish open.
- **Long/Short**: Both.
- **Exit Criteria**:
  - ATR-based stop-loss and take-profit.
  - Optional trailing stop in pips.
- **Stops**: Yes, initial stop and target based on ATR plus optional trailing.
- **Filters**:
  - Pending orders expire after the configured time.

## Parameters

- `AtrPeriod` – ATR calculation period.
- `DojiDiff1` – body size threshold for previous candle.
- `DojiDiff2` – body size threshold when searching last opposite candle.
- `TrailSl` – enable trailing stop.
- `TrailPips` – trailing distance in pips.
- `ResetTime` – minutes before canceling stop orders.
- `CandleType` – working timeframe.
