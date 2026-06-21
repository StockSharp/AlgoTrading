# Majors-Volumen-Summen-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie summiert das vorzeichenbehaftete Volumen über die letzten Kerzen und handelt, wenn die kurzfristige Summe einen Bruchteil ihres historischen Maximums überschreitet.

## Details

- **Einstiegskriterien**:
  - Die 10-Perioden-Volumenssumme mit Vorzeichen liegt über `Threshold` × Maximum und keine Position vorhanden: Long eingehen.
  - Die 10-Perioden-Volumenssumme mit Vorzeichen liegt unter `-Threshold` × Maximum und keine Position vorhanden: Short eingehen.
- **Long/Short**: Beide.
- **Ausstiegskriterien**:
  - Ein entgegengesetztes Signal schließt die Position.
- **Stops**: Keine.
- **Standardwerte**:
  - `Threshold` = 0.75
- **Filter**:
  - Kategorie: Volumen
  - Richtung: Beide
  - Indikatoren: SMA
  - Stops: Nein
  - Komplexität: Niedrig
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
