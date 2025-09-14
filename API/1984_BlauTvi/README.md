# Blau TVI Strategy

This strategy converts the MQL5 expert `Exp_BlauTVI` into a StockSharp high level strategy. It uses the **Blau True Volume Index (TVI)** to detect reversals in the tick volume flow.

## Idea

The True Volume Index separates up‑ticks and down‑ticks and smooths them with three exponential moving averages. The final value oscillates between -100 and +100 and represents the dominance of buyers or sellers. The strategy opens a long position when the indicator turns upward after a decline and opens a short position when the indicator turns downward after a rise. Existing positions in the opposite direction are closed.

## Parameters

- `Length1` – first smoothing period for up and down ticks.
- `Length2` – second smoothing period.
- `Length3` – final smoothing period applied to the TVI.
- `CandleType` – type of candles used for calculations (default: 4‑hour time frame).
- `Allow Buy Open` – enable opening long positions.
- `Allow Sell Open` – enable opening short positions.
- `Allow Buy Close` – enable closing long positions when a sell signal appears.
- `Allow Sell Close` – enable closing short positions when a buy signal appears.
- `Enable Stop Loss` – use stop‑loss protection in points.
- `Stop Loss` – stop‑loss value in points.
- `Enable Take Profit` – use take‑profit protection in points.
- `Take Profit` – take‑profit value in points.
- `Volume` – order volume in lots.

## Signals

1. **Buy** – when the previous TVI value is lower than the one before it and the current TVI value is greater than the previous value. If enabled, existing short positions are closed.
2. **Sell** – when the previous TVI value is higher than the one before it and the current TVI value is less than the previous value. If enabled, existing long positions are closed.

Only finished candles are processed and all calculations use tick volume of the candle. Stop‑loss and take‑profit are optional and expressed in price points.

## Notes

The strategy uses the high level API: it subscribes to candles, calculates the indicator internally with `ExponentialMovingAverage` instances, and manages positions with `BuyMarket` and `SellMarket` methods. The chart shows the TVI indicator along with trades executed by the strategy.
