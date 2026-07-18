# EMA WMA Kreuzungs-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie basierend auf der Kreuzung zwischen dem exponentiellen gleitenden Durchschnitt (EMA) und dem gewichteten gleitenden Durchschnitt (WMA), berechnet auf Kerzeneröffnungspreisen.
Long-Einstieg, wenn EMA die WMA nach unten kreuzt; Short-Einstieg, wenn EMA die WMA nach oben kreuzt.
Die Positionsgröße wird durch den Risikoprozentsatz des Kontoeigenkapitals bestimmt.
Die Strategie verwendet feste Take-Profit- und Stop-Loss-Abstände in Ticks.

## Details

- **Einstiegskriterien**:
  - Long: `EMA crosses below WMA`
  - Short: `EMA crosses above WMA`
- **Long/Short**: Beide
- **Ausstiegskriterien**: Stop-Loss oder Take-Profit
- **Stops**: Ja
- **Standardwerte**:
  - `EmaPeriod` = 28
  - `WmaPeriod` = 8
  - `StopLossTicks` = 50
  - `TakeProfitTicks` = 50
  - `RiskPercent` = 10
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filter**:
  - Kategorie: Gleitender-Durchschnitt-Kreuzung
  - Richtung: Beide
  - Indikatoren: EMA, WMA
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
