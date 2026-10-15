# Verbessertes zeitlich segmentiertes Volumen
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Das verbesserte zeitlich segmentierte Volumen überwacht volumengewichtete Preisänderungen. Wenn der TSV über seinem gleitenden Durchschnitt und positiv ist, kauft die Strategie. Wenn der TSV unter dem Durchschnitt und negativ ist, geht sie Short.

## Details

- **Einstiegskriterien**: TSV relativ zu seinem gleitenden Durchschnitt.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Entgegengesetztes Signal oder Stop.
- **Stops**: Ja.
- **Standardwerte**:
  - `TsvLength` = 13
  - `MaLength` = 7
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Momentum
  - Richtung: Beide
  - Indikatoren: Volumen, SMA
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
