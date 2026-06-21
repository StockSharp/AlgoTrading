# EMA-Crossover-Trailing-Stop-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **EMA-Crossover-Trailing-Stop-Strategie** eröffnet eine Long-Position, wenn die kurze EMA die lange EMA nach oben kreuzt, und eine Short-Position, wenn sie nach unten kreuzt. Ein Trailing Stop, der auf dem höchsten oder niedrigsten Preis nach dem Einstieg basiert, schließt die Position, wenn der Preis um einen festgelegten Prozentsatz zurückläuft.

## Details
- **Einstiegskriterien**: Kreuzung der kurzen EMA über der langen EMA.
- **Long/Short**: beide Richtungen.
- **Ausstiegskriterien**: entgegengesetzte Kreuzung oder Trailing Stop.
- **Stops**: Trailing Stop, der den höchsten/niedrigsten Preis seit dem Einstieg verwendet.
- **Standardwerte**:
  - `ShortLength = 9`
  - `LongLength = 21`
  - `TrailStopPercent = 1`
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: EMA
  - Stops: Trailing Stop
  - Komplexität: Grundlegend
  - Zeitrahmen: Kurzfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
