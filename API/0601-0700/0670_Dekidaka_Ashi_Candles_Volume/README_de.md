# Dekidaka-Ashi Kerzen-Volumen-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie kombiniert den Kerzenkörper mit geglättetem Volumen nach dem Dekidaka-Ashi-Ansatz. Sie kauft bei bullischen Signalen und verkauft bei bärischen. Kerzen, die beide Bereiche überspannen, schließen offene Positionen.

## Details

- **Einstiegskriterien**:
  - Starkes oder schwaches bullisches Signal: Hoch über oberem Bereich und Tief über unterem Bereich.
  - Starkes oder schwaches bärisches Signal: Hoch unter oberem Bereich und Tief unter unterem Bereich.
- **Long/Short**: Beide.
- **Ausstiegskriterien**:
  - Gegenteiliges Signal oder Kerze, die beide Bereiche überspannt (Unsicherheit).
- **Stops**: Nein.
- **Standardwerte**:
  - `BodySize` = 1
  - `VolumeSmooth` = 1
  - `CandleType` = 5-Minuten-Zeitrahmen
- **Filter**:
  - Kategorie: Muster
  - Richtung: Long & Short
  - Indikatoren: EMA, Volumen
  - Stops: Nein
  - Komplexität: Niedrig
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
