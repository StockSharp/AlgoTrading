# ADX Crossing Strategy

The **ADX Crossing Strategy** is built around the Average Directional Index (ADX) indicator. It analyzes the crossover of the positive directional index (+DI) and the negative directional index (-DI) to identify potential trend shifts.

When +DI crosses above -DI, the strategy considers it a bullish signal and can open long positions while optionally closing any existing short positions. Conversely, when +DI crosses below -DI, it is treated as a bearish signal, prompting short entries and optional closure of long positions. Optional stop-loss and take-profit levels are supported through built-in risk management.

## Indicator

The strategy uses the `AverageDirectionalIndex` indicator from StockSharp. Only the directional lines are needed; the ADX main value is not used in decision making.

## Parameters

- `ADX Period` – length of the ADX calculation. Default is `50`.
- `Candle Type` – timeframe used for candle subscription. Default is `1 hour`.
- `Allow Buy Open` – enable opening long positions. Default is `true`.
- `Allow Sell Open` – enable opening short positions. Default is `true`.
- `Allow Buy Close` – allow closing long positions on sell signal. Default is `true`.
- `Allow Sell Close` – allow closing short positions on buy signal. Default is `true`.
- `Stop Loss` – stop-loss distance in absolute price units. Default is `1000`.
- `Take Profit` – take-profit distance in absolute price units. Default is `2000`.

## Trading Logic

1. Subscribe to candles of the selected timeframe and compute the ADX indicator.
2. Track the previous values of +DI and -DI to detect crossovers.
3. On a bullish crossover (+DI crosses above -DI):
   - Close short position if `Allow Sell Close` is enabled.
   - Open long position if `Allow Buy Open` is enabled.
4. On a bearish crossover (+DI crosses below -DI):
   - Close long position if `Allow Buy Close` is enabled.
   - Open short position if `Allow Sell Open` is enabled.
5. Protective stop-loss and take-profit levels are applied using `StartProtection`.

## Notes

- Only completed candles (`CandleStates.Finished`) are processed.
- The strategy relies on built-in StockSharp risk management for stop levels.
- Positions are closed by sending an opposite market order with the current volume.

This strategy is intended for educational purposes and may require further tuning before being used on live markets.
