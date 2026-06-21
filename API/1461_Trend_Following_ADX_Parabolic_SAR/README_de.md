# Trendfolge-Strategie ADX Parabolic SAR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Verwendet ADX mit Richtungsbewegung und Parabolic SAR, um Trends zu folgen. Long-Positionen entstehen, wenn ADX über einem Schwellenwert liegt, +DI -DI überschreitet und der Preis über der SAR-Linie liegt. Short-Signale verwenden die entgegengesetzte Konfiguration.

## Details

- **Einstiegskriterien**: ADX > Schwellenwert mit DI-Kreuzung und Preis > SAR.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Entgegengesetztes Signal.
- **Stops**: Nein.
- **Standardwerte**:
  - `AdxPeriod` = 14
  - `AdxThreshold` = 25
  - `SarStep` = 0.02
  - `SarMax` = 0.2
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: ADX, Parabolic SAR
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (1m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
