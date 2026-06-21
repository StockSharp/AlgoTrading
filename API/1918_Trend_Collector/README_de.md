# Trendsammler-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist eine Konvertierung des ursprünglichen MQL-Algorithmus `TrendCollector.mq4`. Sie kombiniert Trenderkennung mit zwei exponentiellen gleitenden Durchschnitten mit Momentum- und Volatilitätsfiltern.

## Strategielogik

- **Schneller EMA vs. Langsamer EMA** – Die Strategie folgt dem Haupttrend durch Vergleich eines schnellen EMA mit einem langsamen EMA.
- **Stochastischer Oszillator** – Bestimmt überkaufte und überverkaufte Bedingungen. Long-Positionen werden eröffnet, wenn der stochastische Wert unter dem unteren Schwellenwert liegt und der schnelle EMA über dem langsamen EMA ist. Short-Positionen werden ausgelöst, wenn der stochastische Wert über dem oberen Schwellenwert liegt und der schnelle EMA unter dem langsamen EMA ist.
- **ATR-Volatilitätsfilter** – Trades erfolgen nur, wenn der aktuelle ATR-Wert unter dem Volatilitätslimit liegt, um hochvolatile Perioden zu vermeiden.
- **Handelsfenster** – Aufträge werden nur zwischen den konfigurierten Start- und Endstunden generiert.

## Parameter

| Name | Beschreibung | Standard |
| --- | --- | --- |
| FastMaLength | Periode des schnellen EMA | 4 |
| SlowMaLength | Periode des langsamen EMA | 204 |
| StochasticPeriod | Periode des stochastischen Oszillators | 14 |
| StochasticUpper | Oberes Niveau für den Stochastik | 80 |
| StochasticLower | Unteres Niveau für den Stochastik | 20 |
| AtrPeriod | Periode für ATR | 14 |
| AtrLimit | Maximaler ATR-Wert für den Handel | 2 |
| StartHour | Startstunde des Handelsfensters | 5 |
| EndHour | Endstunde des Handelsfensters | 24 |
| CandleTimeFrame | Zeitrahmen der verarbeiteten Kerzen | 5 Minuten |

Die Python-Version ist derzeit nicht verfügbar.
