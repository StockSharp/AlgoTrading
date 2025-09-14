# GG-RSI-CCI Strategy

This strategy replicates the **GG-RSI-CCI** MetaTrader expert advisor using the StockSharp high-level API.
It combines the Relative Strength Index (RSI) and Commodity Channel Index (CCI) indicators, each smoothed by two moving averages.
A position is opened when both indicators point in the same direction.

## Logic

1. **Indicators**
   - Calculate RSI and CCI with the same period.
   - Smooth each indicator with a fast and a slow moving average.
2. **Signals**
   - **Buy** when the fast RSI is above the slow RSI **and** the fast CCI is above the slow CCI.
   - **Sell** when the fast RSI is below the slow RSI **and** the fast CCI is below the slow CCI.
   - If the mode is set to `Flat`, any neutral state will close the current position.
3. **Risk Management**
   - The strategy calls `StartProtection` once on start. Stop loss or take profit levels can be configured via the platform's risk manager.

## Parameters

| Name            | Description                                  |
|-----------------|----------------------------------------------|
| `CandleType`    | Time frame used for calculations.             |
| `Length`        | RSI and CCI period.                           |
| `FastPeriod`    | Fast smoothing period.                        |
| `SlowPeriod`    | Slow smoothing period.                        |
| `Volume`        | Order volume.                                 |
| `AllowBuyOpen`  | Enable opening long positions.                |
| `AllowSellOpen` | Enable opening short positions.               |
| `AllowBuyClose` | Enable closing short positions.               |
| `AllowSellClose`| Enable closing long positions.                |
| `Mode`          | `Trend` closes only on opposite signals; `Flat` closes also on neutral signals. |

## Notes

The strategy processes only finished candles and uses high-level order helpers (`BuyMarket` / `SellMarket`).
It avoids direct access to indicator buffers and stores state internally.
