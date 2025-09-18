# Vlado Williams %R Threshold Strategy

## Overview
The **Vlado Williams %R Threshold Strategy** is a direct conversion of the MetaTrader 4 expert advisor `Vlado_www_forex-instruments_info.mq4`. The original robot trades a single Williams %R oscillator and flips its market exposure whenever the indicator crosses a user-defined level. This StockSharp port reproduces the same regime-switching behaviour while exposing each tunable value as a strategy parameter for optimisation and UI control.

### Key Concepts
- Trades the direction of the Williams %R oscillator relative to a threshold (default `-50`).
- Holds at most one market position at a time and reverses only after the previous trade is closed.
- Optional risk-based position sizing that mimics the MetaTrader money management formula `AccountFreeMargin * MaximumRisk / price`.
- Works with any candle timeframe through the `CandleType` parameter (default 15-minute bars).

## Trading Logic
1. Subscribe to the configured candle stream and calculate a Williams %R of length `WprLength` (default 100).
2. When Williams %R rises **above** `WprLevel`, the strategy marks a bullish bias:
   - If no position is open and the previous trade was not long, send a market **buy** order.
   - If a short position exists, it is closed immediately; fresh longs are considered on the next candle after the position is flat.
3. When Williams %R falls **below** `WprLevel`, the bias flips to bearish:
   - If no position is open and the previous trade was not short, send a market **sell** order.
   - If a long position exists, it is flattened right away.
4. Position size is determined by `CalculateOrderVolume`:
   - When `UseRiskMoneyManagement` is **true**, the strategy estimates the tradable volume from the current portfolio value: `Portfolio.CurrentValue × MaximumRiskPercent ÷ 100 ÷ ClosePrice`.
   - Otherwise the base `Strategy.Volume` is used.
   - Resulting lots are aligned to the instrument `VolumeStep` and clamped by `MinVolume` / `MaxVolume` if these bounds are available.

The strategy intentionally avoids opening a reversal position in the same candle that triggered the exit, matching the original EA flow (`CheckForClose` runs before `CheckForOpen`).

## Conversion Notes
- Money management defaults follow the MT4 script: `MaximumRiskPercent` starts at `10`, matching the original `MaximumRisk = 10` constant that targeted roughly one mini-lot per trade.
- MetaTrader's `shift` parameter (indicator shift) is always zero in the source file; therefore it was omitted.
- MT4 colour arguments (e.g., `Red`, `Blue`) have no StockSharp equivalent and are ignored.
- Slippage inputs are not required because StockSharp market orders already use the current best price.

## Parameters
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `CandleType` | `DataType` | 15-minute timeframe | Timeframe for both signal calculation and order triggers. |
| `WprLength` | `int` | 100 | Lookback period of the Williams %R oscillator. |
| `WprLevel` | `decimal` | `-50` | Threshold separating bullish and bearish regimes. |
| `UseRiskMoneyManagement` | `bool` | `false` | Toggles risk-based position sizing. |
| `MaximumRiskPercent` | `decimal` | `10` | Percentage of portfolio equity deployed per trade when risk management is on. |

> **Tip:** Combine the strategy with `StartProtection()` or external risk controls if you need automatic stop-loss handling. The original EA also relied on manual supervision and did not define hard stops.

## Usage Guidelines
1. Attach the strategy to a security that exposes accurate `PriceStep`, `StepPrice`, `VolumeStep`, and volume limits so the position-sizing helper can normalise orders correctly.
2. Set `Volume` to your desired fallback lot size. It will be used whenever portfolio equity is unavailable or `UseRiskMoneyManagement` is disabled.
3. Optimise `WprLevel` and `WprLength` to adapt the system to different markets. Narrow levels (e.g., `-20` / `-80`) make the strategy more selective, while wide thresholds (`-50`) ensure it is almost always invested.
4. The strategy is trend-following: it will reverse frequently in ranging conditions. Consider combining it with filters such as higher-timeframe trend checks or volatility thresholds when necessary.

## Differences vs. MetaTrader Version
- Uses candle subscriptions and indicator bindings from the StockSharp high-level API; there is no manual order loop or history scanning.
- Risk sizing relies on `Portfolio.CurrentValue`. When account valuation is missing, the logic falls back to the static `Volume`, matching the MT4 behaviour where `mm=0` forced a fixed lot size.
- All comments and parameter descriptions are in English for consistency with the repository guidelines.

## Validation Checklist
- ✅ Strategy file compiled with the StockSharp strategy template conventions (tabs, file-scoped namespace, XML inheritdoc).
- ✅ Parameters created via `Param()` and marked for optimisation where appropriate.
- ✅ Williams %R values consumed through `Bind`, without any direct `GetValue()` access.
