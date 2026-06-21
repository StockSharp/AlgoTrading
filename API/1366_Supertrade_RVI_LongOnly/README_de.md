# Supertrade RVI Nur-Long-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Verwendet den Relative Volatility Index (RVI), der über 20 kreuzt, um Long-Trades zu eröffnen. Stop-Loss und Take-Profit werden aus dem Risikoprozentsatz und dem Gewinn-Verhältnis berechnet.

## Details

- **Einstiegskriterien**: RVI kreuzt über den Schwellenwert
- **Long/Short**: Long
- **Ausstiegskriterien**: Stop-Loss oder Take-Profit
- **Stops**: Ja
- **Standardwerte**:
  - `RviLength` = 10
  - `EmaLength` = 14
  - `RviThreshold` = 20
  - `RiskPercent` = 1
  - `RewardRatio` = 3
- **Filter**:
  - Kategorie: Momentum
  - Richtung: Long
  - Indikatoren: StdDev, EMA
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

