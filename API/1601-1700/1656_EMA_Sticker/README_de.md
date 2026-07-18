# EMA Sticker-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie verwendet einen Exponential Moving Average (EMA), um kurzfristige Trends zu verfolgen. Eine Long-Position wird eröffnet, wenn der Schlusskurs die EMA nach oben kreuzt, während eine Short-Position bei einem Kreuzung nach unten eröffnet wird. Optionale feste Stop-Loss- und Take-Profit-Niveaus helfen beim Risikomanagement.

## Details

- **Einstiegskriterien**:
  - **Long**: `Close > EMA`.
  - **Short**: `Close < EMA`.
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - Entgegengesetztes Signal oder konfigurierte Stop-Niveaus erreicht.
- **Stops**: Ja, optionaler Stop-Loss und Take-Profit in Preiseinheiten.
- **Standardwerte**:
  - `MA period` = 5.
  - `Stop loss` = 0.001.
  - `Take profit` = 0.001.
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Einzeln
  - Stops: Ja
  - Komplexität: Einfach
  - Zeitrahmen: Kurzfristig
