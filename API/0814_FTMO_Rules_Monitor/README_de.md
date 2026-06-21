# FTMO-Regelmonitor
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die die FTMO-Challenge-Regeln verfolgt und Trades auf Basis des ATR-Risikos verwaltet.

Die Strategie dimensioniert Positionen mithilfe des ATR und stoppt, wenn die Challenge-Ziele erreicht sind. Sie überwacht den maximalen Tagesverlust, den Gesamtverlust, das Gewinnziel und die Mindestanzahl an Handelstagen.

## Details

- **Einstiegskriterien**: Bullische Kerze eröffnet Long, bärische Kerze eröffnet Short.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Challenge abgeschlossen oder entgegengesetztes Signal.
- **Stops**: ATR-basiert.
- **Standardwerte**:
  - `AccountSize` = 10000m
  - `RiskPercent` = 1m
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Risikomanagement
  - Richtung: Beide
  - Indikatoren: ATR
  - Stops: ATR
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
