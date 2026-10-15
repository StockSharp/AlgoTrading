# Color J Variations-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die den ColorJVariation Expert Advisor mithilfe des Jurik Moving Average repliziert. Sie verfolgt die JMA-Steigung und steigt ein, wenn sich die Richtung ändert. Die Strategie unterstützt absolute Stop-Loss- und Take-Profit-Levels.

## Details

- **Einstiegskriterien**:
  - Long: `PrevSlopeDown && JMA turns up`
  - Short: `PrevSlopeUp && JMA turns down`
- **Long/Short**: Beide
- **Ausstiegskriterien**:
  - Entgegengesetztes Umkehrsignal
- **Stops**: Absolut über `StopLoss` und `TakeProfit`
- **Standardwerte**:
  - `JmaPeriod` = 12
  - `JmaPhase` = 100
  - `StopLoss` = 1000
  - `TakeProfit` = 2000
  - `CandleType` = TimeSpan.FromHours(1).TimeFrame()
- **Filter**:
  - Kategorie: Trendumkehr
  - Richtung: Beide
  - Indikatoren: Jurik Moving Average
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
