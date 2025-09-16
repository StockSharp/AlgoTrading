# AMA Trader Strategy

## Overview
The AMA Trader strategy replicates the behaviour of the original MetaTrader 5 expert "AMA Trader". It combines Kaufman's Adaptive Moving Average (AMA) with the Relative Strength Index (RSI) to average into trades against short-term pullbacks while the price remains on the prevailing side of the adaptive trend filter. The StockSharp implementation uses the high-level API with candle subscriptions and indicator binding to stay close to the original logic while remaining fully compatible with the StockSharp execution model.

## Market Assumptions
- **Instrument type**: Designed for spot FX or CFD symbols, but applicable to any trending instrument that supports averaging.
- **Timeframe**: Default one-minute candles, configurable through the `CandleType` parameter.
- **Sessions**: No explicit session handling. Signals are evaluated on every finished candle.

## Indicators
1. **Kaufman Adaptive Moving Average (AMA)**
   - Smooths price action with parameters for the fast and slow smoothing constants (`AmaFastPeriod`, `AmaSlowPeriod`) and the averaging length (`AmaLength`).
   - Defines the primary trend direction. Long trades are considered only when the closing price is above AMA; short trades only when it is below AMA.
2. **Relative Strength Index (RSI)**
   - Evaluated with period `RsiLength` on the candle close.
   - `StepLength` controls how many recent RSI values must confirm an overbought/oversold state. A value of 0 falls back to checking only the latest bar, mirroring the MQL implementation where `StepLength == 0` is treated as 1.
   - `RsiLevelDown` (default 30) and `RsiLevelUp` (default 70) define oversold and overbought thresholds respectively.

## Trading Logic
1. **Bar validation**
   - Trades are evaluated only on finished candles and when the strategy is online and allowed to trade.
2. **Profit management before new entries**
   - If the unrealized profit of all open positions exceeds `ProfitTarget`, the strategy closes every open position and waits for the next signal.
   - If the realized profit since the last reset grows by more than `WithdrawalAmount`, all positions are closed and the realized profit checkpoint is updated. This mimics the original expert's withdrawal mechanic (no actual cash is removed; only the checkpoint is reset).
3. **Long entries**
   - Condition: closing price > AMA and at least one of the inspected RSI values is below `RsiLevelDown`.
   - Action: send a market buy order. If the current long exposure is losing money (negative unrealized PnL based on the tracked average entry price), an additional averaging buy order is submitted.
4. **Short entries**
   - Condition: closing price < AMA and at least one of the inspected RSI values is above `RsiLevelUp`.
   - Action: send a market sell order. If the current short exposure is losing, an additional averaging sell order is submitted.
5. **Position tracking**
   - Executions are processed in `OnOwnTradeReceived`. Separate average prices and volumes are tracked for long and short exposure, enabling accurate unrealized PnL estimates even when the market alternates between buying and selling.

## Risk Management
- **Averaging volume**: Each entry uses the fixed `LotSize`. When losses occur, the algorithm doubles up by adding an extra order in the same direction.
- **Unrealized profit target**: `ProfitTarget` (default 50 monetary units) forces a full exit when floating profits reach the specified level.
- **Realized profit checkpoint**: `WithdrawalAmount` (default 1000) closes all positions once accumulated realized PnL exceeds the threshold, after which the checkpoint resets to the current realized PnL.
- **Manual protection**: No automatic stop-loss or take-profit is configured beyond the unrealized profit target. Users can enable external risk controls if required.

## Parameters
| Parameter | Description |
|-----------|-------------|
| `CandleType` | Candle data type or timeframe for indicator calculations. |
| `LotSize` | Fixed volume for each market order. |
| `RsiLength` | RSI averaging period. |
| `StepLength` | Number of recent RSI values examined (0 falls back to 1). |
| `RsiLevelUp` | RSI overbought threshold for short signals. |
| `RsiLevelDown` | RSI oversold threshold for long signals. |
| `AmaLength` | AMA smoothing period. |
| `AmaFastPeriod` | AMA fast smoothing constant. |
| `AmaSlowPeriod` | AMA slow smoothing constant. |
| `ProfitTarget` | Unrealized profit required to flatten all positions (0 disables the rule). |
| `WithdrawalAmount` | Realized profit increment that triggers a full exit (0 disables the rule). |

## Implementation Notes
- High-level API usage: candles are subscribed through `SubscribeCandles`, and AMA/RSI are bound to the subscription via `.Bind`. The processing delegate receives raw decimal values, avoiding manual indicator value access.
- Position monitoring relies on private accumulators updated inside `OnOwnTradeReceived`. This mirrors the MQL expert's inspection of positions without resorting to prohibited aggregate getters.
- Orders are submitted with `BuyMarket` and `SellMarket`, using the current `LotSize`. Flattening uses explicit volume arguments so that both long and short exposure can be cleared.
- The StockSharp version uses the candle closing price instead of the MetaTrader ask/bid check when evaluating the AMA relation, which is the closest available information within a candle-based workflow.

## Differences from the MetaTrader Expert
- `WithdrawalAmount` updates an internal checkpoint instead of calling `TesterWithdrawal`, because the StockSharp backtester does not support synthetic withdrawals.
- AMA shift and applied price options from the original EA are not exposed. The StockSharp indicators operate on candle close prices.
- Commission and swap are not explicitly added to the unrealized PnL calculation; StockSharp's execution environment handles fees internally when trades settle.

## Usage Tips
- Consider pairing the strategy with portfolio-level risk limits or the built-in protection module if averaging is too aggressive for the traded instrument.
- Optimise AMA and RSI settings per instrument. Lower timeframes often benefit from shorter AMA periods and wider RSI thresholds.
- Monitor drawdowns when `StepLength` > 1, as averaging may fire multiple times during strong counter-trend moves.
