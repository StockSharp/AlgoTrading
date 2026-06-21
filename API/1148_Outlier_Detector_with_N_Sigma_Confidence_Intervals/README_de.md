# Ausreißer-Erkennungs-Strategie mit N-Sigma-Konfidenzintervallen
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie identifiziert Ausreißer in Preisveränderungen mithilfe von N-Sigma-Konfidenzintervallen und handelt Mean Reversion bei extremen Bewegungen.

## Details

- **Einstiegskriterien**:
  - Short wenn z-score > `SecondLimit`.
  - Long wenn z-score < -`SecondLimit`.
- **Long/Short**: Beide.
- **Ausstiegskriterien**:
  - Position schließen wenn |z-score| < `FirstLimit`.
- **Stops**: Keine.
- **Standardwerte**:
  - `SampleSize` = 30
  - `FirstLimit` = 2
  - `SecondLimit` = 3
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Beide
  - Indikatoren: StandardDeviation, Z-Score
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Risikolevel: Mittel
