# MA L World-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Gewichtete gleitende Durchschnitt-Kreuzungsstrategie mit Trailing-Stop basierend auf EMA.

Eröffnet eine Long-Position, wenn der schnelle WMA den langsamen WMA von unten kreuzt. Eröffnet eine Short-Position, wenn der schnelle WMA den langsamen WMA von oben kreuzt. Verwendet einen 92-Perioden-EMA als Trailing-Ausstieg sowie feste Stop-Loss- und Take-Profit-Levels.

## Details

- **Einstiegskriterien**:
  - Long: `Schneller WMA` kreuzt über `Langsamer WMA`
  - Short: `Schneller WMA` kreuzt unter `Langsamer WMA`
- **Long/Short**: Beide
- **Ausstiegskriterien**: Gegenläufige Kreuzung oder Preis, der den Trailing-EMA kreuzt
- **Stops**: Stop-Loss und Take-Profit über `StartProtection`
- **Standardwerte**:
  - `FastMaLength` = 12
  - `SlowMaLength` = 25
  - `TrailingMaPeriod` = 92
  - `StopLoss` = 95m
  - `TakeProfit` = 670m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: WMA, EMA
  - Stops: Stop-Loss, Take-Profit, Trailing-EMA
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
