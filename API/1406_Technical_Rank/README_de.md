# Technisches Ranking (Strategie)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie berechnet ein zusammengesetztes technisches Ranking aus gleitenden Durchschnitten, Rate of Change, PPO-Steigung und RSI. Long-Positionen werden eröffnet, wenn das Ranking einen oberen Schwellenwert überschreitet, Short-Positionen, wenn es unter einen unteren Schwellenwert fällt.

## Details

- **Einstiegskriterien**: Ranking > UpperThreshold → Long; Ranking < LowerThreshold → Short.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Entgegengesetztes Signal.
- **Stops**: Nein.
- **Standardwerte**:
  - `UpperThreshold` = 70
  - `LowerThreshold` = 30
  - `CandleType` = 1-Minuten-Kerzen
