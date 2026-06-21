# Strategie des Linearen Korrelationsoszillators
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie des Linearen Korrelationsoszillators misst die Korrelation zwischen Preis und Zeit über ein rollierendes Fenster. Die Strategie geht long, wenn der Oszillator über null kreuzt, und short, wenn er unter null kreuzt.

## Details

- **Einstiegskriterien**:
  - Oszillator kreuzt über null → **Long**.
  - Oszillator kreuzt unter null → **Short**.
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - Entgegengesetzter Nulldurchgang.
- **Stops**: Keine.
- **Standardwerte**:
  - `Length` = 14
- **Filter**:
  - Kategorie: Oszillator
  - Richtung: Beide
  - Indikatoren: Linear Correlation
  - Stops: Keine
  - Komplexität: Niedrig
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
