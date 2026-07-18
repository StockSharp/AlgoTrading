# Doppelter SuperTrend mit VIX-Filter-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie kombiniert zwei SuperTrend-Indikatoren mit einem VIX-basierten Volatilitätsfilter. Eine Long-Position wird eröffnet, wenn beide SuperTrends bullisch sind und der VIX-Index über seinem Mittelwert liegt. Eine Short-Position wird eröffnet, wenn beide SuperTrends bärisch sind und der VIX über seinem Durchschnitt plus einem Standardabweichungspuffer steigt. Positionen werden geschlossen, wenn einer der SuperTrends die Richtung wechselt.

## Details

- **Einstiegskriterien**:
  - **Long**: Beide SuperTrends zeigen einen Aufwärtstrend und der VIX liegt über seinem Mittelwert.
  - **Short**: Beide SuperTrends zeigen einen Abwärtstrend und der VIX liegt über seinem Mittelwert und steigt.
- **Ausstiegskriterien**:
  - Entgegengesetztes SuperTrend-Signal.
- **Stops**: Keine.
- **Standardwerte**:
  - `StLength1` = 13
  - `StMultiplier1` = 3.5
  - `StLength2` = 8
  - `StMultiplier2` = 5
  - `UseVixFilter` = true
  - `VixLookback` = 252
  - `VixTrendPeriod` = 10
  - `StdDevMultiplier` = 1
  - `EnableLong` = true
  - `EnableShort` = true
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: SuperTrend, SMA, StandardDeviation, EMA
  - Stops: Nein
  - Komplexität: Moderat
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
