# RSI & ADX Long/Short-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt beide Seiten mit RSI für Signale und ADX zur Trendbestätigung.
Eine Long-Position wird eröffnet, wenn RSI über 70 kreuzt und ADX über einem Schwellenwert liegt.
Eine Short-Position wird eröffnet, wenn RSI unter 30 kreuzt und ADX über dem Schwellenwert liegt.
Positionen werden bei entgegengesetzten RSI-Kreuzungen geschlossen.

## Details

- **Einstiegskriterien**: RSI kreuzt über 70 für Longs oder unter 30 für Shorts bei ADX über dem Schwellenwert
- **Long/Short**: Beide
- **Ausstiegskriterien**: Entgegengesetzte RSI-Kreuzungen
- **Stops**: Nein
- **Standardwerte**:
  - `RsiLength` = 8
  - `AdxLength` = 20
  - `AdxThreshold` = 14
- **Filter**:
  - Kategorie: Indikator
  - Richtung: Beide
  - Indikatoren: RSI, ADX
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
