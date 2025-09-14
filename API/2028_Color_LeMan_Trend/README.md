# Color LeMan Trend Strategy

This strategy is a port of the original MQL5 expert advisor *ColorLeManTrend*. It uses a custom high/low based trend indicator to identify market direction.

## Idea

The indicator calculates bullish and bearish lines using extreme high and low values over three different lookback periods. Exponential moving averages smooth these values. Trading decisions are based on crossovers of the bullish and bearish lines:

- When the previous bullish line is above the bearish line and the current bullish line drops below the bearish line, a **buy** signal is generated.
- When the previous bullish line is below the bearish line and the current bullish line rises above the bearish line, a **sell** signal is generated.
- Optional flags control whether long or short positions may be opened or closed.

## Parameters

- `CandleType` – timeframe for indicator calculations.
- `Min` – period for the shortest extreme calculation.
- `Midle` – period for the medium extreme calculation.
- `Max` – period for the longest extreme calculation.
- `PeriodEma` – smoothing period for both bullish and bearish lines.
- `StopLossPoints` – protective stop in points.
- `TakeProfitPoints` – take profit in points.
- `AllowBuy` – enable long entries.
- `AllowSell` – enable short entries.
- `AllowBuyClose` – allow closing long positions.
- `AllowSellClose` – allow closing short positions.
- `Volume` – trade volume per order.

## Notes

The strategy processes only finished candles and uses market orders for all operations. Stop loss and take profit values are applied using built-in position protection.
