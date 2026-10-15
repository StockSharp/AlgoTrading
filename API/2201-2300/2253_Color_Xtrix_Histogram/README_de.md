# Color XTRIX Histogramm-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt auf Basis von Richtungswechseln eines geglätteten TRIX (Triple Exponential Moving Average Momentum), berechnet aus logarithmischen Schlusskursen. Eine Long-Position wird eröffnet, wenn das TRIX-Histogramm nach einem Rückgang nach oben dreht, während eine Short-Position eröffnet wird, wenn es nach einem Anstieg nach unten dreht. Positionen werden bei entgegengesetzten Drehungen umgekehrt. Es werden kein Stop-Loss oder Take-Profit verwendet.

## Details

- **Einstiegskriterien**:
  - **Long**: `TRIX rising` && `previous TRIX falling`
  - **Short**: `TRIX falling` && `previous TRIX rising`
- **Long/Short**: Long und Short
- **Ausstiegskriterien**:
  - Long: `TRIX turns downward`
  - Short: `TRIX turns upward`
- **Stops**: Nein
- **Standardwerte**:
  - `TRIX Length` = 5
  - `Smooth Length` = 5
  - `Momentum Period` = 1
  - `Candle Type` = 4h Zeitrahmen
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: TRIX
  - Stops: Nein
  - Komplexität: Niedrig
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
