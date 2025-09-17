# Currency Strength Strategy

## Overview

The **Currency Strength Strategy** is a StockSharp port of the "Currency Strength" MQL4 expert. The system compares the eight major FX currencies (EUR, GBP, AUD, NZD, USD, CAD, CHF, JPY) by analysing the most liquid twenty-eight currency pairs. The strategy trades the instrument attached to the chart when the base currency is the strongest across the basket and the quote currency is simultaneously the weakest. The conversion follows the high-level API guidelines and replaces the original order-sending logic with StockSharp abstractions.

## Trading Logic

1. Subscribe to candles for the selected trading symbol and for the twenty-seven additional pairs required for currency strength aggregation.
2. For every completed candle of the helper pairs, store the last close and the previous close price and compute the percentage change.
3. Aggregate the percent changes by adding the change to the base currency and subtracting it from the quote currency. The result is a strength score for each currency.
4. Detect the strongest and weakest currencies in the basket.
5. Evaluate technical filters on the trading symbol:
   - Linear Weighted Moving Average (LWMA) crossover (fast above slow for longs, below for shorts).
   - MACD main line positioned on the correct side of the signal line and of the zero level.
   - Momentum absolute value above a configurable threshold.
6. Open a long position when the trading symbol's base currency is the strongest and the quote currency is the weakest while all technical conditions are bullish. Open a short position in the symmetric situation.
7. Close existing positions when the bias or the technical filters no longer support the trade.
8. Periodically log the current strength snapshot to simplify debugging and optimisation sessions.

## Parameters

| Name | Description | Default |
| --- | --- | --- |
| `CandleType` | Time-frame used for all candle subscriptions. | 15 minutes |
| `FastMaLength` | Length of the fast LWMA applied to the typical price. | 6 |
| `SlowMaLength` | Length of the slow LWMA applied to the typical price. | 85 |
| `MomentumLength` | Number of candles used by the momentum indicator. | 14 |
| `MomentumThreshold` | Minimum absolute momentum required before a trade is allowed. | 0.3 |
| `MacdFastLength` | Fast EMA period of the MACD filter. | 12 |
| `MacdSlowLength` | Slow EMA period of the MACD filter. | 26 |
| `MacdSignalLength` | Signal period of the MACD filter. | 9 |

All parameters are configured via `StrategyParam<T>` and support optimisation according to the project rules.

## Alerts and Protection

- The strategy calls `StartProtection()` on start to activate the built-in position protection logic once a position is opened.
- Informational logs report the opening and closing of trades together with the currencies recognised as strongest and weakest.
- A strength snapshot is logged every 30 minutes to monitor how the basket evolves during a test.

## Notes and Limitations

- The strategy requires the data provider to supply candle data for all twenty-eight major FX pairs. When a symbol is missing, a warning is recorded and the missing pair is excluded from the calculation.
- The strength model uses the percent change between consecutive candle closes. This mirrors the gap-based approach of the MQL expert while remaining compatible with the StockSharp candle stream.
- Only market orders are used. Risk management via stop-loss or take-profit levels should be added externally if necessary.

## Conversion Details

- All comments have been rewritten in English according to the repository guidelines.
- Indicator handling uses the high-level `SubscribeCandles().Bind(...)` pattern; no direct indicator lists are manipulated.
- The strategy avoids any direct access to indicator buffers beyond the values provided in the bind callbacks, complying with the conversion requirements.
