# Para Retrace Strategy

## Overview
The **Para Retrace Strategy** is a C# conversion of the original MetaTrader 4 expert advisor `Para_Retrace.mq4`. It reproduces the idea of using the Parabolic SAR indicator as a dynamic anchor and waiting for price retracements back toward that level before entering the market. The conversion leverages the high-level StockSharp API to manage market data subscriptions, indicator updates, and order execution.

## Trading Logic
1. Calculate the Parabolic SAR value on every finished candle using the configured acceleration step and maximum acceleration.
2. Determine the prevailing trend by checking whether the whole candle is below or above the SAR value:
   - **Bearish context:** if both the candle high and low are below the SAR value.
   - **Bullish context:** otherwise (price is touching or above the SAR level).
3. Derive a trigger price by offsetting the SAR value by a user-defined number of pips:
   - In a bearish context the strategy subtracts the offset, waiting for a retracement upward.
   - In a bullish context the strategy adds the offset, waiting for a pullback downward.
4. Once price touches the trigger (high crosses above for shorts, low crosses below for longs) the strategy opens a market order in the trend direction.
5. Protective stop-loss and take-profit orders are attached automatically using StockSharp's `StartProtection` facility, matching the distances from the original script.

Unlike the original expert advisor, the StockSharp version keeps trading after a position is opened; there is no need to manually reset the offset value. All actions are taken only on completed candles to avoid intrabar repainting issues.

## Indicators
- **Parabolic SAR** – drives both the trend detection and the entry levels.

## Parameters
| Parameter | Description | Default |
|-----------|-------------|---------|
| `SarStep` | Initial acceleration factor for Parabolic SAR. | `0.01` |
| `SarMax` | Maximum acceleration factor for Parabolic SAR. | `0.2` |
| `RetraceOffsetPips` | Distance (in pips) between the SAR value and the entry trigger. | `30` |
| `StopLossPips` | Stop-loss distance in pips (converted to absolute price). Set to `0` to disable. | `30` |
| `TakeProfitPips` | Take-profit distance in pips (converted to absolute price). Set to `0` to disable. | `30` |
| `CandleType` | Timeframe used for candles and indicator calculations. | `5 Minute` |

> **Note:** The strategy estimates the pip size from the security metadata. If the instrument uses five decimal places (typical for Forex), one pip equals ten minimum price steps.

## Order Management
- Orders are placed at market once the retrace condition is satisfied.
- The default trade size is one lot (`Volume = 1`), but this can be adjusted via the base `Strategy.Volume` property before starting the strategy.
- `StartProtection` automatically manages stop-loss and take-profit placements using absolute price offsets derived from the pip settings.

## Usage Tips
- Tune the pip offset, stop, and target to match the volatility of the instrument being traded.
- Consider pairing the strategy with additional filters (time-of-day, volatility, etc.) when integrating into a broader trading framework.
- Always backtest before deploying live, as the profitability strongly depends on market conditions and broker execution.

## Differences vs. Original Script
- Continuous trading without manual global variables.
- Uses completed candles instead of tick-by-tick checks, which provides deterministic behaviour for backtests.
- Integrated risk management through StockSharp's protective order module.

## Disclaimer
This strategy is provided for educational purposes. Test thoroughly on historical and demo data before committing real capital.
