# Butterfly Pattern Strategy

## Overview

The **Butterfly Pattern Strategy** converts the original MetaTrader "Cypher EA" harmonic pattern logic to StockSharp's high level API. The strategy scans a configurable candle series for bullish and bearish butterfly formations, validates the harmonic ratios, and opens market positions with three staged take-profit targets. Optional risk management features mirror the MetaTrader expert: break-even locking and trailing stop updates are available after partial exits.

## How it works

1. Candles are buffered until a pivot point can be confirmed using the `PivotLeft`/`PivotRight` window.
2. When five alternating pivots are available, the strategy checks the Fibonacci ratios required for a butterfly pattern.
3. Qualified setups are revalidated (optional) and evaluated by a harmonic quality score (`MinPatternQuality`).
4. Once a pattern is confirmed on a closed candle:
   - A market order is placed using either fixed volume or risk-based sizing.
   - The position volume is split between three take-profit levels (`TP1/TP2/TP3`).
   - A geometric stop-loss is derived from the pattern structure.
5. During the lifetime of the position the strategy monitors candles to trigger partial exits, break-even locking, and trailing adjustments according to the configured thresholds.

> **Tip:** The MetaTrader version works with multiple timeframes simultaneously. To replicate this behaviour in StockSharp, launch several instances of the strategy with different `CandleType` values.

## Key parameters

| Parameter | Description |
| --- | --- |
| `CandleType` | Timeframe used for detecting pivots and patterns. |
| `PivotLeft` / `PivotRight` | Number of candles to the left/right required to confirm a pivot high/low. |
| `Tolerance` | Maximum harmonic ratio deviation allowed when validating the butterfly pattern. |
| `AllowTrading` | Enables or disables order generation after a pattern confirmation. |
| `UseFixedVolume` / `FixedVolume` | Forces a constant trade volume. When disabled, the strategy sizes positions via `RiskPercent`. |
| `RiskPercent` | Percent of portfolio value risked per trade (used only when `UseFixedVolume` is false). |
| `AdjustLotsForTakeProfits` | Normalises the partial volumes to ensure the sum matches the entry size. |
| `Tp1Percent` / `Tp2Percent` / `Tp3Percent` | Distribution of the total volume between the three take-profit levels. |
| `MinPatternQuality` | Minimum harmonic score (0–1) required to accept a detected pattern. |
| `UseSessionFilter`, `SessionStartHour`, `SessionEndHour` | Restrict trading to a specific exchange session window. |
| `RevalidatePattern` | Forces a secondary price check before opening a position. |
| `UseBreakEven`, `BreakEvenAfterTp`, `BreakEvenTrigger`, `BreakEvenProfit` | Controls break-even activation after the specified take-profit level and the additional profit buffer. |
| `UseTrailingStop`, `TrailAfterTp`, `TrailStart`, `TrailStep` | Enables trailing stops once a take-profit level has been reached and the minimum favourable excursion is achieved. |

## Risk management

- Stop-loss, break-even, and trailing levels are managed internally without creating additional orders. Partial exits and stop closes are triggered with market orders to emulate the MetaTrader logic.
- When `UseFixedVolume` is disabled, the position size is calculated from the stop distance, instrument tick value and the `RiskPercent` setting.

## Usage notes

- Ensure the connected instrument supports the configured `CandleType` and price step, otherwise the validation logic may reject signals due to minimum distance checks.
- Break-even and trailing features require the respective take-profit levels to be filled (`BreakEvenAfterTp` and `TrailAfterTp`).
- Multiple strategy instances can run concurrently on different securities or timeframes to reproduce the multi-timeframe scanning of the original EA.
