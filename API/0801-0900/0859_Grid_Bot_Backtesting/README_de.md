# Grid-Bot-Backtesting-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Implementiert einen Grid-Trading-Bot, der Long-Positionen akkumuliert, wenn der Preis auf Grid-Levels fällt, und diese schließt, wenn der Preis zur nächsten Linie steigt. Grenzen können manuell gesetzt oder aus aktuellen Daten berechnet werden.

## Details

- **Einstiegskriterien**:
  - **Long**: Preis fällt unter eine Grid-Linie ohne aktive Order
- **Long/Short**: Nur Long
- **Ausstiegskriterien**:
  - Preis steigt über die nächste Grid-Linie
- **Stops**: Keine
- **Standardwerte**:
  - `AutoBounds` = true
  - `BoundSource` = "Hi & Low"
  - `BoundLookback` = 250
  - `BoundDeviation` = 0.10
  - `UpperBound` = 0.285
  - `LowerBound` = 0.225
  - `GridLines` = 30
- **Filter**:
  - Kategorie: Range-Trading
  - Richtung: Nur Long
  - Indikatoren: Highest, Lowest, SimpleMovingAverage
  - Stops: Nein
  - Komplexität: Moderat
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
