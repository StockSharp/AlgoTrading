# Color XMACD Candle Strategy

This strategy is a StockSharp implementation of the "ColorXMACDCandle" expert advisor. It trades using the MACD indicator and interprets color changes of the histogram or its signal line as entry signals.

## Idea

The strategy analyzes the slope of a MACD component:

- **Histogram mode** – A new histogram bar that rises above the previous bar signals growing bullish momentum. A new bar that falls below the previous bar signals bearish momentum.
- **Signal line mode** – The slope of the MACD signal line is used instead. An upward slope acts as a buy signal, while a downward slope acts as a sell signal.

When the chosen component turns upward and was not rising previously, any short position can be closed and a new long position may be opened. When the component turns downward and was not falling previously, any long position can be closed and a short position may be opened.

The behaviour of opening and closing positions is controlled by separate parameters, allowing the user to enable or disable each action independently.

## Parameters

- `Mode` – Source of signals: `Histogram` or `SignalLine`.
- `FastPeriod` – Fast EMA period for MACD.
- `SlowPeriod` – Slow EMA period for MACD.
- `SignalPeriod` – MACD signal smoothing period.
- `EnableBuyOpen` – Allow opening long positions.
- `EnableSellOpen` – Allow opening short positions.
- `EnableBuyClose` – Allow closing long positions.
- `EnableSellClose` – Allow closing short positions.
- `CandleType` – Candle type for calculations.

## Trading Rules

1. Subscribe to the selected candle series and calculate the MACD indicator.
2. Track the slope of the histogram or signal line depending on the selected mode.
3. When the slope turns upward, close any short position (if allowed) and optionally open a long position.
4. When the slope turns downward, close any long position (if allowed) and optionally open a short position.

The strategy does not include stop loss or take profit mechanisms. Risk management can be added separately if required.
