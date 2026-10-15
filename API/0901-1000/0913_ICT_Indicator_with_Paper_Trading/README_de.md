# ICT Indikator mit Paper-Trading Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie speichert die Hochs und Tiefs von Order Blocks und geht long, wenn der Schlusskurs über das letzte Order-Block-Hoch kreuzt. Die Long-Position wird geschlossen, wenn das gespeicherte Order-Block-Tief über den Preis kreuzt.

## Details

- **Einstiegskriterien**:
  - **Long**: Schlusskurs kreuzt über das letzte Order-Block-Hoch.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**:
  - Long schließen, wenn das Order-Block-Tief über den Preis kreuzt.
- **Stops**: Nein.
- **Standardwerte**:
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame().
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Nur Long
  - Indikatoren: Price action
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
