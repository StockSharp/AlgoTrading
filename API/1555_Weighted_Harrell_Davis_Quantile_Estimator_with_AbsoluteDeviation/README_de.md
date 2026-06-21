# Strategie: Gewichteter Harrell-Davis-Quantilschätzer mit AbsoluterAbweichung
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie verwendet einen medianbasierten Quantilschätzer mit Bändern der absoluten Abweichung, um Preisausreißer zu erkennen.
Kauft, wenn der Schlusskurs unter das untere Band fällt, und verkauft, wenn er über das obere Band steigt.

## Details

- **Einstiegskriterien**: Schlusskurs unterhalb des unteren Abweichungsbandes oder oberhalb des oberen Bandes
- **Long/Short**: Beide
- **Ausstiegskriterien**: Kreuzung des gegenüberliegenden Bandes
- **Stops**: Nein
- **Standardwerte**:
  - `Length` = 39
  - `DeviationMultiplier` = 1.213
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Beide
  - Indikatoren: Median
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
