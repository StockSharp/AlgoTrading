# Bleris-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die Bleris-Strategie analysiert den Trend der jüngsten Preisextrema, um Trades in Richtung des vorherrschenden Trends zu eröffnen.
Die Preisreihe wird in drei Segmente der Länge `SignalBarSample` aufgeteilt und die höchsten Hochs und niedrigsten Tiefs dieser Segmente werden verglichen.

- **Indikatoren**: Highest, Lowest
- **Parameter**:
  - `SignalBarSample` – Anzahl der Kerzen pro Segment.
  - `CounterTrend` – Handelsrichtung umkehren.
  - `Lots` – Auftragsvolumen.
  - `CandleType` – Zeitrahmen der Kerzen.
  - `AnotherOrderPips` – Mindestabstand in Pips, bevor eine weitere Order desselben Typs eröffnet wird.

## Funktionsweise
1. Highest- und Lowest-Indikatoren berechnen Extrempreise über die letzten `SignalBarSample` Kerzen.
2. Fallende Hochs signalisieren einen Abwärtstrend; steigende Tiefs signalisieren einen Aufwärtstrend.
3. Die Strategie kauft bei einem Aufwärtstrend und verkauft bei einem Abwärtstrend. Mit aktiviertem `CounterTrend` wird die Logik umgekehrt.
4. Neue Orders in gleicher Richtung werden ignoriert, wenn der Preis der letzten Order innerhalb von `AnotherOrderPips` liegt.

Dieses Beispiel verwendet die High-Level-API von StockSharp und ist für Bildungszwecke gedacht.
