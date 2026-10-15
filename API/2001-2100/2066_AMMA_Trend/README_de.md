# AMMA Trend-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die Strategie verwendet den **Modified Moving Average (AMMA)**-Indikator, um kurzfristige Trendwechsel zu erfassen. Sie analysiert die Richtung der AMMA-Steigung bei den letzten Kerzen und öffnet eine Position in Richtung des aufkommenden Trends, während die entgegengesetzte geschlossen wird.

## Funktionsweise

1. Ein `ModifiedMovingAverage` mit einer konfigurierbaren Periode wird auf dem ausgewählten Zeitrahmen berechnet.
2. Bei jeder abgeschlossenen Kerze vergleicht die Strategie die letzten drei AMMA-Werte.
3. Wenn die Indikatorwerte eine aufsteigende Folge bilden und der neueste Wert größer als der vorherige ist, wird eine Long-Position eröffnet. Jede Short-Position wird geschlossen.
4. Wenn die Indikatorwerte eine absteigende Folge bilden und der neueste Wert kleiner als der vorherige ist, wird eine Short-Position eröffnet. Jede Long-Position wird geschlossen.

## Parameter

- `CandleType` – Zeitrahmen der für Berechnungen verwendeten Kerzen.
- `MaPeriod` – Periode des modifizierten gleitenden Durchschnitts.
- `AllowLongEntry` – Öffnen von Long-Positionen aktivieren.
- `AllowShortEntry` – Öffnen von Short-Positionen aktivieren.
- `AllowLongExit` – Schließen von Long-Positionen aktivieren.
- `AllowShortExit` – Schließen von Short-Positionen aktivieren.

## Hinweise

Die Strategie arbeitet nur auf abgeschlossenen Kerzen und verwendet die integrierten `BuyMarket`- und `SellMarket`-Methoden für die Orderausführung. Risikomanagement-Funktionen können extern über die Standard-`Strategy`-Eigenschaften hinzugefügt werden.
