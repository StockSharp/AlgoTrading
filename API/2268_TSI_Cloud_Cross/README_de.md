# TSI-Wolken-Kreuzungs-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die TSI-Wolken-Kreuzungs-Strategie vergleicht den True Strength Index (TSI) mit einer verzögerten Kopie von sich selbst, um eine Wolke zu bilden. Eine Long-Position wird eröffnet, wenn der TSI die verschobene Linie von unten nach oben kreuzt, was auf bullisches Momentum hindeutet. Eine Short-Position wird eröffnet, wenn der TSI die verschobene Linie von oben nach unten kreuzt. Signale können invertiert werden und entgegengesetzte Positionen können optional geschlossen werden.

## Details

- **Einstiegskriterien**:
  - TSI kreuzt über seinen verschobenen Wert (Long).
  - TSI kreuzt unter seinen verschobenen Wert (Short).
- **Long/Short**: Beide.
- **Ausstiegskriterien**:
  - Optionales Schließen bei entgegengesetztem Signal.
- **Stops**: Keine.
- **Standardwerte**:
  - `LongLength` = 25
  - `ShortLength` = 13
  - `TriggerShift` = 1
  - `Invert` = false
- **Filter**:
  - Kategorie: Momentum-Oszillator
  - Richtung: Long/Short
  - Indikatoren: True Strength Index
  - Stops: Nein
  - Komplexität: Niedrig
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Niedrig
