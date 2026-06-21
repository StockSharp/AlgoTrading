# Risiko-Rendite-Strategie mit festem SL Backtester
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Geht long, wenn der Schlusskurs einem benutzerdefinierten Wert entspricht. Der Stop-Loss wird durch ATR oder Pivot-Tief gesetzt und der Take-Profit verwendet ein Risiko-Rendite-Verhältnis oder einen festen Prozentsatz. Optional wird der Stop nach Erreichen eines Ziels auf Breakeven verschoben.

## Details

- **Einstiegskriterien**: Schlusskurs gleich `DealStartValue`
- **Long/Short**: Long
- **Ausstiegskriterien**: Take-Profit oder Stop-Loss (optionales Breakeven)
- **Stops**: ATR oder Pivot-Tief mit Breakeven
- **Standardwerte**:
  - `DealStartValue` = 100
  - `UseRiskToReward` = true
  - `RiskToRewardRatio` = 1.5
  - `StopLossType` = Atr
  - `AtrFactor` = 1.4
  - `PivotLookback` = 8
  - `FixedTp` = 0.015
  - `FixedSl` = 0.015
  - `UseBreakEven` = true
  - `BreakEvenRr` = 1.0
  - `BreakEvenPercent` = 0.001
- **Filter**:
  - Kategorie: Trend
  - Richtung: Nur Long
  - Indikatoren: ATR, Lowest
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
