# Pivot Point SuperTrend Trendfilter-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Kombiniert eine pivot-basierte SuperTrend-Linie mit einem SuperTrend-Trendfilter und einer gleitenden Durchschnitts-Bestätigung. Handelt, wenn der Trend dreht oder wenn ein Pivot SuperTrend-Signal innerhalb eines Datumsfensters erscheint.

## Details

- **Einstiegskriterien**:
  - Trendfilter dreht nach oben und der Kurs liegt oberhalb des gleitenden Durchschnitts.
  - Pivot SuperTrend gibt ein Kaufsignal innerhalb des konfigurierten Datumsbereichs.
- **Ausstiegskriterien**:
  - Trendfilter dreht nach unten oder Pivot SuperTrend gibt ein Verkaufssignal.
- **Stops**: Keine
- **Standardwerte**:
  - `PivotPeriod` = 2
  - `Factor` = 3
  - `AtrPeriod` = 10
  - `TrendAtrPeriod` = 10
  - `TrendMultiplier` = 3
  - `MaPeriod` = 20
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: Pivot, SuperTrend, SMA
  - Stops: Nein
  - Komplexität: Moderat
  - Zeitrahmen: Beliebig
  - Saisonalität: Optional
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
