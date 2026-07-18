# X-Alert 3-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie repliziert die Logik des ursprünglichen Experten **X_alert_3.mq4**. Sie überwacht zwei gleitende Durchschnitte mit konfigurierbaren Parametern und erzeugt eine informative Benachrichtigung, wenn ein Crossover auftritt.

## Funktionsweise

1. Zwei gleitende Durchschnitte werden auf jeder abgeschlossenen Kerze berechnet.
2. Eine bullische Benachrichtigung wird erzeugt, wenn:
   - MA1 auf der aktuellen Kerze über MA2 liegt.
   - MA1 auf der vorherigen Kerze über MA2 liegt.
   - MA1 vor zwei Kerzen unter MA2 lag.
3. Eine bärische Benachrichtigung wird erzeugt, wenn:
   - MA1 auf der aktuellen Kerze unter MA2 liegt.
   - MA1 auf der vorherigen Kerze unter MA2 liegt.
   - MA1 vor zwei Kerzen über MA2 lag.
4. Die Strategie öffnet oder schließt **keine** Positionen. Sie schreibt nur Nachrichten ins Protokoll.

## Parameter

| Parameter | Beschreibung | Standard |
|-----------|--------------|----------|
| `Ma1Period` | Periode des ersten gleitenden Durchschnitts. | `1` |
| `Ma1Type` | Typ des ersten gleitenden Durchschnitts (Simple, Exponential, Smoothed, Weighted). | `Simple` |
| `Ma2Period` | Periode des zweiten gleitenden Durchschnitts. | `14` |
| `Ma2Type` | Typ des zweiten gleitenden Durchschnitts. | `Simple` |
| `PriceType` | Quellpreis für Berechnungen (Close, Open, High, Low, Median, Typical, Weighted). | `Median` |
| `CandleType` | Kerzenserie für die Verarbeitung. | `1-Minuten`-Zeitrahmen |

## Hinweise

- Die Implementierung verfolgt die letzten zwei Differenzen zwischen den gleitenden Durchschnitten, um Crossovers zu erkennen, ohne direkt auf historische Indikatorwerte zuzugreifen.
- Benachrichtigungen werden über `AddInfoLog` geschrieben, damit die Strategie nebenwirkungsfrei bleibt.
- Der MetaTrader-Parameter `RunIntervalSeconds` ist in StockSharp nicht erforderlich und wurde weggelassen.
