# Game Theory Trading-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Game Theory Trading-Strategie verbindet Herdenverhalten-Analyse, Liquiditätsfallen-Erkennung, institutionelle Geldflüsse und Nash-Gleichgewichtszonen, um konträre und Momentum-Bewegungen zu handeln.

Die Strategie beobachtet RSI-Extreme und Volumenspitzen, um Massenkäufe oder -verkäufe zu erkennen. Liquiditätsfallen rund um jüngste Hochs und Tiefs sowie Akkumulations-/Distributions-Indikatoren und Smart-Money-Bias verfeinern die Einstiege. Preisbänder auf Basis eines gleitenden Durchschnitts und der Standardabweichung definieren das Nash-Gleichgewicht für Reversionstrades. Die Positionsgröße passt sich an, wenn der Preis nahe am Gleichgewicht liegt oder institutionelles Volumen erscheint.

## Details
- **Daten**: Preis- und Volumenkerzen.
- **Einstiegskriterien**: Konträre, Momentum- oder Nash-Reversionssignale.
- **Ausstiegskriterien**: Stop-Loss / Take-Profit oder Gegensignale.
- **Stops**: Optionaler Stop-Loss und Take-Profit.
- **Standardwerte**:
  - `RsiLength` = 14
  - `VolumeMaLength` = 20
  - `HerdThreshold` = 2.0
  - `LiquidityLookback` = 50
  - `InstVolumeMultiplier` = 2.5
  - `InstMaLength` = 21
  - `NashPeriod` = 100
  - `NashDeviation` = 0.02
  - `UseStopLoss` = True
  - `StopLossPercent` = 2
  - `UseTakeProfit` = True
  - `TakeProfitPercent` = 5
- **Filter**:
  - Kategorie: Gemischt konträr/Momentum
  - Richtung: Long & Short
  - Indikatoren: RSI, SMA, Accumulation/Distribution, StandardDeviation, Highest/Lowest
  - Komplexität: Fortgeschritten
  - Risikolevel: Mittel
