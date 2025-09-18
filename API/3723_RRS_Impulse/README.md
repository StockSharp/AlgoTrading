# RRS Impulse Strategy

The **RRS Impulse Strategy** is a high level StockSharp port of the MetaTrader expert advisor "RRS Impulse". The original robot
combined RSI, Stochastic and Bollinger Bands filters, rotated between several signal-strength modes and used protective stops and
virtual trailing exits. This C# version keeps the same behaviour but relies purely on the StockSharp high level API: candle
subscriptions feed the indicators, while `BuyMarket`, `SellMarket` and `ClosePosition` execute the orders.

## Trading Logic

1. **Indicator Modes** – Choose between four options:
   - `Rsi`: trade the oscillator when it leaves the overbought/oversold zone.
   - `Stochastic`: require both %K and %D to be above/below the configured levels.
   - `BollingerBands`: react to closes above the upper band or below the lower band.
   - `RsiStochasticBollinger`: fire only when all three filters confirm the same direction.
2. **Trade Direction** – `Trend` follows the indicator (overbought leads to shorts, oversold to longs). `CounterTrend` fades the
   move (overbought triggers longs, oversold triggers shorts).
3. **Signal Strength** – Controls how many timeframes must agree before entering a trade:
   - `SingleTimeFrame`: use only the base timeframe provided by `CandleType`.
   - `MultiTimeFrame`: require alignment across M1, M5, M15, M30, H1 and H4 candles.
   - `Strong`: focus on intraday momentum by checking M1, M5, M15 and M30.
   - `VeryStrong`: demand confirmation from the full M1 … H4 ladder. When the combined indicator mode is enabled every timeframe
     must satisfy *all* three filters.
4. **Risk Management** – Each position tracks the average fill price and monitors three exit conditions:
   - fixed stop-loss distance in pips;
   - fixed take-profit distance in pips;
   - trailing stop activated once profit exceeds `TrailingStartPips` and maintained by `TrailingGapPips`.
   Whenever the direction flips the strategy calls `ClosePosition()` first to flatten and only opens the opposite trade after
   the next confirmation tick.

## Parameters

| Group       | Name | Description |
|-------------|------|-------------|
| Data        | `CandleType` | Base candle series processed for trading decisions. |
| Orders      | `TradeVolume` | Volume used when sending market orders. |
| Risk        | `StopLossPips`, `TakeProfitPips`, `TrailingStartPips`, `TrailingGapPips` | Virtual protective exits expressed in pips. |
| Signals     | `IndicatorMode`, `TradeDirection`, `SignalStrength` | Behaviour switches copied from the MQL input block. |
| RSI         | `RsiPeriod`, `RsiUpperLevel`, `RsiLowerLevel` | RSI configuration for overbought/oversold detection. |
| Stochastic  | `StochasticKPeriod`, `StochasticDPeriod`, `StochasticSlowing`, `StochasticUpperLevel`, `StochasticLowerLevel` | Slow stochastic oscillator settings. |
| Bollinger   | `BollingerPeriod`, `BollingerDeviation` | Bollinger Bands look-back and deviation multiplier. |

All parameters support optimisation ranges identical to the MetaTrader version where it made sense (e.g. stop and take distances
or oscillator thresholds).

## Data Requirements

The strategy needs minute candles for the confirmation ladder. When `SignalStrength` requests additional timeframes the strategy
automatically adds the required subscriptions (`GetWorkingSecurities` advertises them to the engine). Level1 quotes are not used;
only close prices from finished candles drive entries and exits. Protective logic therefore reproduces the "virtual" stop/target
behaviour of the original robot.

## Notes on the Conversion

- Random symbol rotation from the EA was intentionally removed. StockSharp strategies work with a single `Security`, so the
  port concentrates on matching the indicator logic and risk management while leaving instrument rotation to the user.
- Order management is market-based: when the direction changes or a protective condition triggers, `ClosePosition()` is called,
  mirroring the MetaTrader loops that iterated through tickets.
- The conversion keeps all comments in English and uses tabs for indentation to comply with the repository guidelines.
