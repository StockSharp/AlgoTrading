# Supertrend Hombrok Bot-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Supertrend-Strategie mit Volumen-, Kerzenkörper- und RSI-Filtern sowie ATR-basiertem Stop und Take-Profit.

## Details
- **Einstiegskriterien**: Aufwärtstrend mit Volumen- und Körperfiltern und RSI unter Überkauft für Longs; Abwärtstrend mit Filtern und RSI über Überverkauft für Shorts
- **Long/Short**: Beide
- **Ausstiegskriterien**: ATR-basierter Stop-Loss oder Take-Profit
- **Stops**: Fester Stop und Take-Profit aus ATR
- **Standardwerte**:
  - `AtrPeriod` = 10
  - `AtrMultiplier` = 3m
  - `RsiPeriod` = 14
  - `RsiOverbought` = 70m
  - `RsiOversold` = 30m
  - `VolumeMultiplier` = 1.2m
  - `BodyPctOfAtr` = 0.3m
  - `RiskRewardRatio` = 2m
  - `CapitalPerTrade` = 10m
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Supertrend, RSI, ATR, Volume
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Kurzfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
