# Doctor-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Implementierung der Strategie #15233 „Doctor", konvertiert von MQL zu StockSharp.

## Überblick
Die Strategie kombiniert mehrere klassische Indikatoren zur Erkennung der Trendrichtung und des Momentums:

- **Neigungserkennung** mit einem 40-Perioden Weighted Moving Average zur Bewertung der Trendrichtung.
- **Lineare Lage** über einen 400-Perioden Weighted Moving Average im Vergleich zu den Hochs und Tiefs der letzten drei Kerzen.
- **Momentum**-Bestätigung mit dem Relative Strength Index der Perioden 14 und 5.
- **Trendumkehrfilter** durch den Parabolic SAR.

Eine Long-Position wird eröffnet, wenn alle bullischen Bedingungen erfüllt sind, eine Short-Position bei allen bärischen Bedingungen. Bestehende Positionen werden bei entgegengesetzten Signalen oder beim Erreichen von Schutzlevels geschlossen. Ein optionaler Trailing-Stop verschiebt den Stop Loss vorwärts, sobald die Hälfte des Stop-Abstands erreicht ist.

## Parameter
- `StopLossTicks` – Stop-Loss-Abstand in Ticks.
- `TakeProfitTicks` – Take-Profit-Abstand in Ticks.
- `TrailingStop` – aktiviert die Trailing-Stop-Logik.
- `CandleType` – Zeitrahmen für Kerzen (Standard 30 Minuten).

## Handelsregeln
1. **Kaufen** wenn:
   - Die Neigung von WMA(40) steigt.
   - WMA(400) liegt über den Hochs der letzten drei Kerzen.
   - RSI(14) liegt über 50 und RSI(5) liegt unter RSI(14).
   - Keine offene Long-Position.
2. **Verkaufen** wenn:
   - Die Neigung von WMA(40) fällt.
   - WMA(400) liegt unter den Tiefs der letzten drei Kerzen.
   - RSI(14) liegt unter 50 und RSI(5) liegt über RSI(14).
   - Keine offene Short-Position.
3. **Ausstieg** wenn entgegengesetzte Bedingungen eintreten oder Stop-Loss/Take-Profit-Levels erreicht werden. Der Trailing-Stop aktualisiert das Stop-Level, nachdem sich der Preis um die Hälfte des Stop-Abstands in die gewünschte Richtung bewegt hat.

## Indikatoren
- Weighted Moving Average (40, 400)
- Relative Strength Index (14, 5)
- Parabolic SAR
