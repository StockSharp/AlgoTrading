# SuperTrend SDI Webhook-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie basierend auf SuperTrend und dem Geglätteten Richtungsindikator (SDI). Sie geht Long, wenn +DI über -DI liegt und SuperTrend einen Aufwärtstrend anzeigt. Short-Positionen werden eröffnet, wenn -DI über +DI liegt und SuperTrend nach unten zeigt. Die Strategie wendet prozentualen Take-Profit, Stop-Loss und Trailing-Stop an.

## Details

- **Einstiegskriterien**:
  - Long: `+DI > -DI && SuperTrend up`
  - Short: `-DI > +DI && SuperTrend down`
- **Long/Short**: Beide
- **Ausstiegskriterien**: Take-Profit, Stop-Loss oder Trailing-Stop
- **Indikatoren**: SuperTrend, AverageDirectionalIndex
- **Stops**: Prozentualer Take-Profit, Stop-Loss, Trailing-Stop
- **Standardwerte**:
  - `AtrPeriod` = 10
  - `AtrMultiplier` = 1.8m
  - `DiLength` = 3
  - `DiSmooth` = 7
  - `TakeProfitPercent` = 25m
  - `StopLossPercent` = 4.8m
  - `TrailingPercent` = 1.9m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: SuperTrend, SDI
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
