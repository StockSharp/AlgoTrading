# EMA WPR Rücksetzer-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Trendfolge-Strategie, die einen EMA-Trendfilter mit Williams %R-Extremwerten kombiniert. Sie wartet auf einen Rücksetzer im Williams %R, bevor ein weiterer Trade erlaubt wird, und kann bis zu einer festgelegten Anzahl von Positionen pyramidisieren.

## Details

- **Einstiegskriterien**:
  - **Long**: Williams %R fällt unter -100 und dann tritt ein Rücksetzer über `WPR Retracement` auf. Optionaler Aufwärtstrend durch EMA bestätigt.
  - **Short**: Williams %R steigt über 0 und setzt dann unter `-WPR Retracement` zurück. Optionaler Abwärtstrend durch EMA bestätigt.
- **Long/Short**: Beide Richtungen mit Pyramidisierung.
- **Ausstiegskriterien**:
  - Williams %R verlässt die Extremzone.
  - Optionaler Ausstieg nach `Max Unprofit Bars` ohne Gewinn.
  - Stop-Loss, Take-Profit und optionaler Trailing-Stop, verwaltet durch Schutzmodul.
- **Stops**: Fester Stop-Loss und Take-Profit mit optionalem Trailing-Stop.
- **Standardwerte**:
  - `Use EMA Trend` = true
  - `Bars In Trend` = 1
  - `EMA Trend` = 144
  - `WPR Period` = 46
  - `WPR Retracement` = 30
  - `Use WPR Exit` = true
  - `Order Volume` = 0.1
  - `Max Trades` = 2
  - `Stop Loss` = 50
  - `Take Profit` = 200
  - `Use Trailing` = false
  - `Trailing Stop` = 10
  - `Use Unprofit Exit` = false
  - `Max Unprofit Bars` = 5
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: EMA, Williams %R
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
