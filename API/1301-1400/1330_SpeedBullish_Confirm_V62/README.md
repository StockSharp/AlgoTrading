# SpeedBullish Strategy Confirm V6.2
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Overview
Strategy combining EMA trend filter, MACD histogram crossover and RSI threshold. Optional ATR and volume filters enhance signal quality.

## Entry Conditions
- Price above EMA10 or EMA15 for longs, below for shorts.
- MACD histogram crossing above zero for longs, below zero for shorts.
- RSI greater than or less than the specified level.
- Optional: ATR must exceed its moving average by multiplier.
- Optional: Volume must exceed SMA by multiplier.

## Exit Conditions
- Opposite entry signal.
- Take profit and trailing stop in points.
- Manual stop loss in points.
