# 4153 Vortex Oscillator System

## Overview
The strategy reproduces the MetaTrader 4 "Vortex Oscillator System" expert using StockSharp's high level strategy API. It derives a normalized Vortex oscillator by combining the standard Vortex indicator components and reacts whenever momentum escapes a configurable neutral band. The algorithm trades a single symbol and always works with fully closed or reversed positions.

## Trading rules
- A candle subscription defined by **CandleType** feeds a Vortex indicator with period **VortexLength**. The oscillator is calculated as `(VI+ - VI-) / (VI+ + VI-)`, which keeps readings in the `[-1, 1]` range.
- A long setup is armed when the oscillator falls below **BuyThreshold** and, if **UseBuyStopLoss** is enabled, remains above **BuyStopLossLevel**. A short setup is armed when the oscillator rises above **SellThreshold** and, if **UseSellStopLoss** is enabled, remains below **SellStopLossLevel**.
- Whenever the oscillator moves back inside the neutral band bounded by **BuyThreshold** and **SellThreshold**, both setups are cleared so the next break must happen from a neutral state.
- If a long setup is active while the current position is flat or short, the strategy sends a market buy for **Volume** lots plus any quantity required to cover an existing short. Short setups mirror that behaviour by selling **Volume** lots plus the outstanding long quantity.
- Open positions may be closed without an opposite setup: if **UseBuyStopLoss** is enabled and the oscillator touches **BuyStopLossLevel** the long trade is liquidated; **UseBuyTakeProfit** exits a long once the oscillator exceeds **BuyTakeProfitLevel**. Equivalent checks using **SellStopLossLevel** and **SellTakeProfitLevel** manage short positions when their respective toggles are enabled.

## Parameters
- **VortexLength** – number of candles used to compute VI+ and VI- values.
- **CandleType** – timeframe or data type requested from the market data source.
- **Volume** – base order size for new entries; reversal orders automatically add the quantity needed to flatten the current position.
- **BuyThreshold** – oscillator level that arms a long setup once breached.
- **UseBuyStopLoss** – requires the oscillator to stay above **BuyStopLossLevel** before a long entry can be armed.
- **BuyStopLossLevel** – oscillator level that immediately closes a long position when the stop filter is enabled.
- **UseBuyTakeProfit** – toggles the oscillator based take-profit for long trades.
- **BuyTakeProfitLevel** – oscillator level that realizes profits on long positions when the take-profit filter is active.
- **SellThreshold** – oscillator level that arms a short setup once breached.
- **UseSellStopLoss** – requires the oscillator to stay below **SellStopLossLevel** before a short entry can be armed.
- **SellStopLossLevel** – oscillator level that immediately closes a short position when the stop filter is enabled.
- **UseSellTakeProfit** – toggles the oscillator based take-profit for short trades.
- **SellTakeProfitLevel** – oscillator level that realizes profits on short positions when the take-profit filter is active.

## Additional notes
- The strategy draws candles and executed trades on the chart automatically; the internal oscillator logic does not add custom panes.
- Because the oscillator is normalized, the default thresholds `-0.75`, `0.75`, `-1.00`, and `1.00` translate directly from the original expert advisor and can be optimized using StockSharp's parameter system.
- The logic never keeps simultaneous long and short positions; every reversal closes the current exposure first and then opens the opposite side in a single market order.
