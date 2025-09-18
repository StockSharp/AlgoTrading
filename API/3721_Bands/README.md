# Bands Strategy

## Overview
This strategy ports the MetaTrader 5 expert advisor **Bands.mq5** to the StockSharp high-level API. It waits for a finished candle
that pierces the Bollinger Bands from the outside back into the channel and only opens a position when the Donchian Channel conf
irms that the band slope has been stable for a configurable number of bars. Average True Range (ATR) multiples reproduce the ori
ginal stop-loss and take-profit distances, while an optional regression tracker prints the equity curve determination coefficient
(R-squared) every 100 trades, mirroring the diagnostic output of the MQL version.

## Trading logic
1. Subscribe to a single candle stream and compute Bollinger Bands, a Donchian Channel and ATR with the same periods as the MetaT
rader robot.
2. When no position is open, inspect the **previous** completed candle:
   - Enter long if that candle opened below the lower Bollinger Band and closed above it, and the Donchian lower band has not decl
ined for more than `ConfirmationPeriod` bars.
   - Enter short if the candle opened above the upper Bollinger Band and closed below it, and the Donchian upper band has not ris
en for more than `ConfirmationPeriod` bars.
3. When a position exists, exit if either the trailing Donchian boundary is crossed (using the previous close) or if the ATR-base
d protective levels are violated intrabar.
4. Every executed trade stores the current portfolio equity and prints the linear-regression R-squared metric after each block of
 100 trades. A negative slope produces a negative R-squared just like the original expert advisor.

## Risk management
- Entry orders are always sent at market with the user-defined `TradeVolume`.
- Protective levels are recreated in code (instead of using pending orders) by comparing candle highs and lows against the ATR mu
tiples.
- When the stop-loss or take-profit triggers, the strategy closes the entire position with a market order and resets the protecti
on levels.

## Parameters
| Parameter | Description |
|-----------|-------------|
| `TradeVolume` | Net volume (in lots) for each market order. |
| `CandleType` | Candle data type / timeframe used for all indicators. |
| `BollingerPeriod` | Number of candles used by the Bollinger Bands. |
| `BollingerDeviation` | Standard deviation multiplier applied to the Bollinger Bands. |
| `DonchianPeriod` | Length of the Donchian Channel used as trend filter. |
| `ConfirmationPeriod` | Minimum count of consecutive bars that must keep the Donchian slope non-decreasing (long) or non-increasing (short). |
| `AtrPeriod` | Period of the Average True Range used for risk management. |
| `StopAtrMultiplier` | ATR multiple that defines the stop-loss distance. |
| `TakeAtrMultiplier` | ATR multiple that defines the take-profit distance. |

## Notes
- The Donchian slope check is implemented as a rolling counter instead of copying indicator buffers, which keeps the StockSharp
version efficient while matching the behaviour of the original EA.
- All comments and diagnostics are provided in English as required by the project guidelines.
- Money-management helpers from the MetaTrader code are not reproduced; the StockSharp implementation relies on the `TradeVolume`
parameter for position sizing.
