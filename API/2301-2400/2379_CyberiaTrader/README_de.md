# CyberiaTrader-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist ein vereinfachter StockSharp-Port des ursprünglichen **CyberiaTrader.mq5**-Systems. Sie kombiniert mehrere klassische technische Indikatoren, um die Marktrichtung zu bewerten und Trades zu eröffnen, wenn die meisten Filter übereinstimmen.

## Indikatoren

- **MACD** – Erkennt Momentum-Wechsel mittels schneller/langsamer EMAs und einer Signallinie.
- **Simple Moving Average** – Bestimmt den vorherrschenden Trend.
- **Commodity Channel Index** – Filtert überkaufte/überverkaufte Bedingungen.
- **Average Directional Index** – Bestätigt die Richtungsstärke über +DI- und -DI-Komponenten.

## Parameter

| Name | Beschreibung |
| --- | --- |
| `MacdFast` | Schnelle EMA-Periode für MACD. |
| `MacdSlow` | Langsame EMA-Periode für MACD. |
| `MacdSignal` | Signallinien-Periode für MACD. |
| `MaPeriod` | Länge des gleitenden Durchschnitts-Trendfilters. |
| `CciPeriod` | Periode des Commodity Channel Index. |
| `AdxPeriod` | Periode des Average Directional Index. |
| `EnableMacd` | MACD-Filter aktivieren/deaktivieren. |
| `EnableMa` | Filter für gleitenden Durchschnitt aktivieren/deaktivieren. |
| `EnableCci` | CCI-Filter aktivieren/deaktivieren. |
| `EnableAdx` | ADX-Filter aktivieren/deaktivieren. |
| `CandleType` | Zeitrahmen der Eingangskerzen. |

## Handelslogik

1. Werte für alle aktivierten Indikatoren werden bei jeder abgeschlossenen Kerze berechnet.
2. Filter können Käufe oder Verkäufe nach ihren jeweiligen Regeln blockieren:
   - MACD über seiner Signallinie blockiert Short-Einstiege; darunter blockiert Long-Einstiege.
   - Preis über dem gleitenden Durchschnitt blockiert Shorts; darunter blockiert Longs.
   - CCI über +100 blockiert Longs; unter -100 blockiert Shorts.
   - +DI größer als -DI blockiert Shorts; -DI größer als +DI blockiert Longs.
3. Ein Trade wird nur eröffnet, wenn eine Seite erlaubt und die andere blockiert ist.
4. Der grundlegende Positionsschutz verwendet 2% Take-Profit und 1% Stop-Loss.

## Hinweise

Diese Übersetzung konzentriert sich auf die wesentlichen Richtungsfilter des ursprünglichen Algorithmus. Die umfangreiche Wahrscheinlichkeitsanalyse und die Hilfsmodule der MQL5-Version werden für mehr Übersichtlichkeit bewusst weggelassen.
