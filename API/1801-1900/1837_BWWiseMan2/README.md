# Bill Williams Wise Man 2 Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This strategy implements the second "wise man" pattern from Bill Williams' trading system.
It analyses the Awesome Oscillator (AO) histogram to spot momentum shifts:

- **Buy** when AO is above zero and forms a peak followed by three consecutively lower bars.
- **Sell** when AO is below zero and forms a trough followed by three consecutively higher bars.

Whenever a signal appears the strategy closes the opposite position and opens a new one in the
signal direction. By default four-hour candles are used, but the timeframe can be changed through
a parameter.

No stop-loss or take-profit logic is included; positions are reversed only when an opposite
pattern arises. The strategy also plots candles, the AO indicator and executed trades on a chart
for visual analysis.
