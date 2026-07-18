# Supertrend AT v1.0 Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Eine Supertrend-basierte Strategie, die eine Long-Position eröffnet, wenn der Supertrend von abwärts nach aufwärts wechselt, und eine Short-Position, wenn er von aufwärts nach abwärts wechselt. Die Positionsgröße wird aus dem Risiko pro Trade berechnet, und Ausstiege verwenden Stop-Loss- und Take-Profit-Niveaus aus dem vorherigen Supertrend.

## Details

- **Einstiegskriterien**: Supertrend-Richtungsänderung.
- **Long/Short**: Long und Short.
- **Ausstiegskriterien**: Ziel oder Stop erreicht.
- **Stops**: Ja.
- **Standardwerte**:
  - `SupertrendLength` = 10
  - `SupertrendMultiplier` = 3m
  - `RiskPerTrade` = 2m
  - `RewardRatio` = 3m
  - `CommissionPercent` = 0.05m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Long & Short
  - Indikatoren: Supertrend
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Kurzfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
