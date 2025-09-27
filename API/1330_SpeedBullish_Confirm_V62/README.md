# SpeedBullish Strategy Confirm V6.2

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
