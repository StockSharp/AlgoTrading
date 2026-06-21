# Color JLaguerre
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie basierend auf dem farbcodierten Laguerre-Oszillator.

Der Indikator glättet die Kursbewegung mit einem Jurik-Filter und färbt seine Linie entsprechend der Position innerhalb vordefinierter Niveaus. Ein Farbwechsel markiert eine potenzielle Trendumkehr.

Die Strategie kauft, wenn der Oszillator das mittlere Niveau nach oben kreuzt, und verkauft, wenn er es nach unten kreuzt. Positionen werden geschlossen, wenn der Oszillator extreme Niveaus erreicht oder ein entgegengesetztes Signal erscheint.

## Details

- **Einstiegskriterien**: Farbwechsel des Laguerre-Oszillators um das mittlere Niveau.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Gegensignal oder Erreichen eines extremen Niveaus.
- **Stops**: Ja.
- **Standardwerte**:
  - `RsiLength` = 14
  - `HighLevel` = 85
  - `MiddleLevel` = 50
  - `LowLevel` = 15
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromHours(1)
- **Filter**:
  - Kategorie: Oszillator
  - Richtung: Beide
  - Indikatoren: RSI
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Stündlich (1h)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
