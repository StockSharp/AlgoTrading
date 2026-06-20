# Balance-of-Power-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Balance-of-Power-Strategie bewertet die Stärke von Bullen gegenüber Bären innerhalb jeder Kerze, indem der Schluss mit der Handelsspanne verglichen wird. Wenn dieser Wert über einen positiven Schwellenwert steigt, signalisiert dies starken Kaufdruck.

Die Strategie eröffnet eine Long-Position, wenn der Balance of Power den definierten `Threshold` nach oben kreuzt, und schließt sie, wenn er unter den negativen Schwellenwert fällt.

## Details

- **Einstiegskriterien**:
  - Balance of Power kreuzt über `Threshold`.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**:
  - Balance of Power kreuzt unter `-Threshold`.
- **Stops**: Keine.
- **Standardwerte**:
  - `Threshold` = 0.8
- **Filter**:
  - Kategorie: Momentum
  - Richtung: Long
  - Indikatoren: Balance of Power
  - Stops: Keine
  - Komplexität: Niedrig
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
