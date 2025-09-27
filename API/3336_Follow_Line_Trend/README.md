# Follow Line Trend Strategy

## Overview
The Follow Line strategy is a direct port of the MetaTrader expert advisor `FollowLineEA_v1.0`. It replicates the original logic by combining a Bollinger Band breakout detector with an adaptive trend line that hugs price action. The strategy listens to finished candles and works on any timeframe provided by the user.

A breakout above the upper Bollinger band lifts the support line under price, while a close below the lower band drops a resistance line over price. The line slides only in the breakout direction, creating a staircase pattern that highlights sustained trends. Optional ATR padding can widen the line to keep positions from triggering too early. Momentum filters based on moving averages confirm entries depending on the selected arrow mode.

## Trading Logic
1. **Indicator chain**
   - Bollinger Bands (length = `BollingerPeriod`, width = `BollingerDeviations`).
   - Optional ATR (length = `AtrPeriod`) to offset the trend line when `UseAtrFilter` is enabled.
   - A family of simple moving averages (length = `MovingAveragePeriod`) applied to high, low, open, close and median prices. These averages generate confirmation flags when `TypeOfArrows` is set to `OpenCloseMedian` or `HighLowOpenClose`.
2. **Trend line update**
   - A candle closing above the upper band pushes the trend line to the candle low (minus ATR offset if used) but never lowers it.
   - A candle closing below the lower band pulls the line to the candle high (plus ATR offset if used) but never lifts it.
   - The direction of the trend line defines if the market is considered bullish (>0) or bearish (<0).
3. **Entry signals**
   - When the direction flips from bearish to bullish and the arrow filters agree, a buy arrow is queued.
   - When the direction flips from bullish to bearish, a sell arrow is queued.
   - The `IndicatorsShift` parameter delays execution so that the arrow can be processed `IndicatorsShift` bars after it is formed, mimicking the MT4 buffer shift.
4. **Execution filters**
   - Time filter: trades are allowed only between `TimeStartTrade` and `TimeEndTrade` when `UseTimeFilter` is enabled (the window can wrap midnight).
   - Spread filter: if the current spread exceeds `MaxSpread` (measured in price steps), orders are skipped.
   - Order cap: `MaxOrders` limits the absolute position size to replicate the original “maximum orders” check.

## Risk Management
- **Exit on opposite signal**: set `CloseInSignal` to `true` to immediately flatten existing exposure when the opposite arrow fires.
- **Basket locks**: `CloseInProfit` and `CloseInLoss` close the current position once the specified pip target is reached. `UseBasketClose` applies the thresholds to the whole basket instead of separating long and short logic (mirrors the MQL implementation).
- **Stops and targets**: the strategy calls `SetStopLoss`, `SetTakeProfit`, trailing and break-even guards every bar when the corresponding toggles are enabled (`UseStopLoss`, `UseTakeProfit`, `UseTrailingStop`, `UseBreakEven`). All distances are expressed in price steps.
- **Lot sizing**: when `AutoLotSize` is enabled the position size equals the selected share of the current portfolio value (`RiskFactor` percent). Otherwise a fixed `ManualLotSize` is used. The amount is normalized to the instrument volume step and bounded by exchange limits.

## Parameters
| Group | Name | Description |
| --- | --- | --- |
| General | `CandleType` | Timeframe or custom candle type used for subscription. |
| Indicator | `BarsCount` | Historical depth used by the indicator. |
| Indicator | `BollingerPeriod` / `BollingerDeviations` | Bollinger configuration for breakout detection. |
| Indicator | `MovingAveragePeriod` | Length of the moving averages powering arrow filters. |
| Indicator | `AtrPeriod` / `UseAtrFilter` | ATR length and activation flag. |
| Indicator | `TypeOfArrows` | Arrow mode (`HideArrows`, `SimpleArrows`, `OpenCloseMedian`, `HighLowOpenClose`). |
| Indicator | `IndicatorsShift` | Delay (in bars) between arrow formation and execution. |
| Time | `UseTimeFilter`, `TimeStartTrade`, `TimeEndTrade` | Session limits. |
| Filters | `MaxSpread`, `MaxOrders` | Spread ceiling and position limit. |
| Risk | `CloseInSignal`, `UseBasketClose`, `CloseInProfit`, `PipsCloseProfit`, `CloseInLoss`, `PipsCloseLoss` | Basket management rules. |
| Risk | `UseTakeProfit`, `TakeProfit`, `UseStopLoss`, `StopLoss`, `UseTrailingStop`, `TrailingStop`, `TrailingStep`, `UseBreakEven`, `BreakEven`, `BreakEvenAfter` | Protective order suite (values in price steps). |
| Money Management | `AutoLotSize`, `RiskFactor`, `ManualLotSize` | Position sizing. |

## Usage Notes
- The strategy operates on finished candles only. It is therefore safe to backtest with the same bar compression as live trading.
- The custom queue behind `IndicatorsShift` keeps the high-level API behaviour identical to the MT4 indicator buffer access (`iCustom(..., shift)`).
- `TypeOfArrows = HideArrows` disables trading while preserving indicator drawing logic, exactly like the source EA.
- To visualise trades, attach the strategy to a chart area after calling `CreateChartArea()` (already handled in `OnStarted`).

## Conversion Details
- The logic relies exclusively on built-in StockSharp indicators and the high-level candle subscription API (no manual buffering or `GetValue` calls).
- Order management is done with `BuyMarket`/`SellMarket` plus the helper methods `SetStopLoss` and `SetTakeProfit`, mirroring the MT4 behaviour of the original code.
- Portfolio based lot sizing honours exchange limits through `VolumeStep`, `VolumeMin`, and `VolumeMax` checks before sending orders.
- The strategy retains English code comments and parameter descriptions to align with the repository guidelines.
