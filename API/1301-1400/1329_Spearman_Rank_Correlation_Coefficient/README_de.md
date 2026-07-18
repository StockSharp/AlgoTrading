# Strategie des Spearman-Rangkorrelationskoeffizienten
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Paarhandelsstrategie misst die Spearman-Rangkorrelation zwischen zwei Wertpapieren. Wenn die Korrelation einen positiven Schwellenwert überschreitet, geht die Strategie das erste Wertpapier short und das zweite long. Wenn sie unter den negativen Schwellenwert fällt, nimmt sie die entgegengesetzte Position ein. Positionen werden geschlossen, wenn die Korrelation gegen null zurückkehrt.

## Details

- **Einstiegskriterien:**
  - **Long erstes / Short zweites**: Korrelation < -Threshold.
  - **Short erstes / Long zweites**: Korrelation > Threshold.
- **Long/Short**: Paarhandel.
- **Ausstiegskriterien:**
  - Absolutwert der Korrelation < Threshold / 2.
- **Stops**: Nein.
- **Standardwerte:**
  - `CorrelationPeriod` = 10
  - `Threshold` = 0.8
- **Filter:**
  - Kategorie: Korrelation
  - Richtung: Beide
  - Indikatoren: Spearman Rank Correlation
  - Stops: Nein
  - Komplexität: Mittel
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
