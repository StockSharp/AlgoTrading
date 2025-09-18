# Triple Top Triple Bottom Strategy

The **Triple Top Triple Bottom Strategy** is a port of the MetaTrader Expert Advisor with the same name. The original system
combines several confirmation layers (trend direction, momentum strength and a MACD filter) before entering the market. This
StockSharp implementation keeps the same core ideas while exposing the important thresholds as strategy parameters.

## Core Logic

1. **Trend filter** – two linear weighted moving averages (LWMA) calculated on the typical price (H+L+C)/3 define the trading
direction. The fast LWMA must be above the slow LWMA to allow long trades and below the slow LWMA to permit short trades.
2. **Momentum confirmation** – the built-in momentum indicator with a configurable lookback length must deviate from the neutral
   100 level by at least the user-defined threshold within the latest three completed candles. The EA required the same behaviour
   by analysing the previous momentum values and we mirror this validation to avoid entries in flat markets.
3. **MACD filter** – a classic 12/26/9 MACD signal line filter prevents fading a strong trend. The strategy only buys when the
   MACD line is above the signal line and sells when it is below.
4. **Risk management** – market orders are protected with both stop-loss and take-profit targets measured in absolute price units.
   The parameters are optional; setting them to zero disables the respective order. The code also closes the position if the
   opposite risk threshold is reached during candle processing.

## Parameters

- **Entry Candle** – `DataType` that defines the timeframe of the working candles.
- **Fast LWMA / Slow LWMA** – lengths of the fast and slow trend filters.
- **Momentum Period / Momentum Threshold** – lookback for the momentum indicator and the minimal deviation from 100 that confirms
  a trade idea.
- **Stop Loss / Take Profit** – protective distances in absolute price units; they are also sent as native protective orders via
  `SetStopLoss` and `SetTakeProfit` so that risk control is enforced even if the strategy session stops.

## Differences vs. MQL Version

- All money-management extras (lot multipliers, equity protection, candle trailing, break-even and manual trend-line checks) were
  omitted because the StockSharp high-level API already offers position sizing utilities and because the graphical objects used in
  the original EA are specific to MetaTrader.
- Risk thresholds are expressed in absolute price units instead of pips. This keeps the implementation broker-neutral; users can
  easily convert their preferred pip distance by multiplying the broker's pip size with the desired number of pips.
- Chart output uses StockSharp areas for the price candles, moving averages, momentum and MACD indicators.

## Usage Notes

1. Attach the strategy to an instrument and configure the desired candle type before starting.
2. Adjust the momentum threshold and the stop distances according to the instrument's volatility.
3. The strategy trades a single net position. When an opposite signal appears the current exposure is closed first, preventing
   overlapping trades.

The code is fully commented in English and follows the StockSharp high-level API guidelines provided in the repository.
