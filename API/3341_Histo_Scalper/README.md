# Histo Scalper Strategy

## Overview
The **Histo Scalper Strategy** is a C# port of the MetaTrader expert advisor *HistoScalperEA v1.0*. The algorithm fuses eight histogram-style indicators (ADX, ATR, Bollinger Bands, Bulls/Bears Power, CCI, MACD, RSI, and Stochastic) and requires unanimous agreement from all enabled filters before opening a trade. A second requirement is that at least one filter reported the opposite direction on the previous bar, which prevents the strategy from entering during flat markets and mimics the original "two bar" confirmation logic.

## Signal Generation
1. **ADX filter** – checks whether +DI is greater than −DI. Optionally invert the decision.
2. **ATR filter** – compares the current ATR with an SMA baseline and measures the percentage deviation. Long trades require a positive deviation above `AtrPositiveThreshold`; short trades require a negative deviation below `AtrNegativeThreshold`.
3. **Bollinger breakout** – expects the close price to break the upper/lower band.
4. **Bulls/Bears power** – uses Bulls Power for long entries and Bears Power magnitude for short entries.
5. **CCI** – triggers when the CCI value crosses configured oversold/overbought levels.
6. **MACD histogram** – measures the distance between MACD and its signal line.
7. **RSI** – uses classic oversold/overbought zones.
8. **Stochastic** – reads the %K line and compares it to configured bounds.

If any enabled filter produces a neutral value the strategy aborts processing for the current candle. The historical state of each filter is stored to enforce the "previous bar opposite" rule.

## Risk Management
* Market entries use the `TradeVolume` parameter.
* Optional pyramiding adds to open positions; otherwise the strategy only flips direction when the signal changes.
* Take-profit and stop-loss levels are expressed in instrument price steps and are applied immediately after order submission via `SetTakeProfit` and `SetStopLoss`.
* A session filter (`UseTimeFilter`, `SessionStart`, `SessionEnd`) can disable trading outside the configured hours.

## Parameters
| Parameter | Description |
|-----------|-------------|
| `TradeVolume` | Base volume for new trades.
| `AllowPyramiding` | Allows stacking additional trades while already positioned.
| `CloseOnOppositeSignal` | Closes existing positions when the aggregated signal flips.
| `UseTimeFilter`, `SessionStart`, `SessionEnd` | Restricts trading to a custom daily window.
| `UseTakeProfit`, `TakeProfitPoints` | Enables and configures take profit in price steps.
| `UseStopLoss`, `StopLossPoints` | Enables and configures stop loss in price steps.
| `UseIndicator1` … `UseIndicator8` | Enable individual filters.
| `ModeIndicatorX` | Switch between straight and inverted logic for each filter.
| Indicator-specific settings | Periods, thresholds, and levels that replicate the original expert advisor inputs.

## Differences from the MQL Expert
* Basket profit/loss management, sound alerts, and grid order management are intentionally omitted.
* Risk automation (auto lot sizing, break-even and trailing logic) is not included; use the risk parameters above instead.
* Spread checks and broker-specific protections are not ported.

## Usage Notes
1. Set the `Security` and `Portfolio` before starting the strategy.
2. Adjust the candle type (`CandleType`) to match the desired timeframe.
3. Configure indicator thresholds to fit the target instrument’s volatility.
4. Enable or disable filters individually to simplify optimisation.
5. Use `AllowPyramiding` and `CloseOnOppositeSignal` to control exposure during fast markets.
