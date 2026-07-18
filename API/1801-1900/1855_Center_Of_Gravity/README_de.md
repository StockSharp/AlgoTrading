# Schwerpunkt-Strategie (Center of Gravity)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie basierend auf dem Center of Gravity-Indikator, der SMA und WMA multipliziert und das Ergebnis glättet. Eine Long-Position wird eröffnet, wenn die Mittellinie ihre geglättete Durchschnittslinie nach oben kreuzt, und eine Short-Position beim entgegengesetzten Crossover. Positionen werden geschlossen, wenn das Signal gegen die aktuelle Richtung wechselt.

## Details

- **Einstiegskriterien**: Mittellinie kreuzt ihre geglättete Durchschnittslinie
- **Long/Short**: Beide
- **Ausstiegskriterien**: Signal wechselt die Seite
- **Stops**: Nein
- **Standardwerte**:
  - `CandleType` = H4
  - `Period` = 10
  - `SmoothPeriod` = 3
- **Filter**:
  - Kategorie: Indikator
  - Richtung: Beide
  - Indikatoren: SMA, WMA
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
