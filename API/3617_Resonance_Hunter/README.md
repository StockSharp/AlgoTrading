# Resonance Hunter Strategy

## Overview
The Resonance Hunter strategy is the StockSharp port of the MetaTrader expert advisor `Exp_ResonanceHunter`. It monitors three correlated currency pairs per slot and looks for synchronous momentum in their Stochastic oscillators. When the oscillators resonate in the same direction the strategy opens a position on the primary symbol while the secondary and confirmation symbols act as filters. The trade is closed as soon as the leading instrument loses momentum or when the configured stop loss is reached.

Three slots are preconfigured:

1. EURUSD traded with EURJPY and USDJPY as confirmations.
2. GBPUSD traded with GBPJPY and USDJPY.
3. AUDUSD traded with AUDJPY and USDJPY.

Each slot can be enabled or disabled independently and can use its own timeframe and indicator parameters.

## Parameters
All parameters are grouped by slot (Slot 1–3). Every group shares the following settings:

| Parameter | Description |
| --- | --- |
| `{Slot} Enabled` | Enables trading for the slot. |
| `{Slot} Primary` | Instrument traded by the strategy and used for exit signals. |
| `{Slot} Secondary` | Second instrument that participates in the resonance check. |
| `{Slot} Confirmation` | Third instrument used in the resonance check. |
| `{Slot} Candle Type` | Timeframe applied to all three instruments (default = 1 hour). |
| `{Slot} K Period` | Stochastic %K lookback. |
| `{Slot} D Period` | Smoothing period for %D. |
| `{Slot} Slowing` | Additional smoothing for %K. |
| `{Slot} Volume` | Order volume in lots. Existing opposite exposure is netted. |
| `{Slot} Stop Loss` | MetaTrader-style stop-loss distance in points. Set to 0 to disable the protective stop. |

## Trading Logic
1. For every configured instrument a `StochasticOscillator` with the selected parameters is calculated on completed candles.
2. Once the latest candles of the three instruments share the same open time, the differences `%K - %D` are evaluated:
   * Positive difference marks an upward impulse (`Up`), negative difference marks a downward impulse (`Down`).
   * Additional consistency rules from the original indicator adjust the impulses by comparing the magnitude of each pair.
3. A **long entry** is generated when all three impulses point upward. A **short entry** appears when all three impulses point downward.
4. Before submitting new orders the strategy closes existing positions if the primary symbol indicates an opposite impulse (mirrors the indicator’s `UpStop`/`DnStop` buffers).
5. After entering a position a protective stop price is calculated using the latest close and the `{Slot} Stop Loss` distance. On every new primary candle the stop is checked and, if breached, the position is closed immediately.

Orders are routed through `BuyMarket`/`SellMarket`. Existing exposure on the primary symbol is netted so that the strategy can reverse directly when required.

## Notes
* The strategy requires synchronized candle data for the three instruments inside each slot. If one symbol lags behind the signal is postponed until the bar timestamps align.
* Stop levels are emulated inside the strategy (no actual stop orders are sent) to match the MetaTrader behaviour.
* Default parameter values reproduce the original expert advisor but can be optimized through the `Param` interface.
