# Distance-to-Demand-Vector-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie basierend auf dem Distance-to-Demand-Vector-Indikator. Sie vergleicht die Abstände zu den Long- und Short-Nachfragevektoren und handelt bei deren Kreuzung.

## Details

- **Einstiegskriterien**:
  - Long: Abstand zum Long-Vektor > Abstand zum Short-Vektor
  - Short: Abstand zum Long-Vektor < Abstand zum Short-Vektor
- **Long/Short**: Beide
- **Ausstiegskriterien**:
  - Entgegengesetztes Signal
- **Stops**: Keine
- **Standardwerte**:
  - `Length` = 100
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: Highest, Lowest
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Kurzfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
