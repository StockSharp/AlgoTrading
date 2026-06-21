# Kaufman Trend-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **Kaufman Trend-Strategie** verwendet einen Kalman-Filter, um Preis und Momentum zu schätzen. Die Trendstärke wird aus der Geschwindigkeitskomponente des Filters abgeleitet und über ein aktuelles Fenster normalisiert. Einstiege erfolgen, wenn starke Trendbedingungen mit einem Preis über oder unter dem gefilterten Wert zusammenfallen. Stops basieren auf aktuellen Swings plus ATR, und Gewinne werden schrittweise mitgenommen, wenn das Momentum nachlässt.

## Details
- **Einstiegskriterien**: Trendstärkeschwellenwert mit Preis über/unter dem gefilterten Wert.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: gestaffelte Gewinnmitnahmen und Trendabschwächung oder Stop-Auslösung.
- **Stops**: Ja, Swing-Tief/Hoch minus/plus ATR.
- **Standardwerte**:
  - `TakeProfit1Percent = 50`
  - `TakeProfit2Percent = 25`
  - `TakeProfit3Percent = 25`
  - `SwingLookback = 10`
  - `AtrPeriod = 14`
  - `TrendStrengthEntry = 60`
  - `TrendStrengthExit = 40`
  - `CandleType = TimeSpan.FromMinutes(15).TimeFrame()`
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Kalman
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday (15m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
