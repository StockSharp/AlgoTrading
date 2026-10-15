# CCI Normalized Reversal Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This strategy uses the Commodity Channel Index (CCI) to detect reversals after the indicator exits extreme zones.

## Overview

The indicator is calculated on 8-hour candles with a configurable period. Two threshold levels define overbought and oversold areas. When the CCI crosses back inside these bounds after reaching an extreme, the strategy enters a position in the opposite direction, expecting a mean reversion.

## Trading Rules

- **Long Entry**: Two bars ago the CCI was above the high level and the previous bar moved below it.
- **Short Entry**: Two bars ago the CCI was below the low level and the previous bar moved above it.
- **Close Long**: The previous bar's CCI was below the middle level.
- **Close Short**: The previous bar's CCI was above the middle level.

## Parameters

- `CciPeriod` – lookback period for the CCI.
- `HighLevel` – upper CCI threshold considered overbought.
- `MiddleLevel` – middle threshold used to exit positions.
- `LowLevel` – lower CCI threshold considered oversold.
- `CandleType` – candle series used for calculations (default 8 hours).

## Notes

The strategy opens at most one position at a time and uses market orders. Default risk management is enabled via `StartProtection`.

