# EMA-Crossover-Take-Profit-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt auf Basis des Kreuzungssignals der 20- und 50-Perioden Exponentiell Gleitenden Durchschnitte (EMAs). Eine Long-Position wird eröffnet, wenn die schnelle EMA die langsame EMA nach oben kreuzt, und eine Short-Position beim entgegengesetzten Kreuzungssignal.

Nach einem Einstieg werden vier Take-Profit-Niveaus aus der Spanne der Signalkerze berechnet. Die Position wird geschlossen, wenn der Preis eines dieser Niveaus erreicht oder ein Stop-Loss ausgelöst wird. Kerzen werden grün markiert, wenn die schnelle EMA über der langsamen EMA liegt, und rot, wenn sie darunter liegt.

## Details

- **Einstiegskriterien**:
  - **Long**: EMA20 kreuzt EMA50 nach oben.
  - **Short**: EMA20 kreuzt EMA50 nach unten.
- **Take Profit**: Vier Ziele basierend auf Multiplikatoren der vorherigen Range.
- **Stops**: 3% Stop-Loss vom Einstiegspreis.
- **Indikatoren**: EMA20, EMA50, EMA200.
- **Zeitrahmen**: Per Parameter konfigurierbar.
- **Richtung**: Long und Short.
