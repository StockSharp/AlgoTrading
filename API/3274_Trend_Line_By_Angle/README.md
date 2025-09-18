# Trend Line By Angle Strategy

## Overview

The strategy is a StockSharp port of the MetaTrader expert advisor *Trend Line By Angle*. The original robot mixed manual button entries with extensive money-management tools. This port converts the discretionary workflow into an automated MACD trend-following system while preserving the protective logic:

- Monthly MACD (12/26/9) calculated on the configured signal candle type defines the direction. Bullish crosses open long exposure, bearish crosses open short exposure.
- Entries scale in up to the configured number of blocks, mirroring the repeated manual clicks in the source EA.
- Bollinger Bands (20, 2) watch the execution timeframe. Touching the upper band liquidates long exposure; touching the lower band liquidates shorts, replicating the visual stop buttons from MetaTrader.
- Classic risk controls — stop-loss, take-profit, trailing stop, break-even move — operate on pip distances converted through the instrument `PriceStep`.
- Account-level protection closes all orders when either a money or percentage profit target is reached. An additional money-based trailing lock follows floating profit and exits on the configured drawdown.

## Execution Flow

1. **Indicator preparation** – `MovingAverageConvergenceDivergenceSignal` runs on the `SignalCandleType`, while `BollingerBands` run on the trading `CandleType`.
2. **Entry signals** – on each finished execution candle the latest MACD cross is evaluated. A cross up triggers `BuyMarket`, a cross down triggers `SellMarket`. Existing opposite exposure is closed before reversing.
3. **Scaling logic** – the strategy keeps buying/selling until the aggregate position reaches `TradeVolume * MaxEntries`.
4. **Risk management** – break-even, trailing stop, stop-loss, and take-profit levels are recalculated on every candle. A Bollinger touch forces an exit even if other levels are not hit.
5. **Account protection** – money and percent take-profit checks run before generating new signals. The money trailing module tracks the highest total PnL and closes everything once the drop exceeds `MoneyTrailStop`.

## Money Management Details

- **Total PnL** is the sum of realized profit (`PnL`) and the floating PnL computed from the candle close price, price step, and step value.
- **Break-even** moves the protective stop to `Entry + BreakEvenOffsetPips` (long) or `Entry - BreakEvenOffsetPips` (short) once the move exceeds `BreakEvenTriggerPips`.
- **Trailing stop** shifts closer to price whenever the gain exceeds `TrailingStopPips`. Long trailing levels only increase; short trailing levels only decrease.
- **Money trail** activates after `MoneyTrailTrigger` profit is seen. From then on the highest profit is memorized; losing more than `MoneyTrailStop` from that peak closes all positions.

## Parameters

| Parameter | Description |
| --- | --- |
| `TradeVolume` | Volume of each entry block. |
| `MaxEntries` | Maximum number of volume blocks to accumulate. |
| `StopLossPips` | Stop-loss distance in pips. |
| `TakeProfitPips` | Take-profit distance in pips. |
| `TrailingStopPips` | Trailing distance in pips. |
| `UseBreakEven` | Enables the break-even stop move. |
| `BreakEvenTriggerPips` | Profit required before break-even activates. |
| `BreakEvenOffsetPips` | Extra pips added when moving to break-even. |
| `UseBollingerExit` | Enables exits on Bollinger band touches. |
| `BollingerPeriod` / `BollingerDeviation` | Bollinger Bands settings. |
| `UseProfitMoneyTarget` / `ProfitMoneyTarget` | Absolute profit target switch and value. |
| `UseProfitPercentTarget` / `ProfitPercentTarget` | Percentage profit target switch and value. |
| `EnableMoneyTrail` | Enables the money trailing stop. |
| `MoneyTrailTrigger` | Profit required before the money trail becomes active. |
| `MoneyTrailStop` | Allowed drawdown from the peak before exiting. |
| `MacdFastPeriod`, `MacdSlowPeriod`, `MacdSignalPeriod` | MACD configuration. |
| `CandleType` | Execution timeframe. |
| `SignalCandleType` | Timeframe used for the MACD signal. |

## Usage Notes

- The strategy relies on correct `PriceStep` and `StepPrice` values from the instrument. Configure the security before launching.
- If the account does not report portfolio value (`Portfolio.CurrentValue` or `Portfolio.BeginValue`), the percent take-profit is automatically ignored.
- All comments inside the C# file document the trading logic in English to simplify further maintenance.
