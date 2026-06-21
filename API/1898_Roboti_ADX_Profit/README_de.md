# Roboti ADX Profit-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie konvertiert den ursprünglichen **RobotiADXProfitwining.mq4** Expert Advisor in die StockSharp-API. Sie basiert auf dem Directional Movement Index (DMI), um die Trendrichtung zu bestimmen.

## Handelslogik

- Verwendet den `DirectionalIndex`-Indikator mit einem Standardzeitraum von 14.
- Arbeitet standardmäßig mit Einstunden-Kerzen, der Zeitrahmen kann jedoch geändert werden.
- Öffnet eine **Long**-Position, wenn die `+DI`-Linie die `-DI`-Linie von unten kreuzt und keine Long-Position offen ist.
- Öffnet eine **Short**-Position, wenn die `-DI`-Linie die `+DI`-Linie von unten kreuzt und keine Short-Position offen ist.
- Positionen werden durch einen Trailing Stop geschützt, der als Prozentsatz des Preises ausgedrückt wird.

## Parameter

| Name | Beschreibung | Standard |
| ---- | ----------- | ------- |
| `DmiPeriod` | Zeitraum für die DMI-Berechnung. | 14 |
| `CandleType` | Kerzentyp und Zeitrahmen, der von der Strategie verwendet wird. | 1 Stunde |
| `TrailingStopPercent` | Größe des Trailing Stops in Prozent. | 1% |

## Hinweise

Die Strategie verwendet die High-Level-Binding-API von StockSharp und vermeidet direkte Aufrufe von Indikatorpuffern. Alle Kommentare im Code sind auf Englisch.
