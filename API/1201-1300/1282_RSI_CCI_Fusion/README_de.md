# RSI-CCI Fusions-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Kombiniert standardisiertes RSI und CCI zu einem einzigen Oszillator mit dynamischen Bändern.
Kauft, wenn der fusionierte Wert das untere Band nach oben kreuzt, und verkauft oder geht short, wenn er das obere Band nach unten kreuzt.

## Details

- **Einstiegskriterien**: neu skalierte Fusion kreuzt das untere Band für Long; kreuzt das obere Band für Short
- **Long/Short**: Beide (Short optional)
- **Ausstiegskriterien**: gegensätzliches Signal
- **Stops**: Nein
- **Standardwerte**:
  - `Length` = 14
  - `RsiWeight` = 0.5
  - `EnableShort` = false
- **Filter**:
  - Kategorie: Momentum
  - Richtung: Beide
  - Indikatoren: RSI, CCI, SMA, StandardDeviation
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

