# ErrorEA Strategy

## Overview
The **ErrorEA Strategy** is a StockSharp port of the MetaTrader advisor `errorEA.mq4`. The original expert compared the +DI and -DI components of the Average Directional Index and kept stacking market orders in the detected trend direction while applying a very large safety stop-loss and a tight scalping take-profit. This C# version recreates the same idea with StockSharp's high-level API, adds clear parameter controls, and documents the risk model explicitly.

## Trading logic
1. Subscribe to the configured timeframe (`CandleType`) and feed an `AverageDirectionalIndex` indicator with the incoming candles.
2. Wait until the candle is fully closed and the ADX produces a final value for that bar.
3. Compare the +DI and -DI lines:
   - if +DI > -DI, the strategy treats the market as bullish;
   - if -DI > +DI, the market is considered bearish;
   - equal values generate no new signals.
4. On a bullish signal:
   - flatten an existing short net position (StockSharp uses netting accounts, so opposite hedges are closed);
   - if the number of long scale-in trades is still below `MaxTrades`, send one more market buy order with the volume returned by the risk-control block.
5. On a bearish signal:
   - close an existing long position;
   - if the number of short tranches is below `MaxTrades`, send one market sell order with the same position-sizing logic.
6. Protective orders are managed by `StartProtection`:
   - `StopLossPoints` is converted to price steps and works as a wide fixed stop, just like the `StopLoss` input in MetaTrader;
   - if `EnableTakeProfit` is true, `TakeProfitPoints` replicates the small scalping target that the EA applied through `OrderModify`.
7. Position counters (`_longTrades`/`_shortTrades`) are reset whenever the net position returns to zero or flips to the opposite side, ensuring the scale-in cap is enforced across stop-outs and reversals.

## Risk management and sizing
- `BaseVolume` mirrors the `MiniLots` input from MetaTrader. It acts as the starting lot size for every trade.
- When `EnableRiskControl` is true, the strategy reproduces the original `PowerRisk` formula: `volume = BaseVolume * max(1, PortfolioValue / RiskDivider)`. The default divider (`10000`) matches the MQL implementation.
- After the formula is applied, the result is clamped by `MinVolume`, `MaxVolume`, the exchange limits (`Security.MinVolume`, `Security.MaxVolume`) and the volume step (`Security.VolumeStep`). This prevents the EA from requesting a size that the venue would reject.
- The calculated size is used for every new scale-in order while the corresponding direction stays within the `MaxTrades` cap.

## Parameters
| Name | Type | Default | MetaTrader counterpart | Description |
| --- | --- | --- | --- | --- |
| `AdxPeriod` | `int` | `14` | `iADX(..., 14, ...)` | Smoothing period of the Average Directional Index. |
| `CandleType` | `DataType` | 15-minute time frame | chart timeframe | Candle series used for all calculations. |
| `MaxTrades` | `int` | `9` | `MaxTrades` | Maximum number of scale-in orders per direction. |
| `EnableRiskControl` | `bool` | `true` | `RiskControl` | Enables the dynamic lot calculation based on the portfolio value. |
| `BaseVolume` | `decimal` | `0.15` | `MiniLots` | Base lot size before applying the risk multiplier. |
| `RiskDivider` | `decimal` | `10000` | implicit (divisor in `PowerRisk`) | Divider applied to the portfolio value when risk control is active. |
| `MaxVolume` | `decimal` | `3` | `MaxLot` | Cap for the auto-calculated volume (before exchange rounding). |
| `MinVolume` | `decimal` | `0.01` | `MarketInfo(..., MODE_MINLOT)` | Minimum volume allowed in the final order. |
| `StopLossPoints` | `int` | `1000` | `StopLoss` | Stop-loss distance in price steps. Set to `0` to disable the stop. |
| `EnableTakeProfit` | `bool` | `true` | `ScalpeControl` | Enables the tight scalping take-profit. |
| `TakeProfitPoints` | `int` | `10` | `ScalpeProfit` | Take-profit distance in price steps. |

## Differences from the original expert advisor
- The MetaTrader version contained a bug that overwrote the +DI value with the -DI value. The StockSharp port compares the correct components, reflecting the intended behaviour of the strategy.
- MetaTrader allows hedging. StockSharp operates in a netting environment, so the port closes the opposite exposure before adding new trades in the signal direction.
- Slippage detection (`GetSlippage`) and comment output were removed because StockSharp handles order slippage internally and the risk strings were purely cosmetic.
- Order modifications (`OrderModify`) are replaced with a single `StartProtection` call, which covers both stop-loss and take-profit distances with exchange-aware rounding.

## Usage tips
- Ensure the security has proper `PriceStep`, `VolumeStep`, `MinVolume`, and `MaxVolume` metadata so the built-in volume adjustment can work correctly.
- Align `BaseVolume`, `MinVolume`, and `MaxVolume` with the instrument you trade. The constructor also assigns the adjusted base volume to `Strategy.Volume`, which makes manual actions in the UI consistent with automated orders.
- Increase the timeframe or ADX period when the +DI/-DI signals become too noisy; the scale-in logic performs best during steady trends.
- Disable `EnableTakeProfit` if you prefer to let the stop-loss exit the position instead of scalping small profits.
