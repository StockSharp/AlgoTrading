# Kalman-Filter-Signal-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie verwendet den Kalman Filter-Indikator zur Erkennung von Richtungsänderungen. Die Filterausgabe wird je nach gewähltem Signalmodus entweder mit dem Preis oder seiner Steigung verglichen. Wenn das Signal bullisch wird, eröffnet die Strategie eine Long-Position; wenn es bärisch ist, eine Short-Position. Positionen werden bei entgegengesetzten Signalen umgekehrt. Stop-Loss und Take-Profit werden mit absoluten Abständen angewendet.

## Details

- **Einstiegskriterien**:
  - Long: Signal wechselt auf bullisch
  - Short: Signal wechselt auf bärisch
- **Long/Short**: Beide
- **Ausstiegskriterien**: Entgegengesetztes Signal
- **Stops**: Absoluter Stop-Loss und Take-Profit
- **Standardwerte**:
  - `ProcessNoise` = 1.0
  - `MeasurementNoise` = 1.0
  - `CandleType` = TimeSpan.FromHours(3).TimeFrame()
  - `Mode` = SignalModes.Kalman
  - `StopLoss` = 1000m
  - `TakeProfit` = 2000m
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Kalman Filter
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
