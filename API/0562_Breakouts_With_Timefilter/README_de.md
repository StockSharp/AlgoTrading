# Ausbrüche mit Zeitfilter-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Ausbruchsstrategie, die beim Überschreiten von jüngsten Hochs oder Tiefs innerhalb einer bestimmten Handelssitzung einsteigt. Ein optionaler gleitender Durchschnittsfilter bestätigt die Richtung. Der Stop-Loss kann auf ATR, Kerzenextremen oder festen Punkten mit einem konfigurierbaren Risiko-Ertrags-Ziel basieren.

## Details

- **Einstiegskriterien**:
  - **Long**: Schluss > höchstes Hoch über `Length` und innerhalb des Zeitfensters; optional Schluss > MA.
  - **Short**: Schluss < niedrigstes Tief über `Length` und innerhalb des Zeitfensters; optional Schluss < MA.
- **Long/Short**: Beide
- **Stops**: ATR, kerzenbasiert oder feste Punkte mit Risiko-Ertrags-Ziel
- **Standardwerte**:
  - `Length` = 5
  - `MaLength` = 99
  - `UseMaFilter` = false
  - `UseTimeFilter` = true (14:30–15:00)
  - `SlType` = Atr
  - `SlLength` = 0
  - `AtrLength` = 14
  - `AtrMultiplier` = 0.5
  - `PointsStop` = 50
  - `RiskReward` = 3
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
