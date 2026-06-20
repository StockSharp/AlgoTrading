# AI Supertrend Pivot Percentile-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie kombiniert zwei Supertrend-Indikatoren mit einem ADX-Filter und einem Williams %R Pivot-Perzentil-Filter. Eine Long-Position wird eröffnet, wenn der Preis über beiden Supertrends liegt, der ADX einen starken Trend bestätigt und der Williams %R über -50 liegt. Short-Positionen verwenden die entgegengesetzten Bedingungen.

## Details

- **Einstiegskriterien**:
  - **Long**: Preis über beiden Supertrends, ADX > Schwellenwert, Williams %R > -50.
  - **Short**: Preis unter beiden Supertrends, ADX > Schwellenwert, Williams %R < -50.
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - Entgegengesetztes Signal.
- **Stops**: Prozentbasierter Take-Profit und Stop-Loss.
- **Standardwerte**:
  - `Length1` = 10
  - `Factor1` = 3
  - `Length2` = 20
  - `Factor2` = 4
  - `AdxLength` = 14
  - `AdxThreshold` = 20
  - `PivotLength` = 14
  - `TpPercent` = 2
  - `SlPercent` = 1
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: SuperTrend, ADX, Williams %R
  - Stops: Ja
  - Komplexität: Moderat
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
