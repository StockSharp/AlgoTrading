# LANZ-Strategie 5.0
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die LANZ-Strategie 5.0 handelt in Richtung eines 200-Perioden-EMA und erfordert drei aufeinanderfolgende Kerzen derselben Farbe. Sie begrenzt die Trades nach täglicher Anzahl, New Yorker Zeitfenster und Mindestabstand zwischen Einstiegen.

## Details

- **Einstiegskriterien**:
  - Preis über EMA und drei bullische Kerzen für Long-Einstiege.
  - Preis unter EMA und drei bärische Kerzen für Short-Einstiege (optional).
- **Long/Short**: Standard Long.
- **Ausstiegskriterien**:
  - Fester Stop-Loss oder Take-Profit.
  - Manuelles Schließen zur konfigurierten Zeit.
- **Stops**:
  - Stop Loss = 40 Pips.
  - Take Profit = 120 Pips.
- **Standardwerte**:
  - `EmaPeriod` = 200
  - `MaxTrades` = 99
  - `MinDistancePips` = 25
  - `StopLossPips` = 40
  - `TakeProfitPips` = 120
  - `StartHour` = 19
  - `EndHour` = 15
- **Filter**:
  - Kategorie: Trend
  - Richtung: Long
  - Indikatoren: EMA
  - Stops: Ja
  - Komplexität: Moderat
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
