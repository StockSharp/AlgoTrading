# Fractal RSI-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Adaptive Strategie basierend auf dem Fractal RSI-Indikator.
Fractal RSI passt die Länge der RSI-Berechnung anhand der fraktalen Dimension der Preisbewegung an,
sodass der Oszillator in Trendmärkten schneller und in Seitwärtsphasen langsamer reagiert.

Die Strategie eröffnet Positionen, wenn der Indikator vordefinierte Levels kreuzt.
Sie kann mit dem erkannten Trend oder gegen ihn handeln, je nach gewähltem Modus.

## Details

- **Einstiegskriterien**:
  - *Trend-Modus - Direkt*:
    - Kauf: Wert kreuzt `LowLevel` von oben nach unten
    - Verkauf: Wert kreuzt `HighLevel` von unten nach oben
  - *Trend-Modus - Gegen*:
    - Kauf: Wert kreuzt `HighLevel` von unten nach oben
    - Verkauf: Wert kreuzt `LowLevel` von oben nach unten
- **Long/Short**: Beide
- **Ausstiegskriterien**: Gegensignal
- **Stops**: Optionaler fester Stop-Loss und Take-Profit
- **Standardwerte**:
  - `CandleType` = `TimeSpan.FromHours(4).TimeFrame()`
  - `FractalPeriod` = 30
  - `NormalSpeed` = 30
  - `HighLevel` = 60
  - `LowLevel` = 40
  - `StopLoss` = 1000 Punkte
  - `TakeProfit` = 2000 Punkte
- **Filter**:
  - Kategorie: Trend / Oszillator
  - Richtung: Beide
  - Indikatoren: Fractal Dimension, RSI
  - Stops: Ja
  - Komplexität: Fortgeschrittene Indikatornutzung
  - Zeitrahmen: 4H (konfigurierbar)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
