# COSTAR-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die COSTAR-Strategie erstellt eine lineare Regression der Schlusskurse und misst die Standardabweichung der Residuen. Obere und untere Bänder werden gebildet, indem die mit einem Faktor multiplizierte Abweichung addiert und subtrahiert wird. Trades versuchen, extreme Abweichungen gegenzuhandeln, und werden geschlossen, wenn der Kurs zur Regressionslinie zurückkehrt.

## Details

- **Einstiegskriterien**:
  - **Long**: Kurs kreuzt über das untere Band.
  - **Short**: Kurs kreuzt unter das obere Band.
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - Kurs kreuzt zurück durch die Regressionslinie.
- **Stops**: Keine.
- **Standardwerte**:
  - `Length` = 100
  - `Multiplier` = 1
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Beide
  - Indikatoren: Linear Regression, Standard Deviation
  - Stops: Nein
  - Komplexität: Niedrig
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
