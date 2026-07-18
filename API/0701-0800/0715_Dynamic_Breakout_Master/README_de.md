# Dynamische Ausbruch-Meister-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Ausbruchsstrategie mit Donchian-Kanälen, gleitenden Durchschnitts-Trendfilter, RSI- und ATR-Filtern sowie Volumen- und Zeitbeschränkungen.

## Strategieregeln

- Long: Preis bricht über das obere Donchian-Band aus oder zieht nach dem Ausbruch zurück, MA1 > MA2, RSI zwischen `RsiOversold` und `RsiOverbought`, ATR über `AtrMultiplier`, Volumen über Durchschnitt und innerhalb der Handelszeiten.
- Short: Preis bricht unter das untere Donchian-Band aus oder zieht nach dem Ausbruch zurück, MA1 < MA2, RSI zwischen Schwellenwerten, ATR über `AtrMultiplier`, Volumen über Durchschnitt und innerhalb der Handelszeiten.
- Ausstiege: Stop-Loss/Trailing, Take-Profit, RSI-Extremwert oder gleitende Durchschnittskreuzung.

## Parameter

- `DonchianPeriod` – Kanal-Rückblickperiode.
- `Ma1Length`, `Ma1IsEma` – erster gleitender Durchschnitt.
- `Ma2Length`, `Ma2IsEma` – zweiter gleitender Durchschnitt.
- `RsiLength`, `RsiOverbought`, `RsiOversold` – RSI-Filter.
- `AtrLength`, `AtrMultiplier` – Volatilitätsfilter.
- `RiskPerTrade`, `RewardRatio`, `AccountSize` – Positionsgrößenbestimmung.
- `TradingStartHour`, `TradingEndHour` – Handelssitzung.
- `CandleType` – Kerzen-Zeitrahmen.
