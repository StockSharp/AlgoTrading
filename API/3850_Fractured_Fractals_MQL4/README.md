# Fractured Fractals (MT4) Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Detailed C# port of the classic MetaTrader 4 expert advisor `MQL/7696/Fractured_fractals.mq4`. The strategy watches for newly confirmed
Williams fractal levels, queues breakout stop orders, and trails risk using the previous fractal swings. Position sizing follows the
original risk-per-trade logic with the adaptive "DecreaseFactor" volume reduction after drawdowns.

## Details

- **Source**: Converted from `MQL/7696/Fractured_fractals.mq4`.
- **Market Regime**: Breakout continuation, works on any instrument that forms reliable fractal structures.
- **Order Types**: Uses stop orders for entries and protective stop orders for exits.
- **Position Sizing**: Percentage risk model controlled by `MaximumRiskPercent` with loss-streak damping via `DecreaseFactor`.
- **Default Parameters**:
  - `MaximumRiskPercent` = 2%
  - `DecreaseFactor` = 3
  - `CandleType` = 1-hour time frame
- **Core Indicators**: Native five-bar Williams fractal detection implemented in the strategy.
- **Strategy Type**: Symmetric long/short breakout with fractal-based trailing stops.

## Strategy Logic

### Fractal detection

- Maintains a rolling window of five candle highs and lows to reproduce MetaTrader's `iFractals` buffers.
- A new up fractal is confirmed when the middle high exceeds the surrounding two highs on each side; a down fractal requires the
  middle low to be the lowest in the five-bar sequence.
- When a fresh fractal appears, it is stored together with the three previous values, mirroring the EA's `cfu`, `pfu`, and
  `pfu.1` style buffers for later comparisons and trailing calculations.

### Entry setup

- Long trades require the most recent up fractal to exceed the previous one and the latest down fractal to define a risk floor.
  The strategy then places a buy stop slightly above the fractal (spread compensation) with a protective stop below the opposing
  down fractal.
- Short trades mirror the logic: a lower low fractal combined with a higher up fractal generates a sell stop and a protective
  stop above the up fractal plus spread.
- Only one pending order per direction is allowed. If fractal structure invalidates the pattern—for example, the latest fractal no
  longer exceeds the previous one—the pending order is cancelled immediately.

### Stop management

- Once positioned, the bot trails the protective stop using the previous fractal on the entry side, subtracting/adding the current
  spread. The stop only moves in the trade's favour.
- When the position direction changes or closes, the unused stop order is cancelled to prevent stale exposure.

### Risk management

- `CalculateOrderVolume` replicates the EA's risk-per-trade calculation: position size is the ratio of monetary risk allowance to
  the distance between entry and stop levels.
- Account valuation prefers `Portfolio.CurrentValue`; if unavailable the routine falls back to the strategy's `Volume` property
  multiplied by price.
- After two or more consecutive losing trades the volume is reduced by `losses / DecreaseFactor`, emulating the MetaTrader
  `DecreaseFactor` behaviour.

### Trade cycle tracking

- `OnOwnTradeReceived` aggregates fills into trade cycles, tracks floating PnL, and updates the loss streak once the volume returns
  to flat. This keeps the risk logic aligned with the MT4 expert where `HistoryTotal` was used to analyse previous outcomes.

## Usage Notes

1. Attach the strategy to any security/portfolio pair and choose an appropriate `CandleType` resolution that matches the original
   EA setup.
2. Ensure level-1 quotes are available—spread estimation relies on best bid/ask; if unavailable the strategy falls back to
   `PriceStep`.
3. The stop orders assume the broker supports server-side stops. Replace `BuyStop`/`SellStop` registration with market orders if
   required by your adapter.
4. Because processing occurs on candle close, intrabar fractal signals are only acted upon at the end of each bar, reproducing the
   expert advisor's bar-by-bar evaluation.
