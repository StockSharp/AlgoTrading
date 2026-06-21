# TEMA Benutzerdefinierte Steigungs-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Umkehrstrategie, die Steigungsänderungen eines Triple Exponential Moving Average (TEMA) nutzt. Der Indikator wird auf dem angegebenen Zeitrahmen berechnet, und die Strategie reagiert auf Richtungsänderungen.

## Funktionsweise

- **Einstiegskriterien**:
  - **Long**: TEMA fiel und dreht nach oben.
  - **Short**: TEMA stieg und dreht nach unten.
- **Ausstiegskriterien**: Umgekehrtes Signal schließt die bestehende Position.
- **Indikatoren**: Triple Exponential Moving Average.

## Schlüsselparameter

- `TemaLength` – Anzahl der Balken für die TEMA-Berechnung.
- `CandleType` – Zeitrahmen der für die Analyse verwendeten Kerzen.
