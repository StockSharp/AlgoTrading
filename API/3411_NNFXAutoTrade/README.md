# NNFX Auto Trade Strategy

## Overview
The **NNFX Auto Trade Strategy** replicates the risk-sizing and management workflow of the original NNFX MetaTrader 4 panel inside StockSharp. Instead of a graphical interface, the strategy exposes manual commands through parameters. Traders can request long or short entries, instantly flatten exposure, or apply breakeven and trailing logic that mirrors the expert advisor.

Key characteristics:

- ATR-driven volatility sizing with an optional override for manual stop and take-profit distances.
- Position entries are split into two parts: one with a projected target and a runner that is left open for discretionary management.
- Breakeven and trailing commands operate on demand, updating the stored stop levels without automatically firing on every bar.
- Additional capital can be included when computing the monetary risk, matching the MQL script behaviour.

## Trading Logic
1. **ATR collection** – The strategy subscribes to the configured candle type and processes an Average True Range indicator. When `UsePreviousDailyAtr` is enabled it copies the previous day's ATR value during the first 12 hours of the new trading day, imitating the original script.
2. **Risk-based sizing** – On a manual `Buy` or `Sell` command the engine calculates the per-unit monetary risk using the protective stop distance and converts the desired risk percentage into an executable volume.
3. **Position split** – The entry volume is divided into two halves. The first half is liquidated automatically when the projected target is touched, while the second half remains until the trader issues further commands.
4. **Stop handling** – Initial stops are stored internally and evaluated on every finished candle. Manual commands can push the stop to breakeven or advance it according to the NNFX trailing formula.
5. **Exit controls** – `CloseAll` immediately flattens the book, while stop breaches or partial targets trigger market exits that respect the calculated volumes.

## Parameters
| Parameter | Default | Description |
|-----------|---------|-------------|
| `RiskPercent` | `2.0` | Percentage of account equity (plus `AdditionalCapital`) risked per trade. |
| `AdditionalCapital` | `0` | Extra capital added to the equity base when sizing positions. |
| `UseAdvancedTargets` | `false` | Switches risk distances from ATR multiples to manual pip values. |
| `AdvancedStopPips` | `0` | Stop distance in pips when advanced mode is active. |
| `AdvancedTakeProfitPips` | `0` | Target distance in pips for the partial exit when advanced mode is active. |
| `UsePreviousDailyAtr` | `true` | Copies the previous daily ATR during the first 12 hours of a new day. |
| `AtrPeriod` | `14` | ATR lookback length. |
| `AtrStopMultiplier` | `1.5` | Multiplier applied to ATR when computing the stop distance. |
| `AtrTakeProfitMultiplier` | `1.0` | Multiplier applied to ATR when computing the take-profit distance. |
| `CandleType` | `1 Minute` | Candle type used for ATR and price monitoring. |
| `BuyCommand` | `false` | Manual flag – set to `true` to request a long entry. Resets automatically. |
| `SellCommand` | `false` | Manual flag – set to `true` to request a short entry. Resets automatically. |
| `BreakevenCommand` | `false` | Manual flag – move the protective stop to the entry price. Resets automatically. |
| `TrailingCommand` | `false` | Manual flag – apply the NNFX trailing formula once. Resets automatically. |
| `CloseAllCommand` | `false` | Manual flag – close all open positions instantly. Resets automatically. |

## Usage Notes
- The strategy requires a connected portfolio and security with valid `Step`, `StepPrice`, and `VolumeStep` metadata for accurate risk calculations.
- Commands are evaluated on finished candles, so a new bar (or candle update) must be received after toggling a manual parameter.
- When using advanced distances ensure both `AdvancedStopPips` and `AdvancedTakeProfitPips` are populated; otherwise the ATR-based defaults will remain in effect.
