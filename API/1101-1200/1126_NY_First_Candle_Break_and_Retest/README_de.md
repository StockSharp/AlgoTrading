# NY Erste-Kerze-Ausbruch-und-Retest-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Handelt Ausbrüche der ersten New-York-Sitzungskerze mit Retest-Bestätigung. Verwendet ATR für Stop-Platzierung und Risiko-Ertrags-Ziele mit optionalem EMA-Trendfilter und Trailing-Stop.

## Details

- **Einstiegskriterien**: Ausbruch über Hoch oder Tief der ersten Sitzungskerze, gefolgt von einem Retest innerhalb von `RetestThreshold` ATR.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: ATR-basierter Stop und `RewardRiskRatio`-Ziel. Optionaler Trailing-Stop.
- **Stops**: `AtrMultiplier` * ATR.
- **Standardwerte**:
  - `NyStartHour` = 9
  - `NyStartMinute` = 30
  - `SessionLength` = 4
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 1.2
  - `RewardRiskRatio` = 1.5
  - `MinBreakSize` = 0.15
  - `RetestThreshold` = 0.25
  - `UseEmaFilter` = true
  - `EmaLength` = 13
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: ATR, EMA
  - Stops: ATR
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Ja
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
