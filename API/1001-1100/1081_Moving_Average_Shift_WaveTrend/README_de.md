# Moving-Average-Shift-WaveTrend-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie kombiniert einen konfigurierbaren gleitenden Durchschnitt mit einem WaveTrend-artigen Oszillator. Long-Trades entstehen, wenn der Preis über dem gleitenden Durchschnitt liegt und der Oszillator steigt, mit Bestätigung durch einen langfristigen EMA und Volatilitätsfilter. Short-Positionen werden bei entgegengesetzten Bedingungen ausgelöst. Positionen sind durch prozentualen Stop-Loss, Take-Profit und Trailing-Stop geschützt.

## Details

- **Einstiegskriterien**:
  - **Long**: Preis über MA, Oszillator > 0 und steigend, langfristiger Trend aufwärts, ATR über seinem Durchschnitt, innerhalb der Handelszeiten, nicht bereits in einer Welle.
  - **Short**: Preis unter MA, Oszillator < 0 und fallend, langfristiger Trend abwärts, ATR über seinem Durchschnitt, innerhalb der Handelszeiten, nicht bereits in einer Welle.
- **Long/Short**: Beide.
- **Ausstiegskriterien**:
  - Oszillatorumkehr mit Preiskreuzung der MA, oder Trailing-Stop, oder Schutzstops.
- **Stops**: Ja.
- **Standardwerte**:
  - `MaType` = SMA
  - `MaLength` = 40
  - `OscLength` = 15
  - `TakeProfitPercent` = 1.5
  - `StopLossPercent` = 1
  - `TrailPercent` = 1
  - `LongMaLength` = 200
  - `AtrLength` = 14
  - `StartHour` = 9
  - `EndHour` = 17
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: MA, Hull MA, ATR
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Mittelfristig
