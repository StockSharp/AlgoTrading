# IBS-Strategie für die Innere Balkenstärke
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie eröffnet eine Long-Position, wenn die innere Balkenstärke (IBS) unter einem unteren Schwellenwert liegt, und schließt sie, wenn der IBS innerhalb eines bestimmten Zeitfensters über einem oberen Schwellenwert steigt.

## Details

- **Einstiegskriterien**:
  - IBS < `LowerThreshold`.
  - Zeit zwischen `StartTime` und `EndTime`.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**:
  - IBS >= `UpperThreshold`.
- **Stops**: Keine.
- **Standardwerte**:
  - `UpperThreshold` = 0.8
  - `LowerThreshold` = 0.2
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Nur Long
  - Indikatoren: Keine
  - Stops: Nein
  - Komplexität: Niedrig
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Niedrig
