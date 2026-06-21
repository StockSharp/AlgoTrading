# Strategie mit Polynomialregressions-Bandkanal
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie passt eine Polynomialregressionslinie an die jüngsten Kurse an und erstellt obere und untere Bänder aus der Standardabweichung der Residuen. Long-Positionen werden eröffnet, wenn der Kurs unter das untere Band fällt, und Short-Positionen, wenn der Kurs über das obere Band steigt.

## Details

- **Einstiegskriterien**:
  - **Long**: `Close < LowerBand`.
  - **Short**: `Close > UpperBand`.
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - Gegensätzliches Signal.
- **Stops**: Nein.
- **Standardwerte**:
  - `Length` = 100.
  - `Degree` = 2.
  - `Std Dev Multiplier` = 2.
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Beide
  - Indikatoren: Polynomialregression
  - Stops: Nein
  - Komplexität: Moderat
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
