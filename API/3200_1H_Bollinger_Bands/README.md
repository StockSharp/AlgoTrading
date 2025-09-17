# 1H Bollinger Bands Strategy

## Overview
The **1H Bollinger Bands Strategy** adapts the MetaTrader expert "1H Bolinger Bands" to the StockSharp high-level API. The idea is to trade bounces from the daily Bollinger Bands while the hourly trend is aligned and the long-term monthly MACD confirms the direction. The strategy works on the H1 timeframe (default) and relies on additional higher-timeframe data streams for confirmation.

## Trading Logic
- **Trend Filter:** Two linear weighted moving averages (LWMA 250 and 500) on the base timeframe ensure that only trades aligned with the dominant direction are allowed.
- **Trigger Pattern:** On the higher timeframe (daily by default), the strategy watches for a candle whose low pierces below the lower Bollinger Band and the next candle opens back above it (reverse for shorts with the upper band). This replicates the original bounce condition.
- **Momentum Confirmation:** Momentum (period 14) is calculated on the higher timeframe. At least one of the three most recent momentum deviations from 100 must exceed the configured threshold (default 0.3).
- **MACD Filter:** A monthly MACD (12/26/9) must agree with the signal. For long trades the MACD line must be above the signal line, for shorts it must be below.
- **Entry:** When all filters align, the strategy opens a market order. If there is an opposite position open, the requested volume neutralizes the existing exposure and flips the direction.

## Position Management
Risk management is implemented directly in the strategy using pip-based distances converted through `Security.PriceStep`:
- **Stop Loss:** Closes the position once price moves against the entry by the configured number of pips.
- **Take Profit:** Locks in profits when price reaches the configured pip target.
- **Trailing Stop (optional):** When enabled and the move exceeds the trailing distance, an internal trailing level follows price. A bar penetrating that level closes the trade.
- **Break-Even (optional):** After price advances by the trigger distance, the stop level is moved to the entry price plus the configured offset (minus for shorts). A pullback to that level exits the position.

Money-based profit management from the original expert is not recreated; the StockSharp version focuses on price-based controls to remain exchange-agnostic.

## Parameters
| Parameter | Description | Default |
|-----------|-------------|---------|
| `CandleType` | Base timeframe for signal evaluation. | 1 hour candles |
| `HigherTimeFrame` | Timeframe used for Bollinger Bands and momentum. | 1 day candles |
| `MacdTimeFrame` | Timeframe for the confirming MACD. | 30-day candles |
| `FastMaPeriod` / `SlowMaPeriod` | Fast/slow LWMA lengths on the base timeframe. | 6 / 85 |
| `TrendFastPeriod` / `TrendSlowPeriod` | Long-term LWMA trend filters. | 250 / 500 |
| `MomentumPeriod` | Momentum lookback on the higher timeframe. | 14 |
| `MomentumThreshold` | Minimum absolute deviation from 100 for momentum. | 0.3 |
| `BollingerPeriod` / `BollingerWidth` | Daily Bollinger Band settings. | 20 / 2.0 |
| `TradeVolume` | Base volume for each new position. | 1 |
| `StopLossPips` / `TakeProfitPips` | Protective stop and target in pips. | 20 / 50 |
| `EnableTrailing` / `TrailingStopPips` | Trailing stop toggle and distance. | true / 40 |
| `EnableBreakEven` / `BreakEvenTriggerPips` / `BreakEvenOffsetPips` | Break-even toggle, trigger distance, and offset. | true / 30 / 30 |

All numeric parameters are exposed through `StrategyParam<T>` and can be optimized in Designer/Runner.

## Implementation Notes
- The strategy subscribes to three candle streams simultaneously: base timeframe, higher timeframe for Bollinger/Momentum, and MACD timeframe.
- Momentum uses the standard StockSharp `Momentum` indicator and stores the last three deviations to mimic the MQL logic.
- Trade volume and pip distances assume that `Security.PriceStep` is correctly populated; otherwise, protective logic will not trigger.
- StockSharp maintains a single net position. The "Max_Trades" scaling behaviour from the original script is simplified to a single aggregated position in this port.
- Equity-based stop outs and money trailing features from the MQL version are intentionally omitted to keep the implementation exchange-neutral.

## Usage
1. Attach the strategy to a security that provides hourly, daily, and monthly candles (or adjust the parameters accordingly).
2. Ensure the security exposes `PriceStep` so pip distances translate into price offsets.
3. Configure the desired volume and risk parameters in the UI or in code before starting the strategy.
4. Start the strategy; it will automatically subscribe to the necessary data, evaluate signals on closed candles, and manage the position with the configured protective rules.

## Known Differences from the MQL Expert
- Money-based trailing and total equity stop are not implemented; only price-based controls are retained.
- Alerts, e-mail, and push notifications from the MQL code are omitted.
- Order stacking is replaced by StockSharp's single net position model.

These adjustments keep the strategy idiomatic for StockSharp while preserving the core trading idea of the original expert.
