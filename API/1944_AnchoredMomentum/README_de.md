# AnchoredMomentum
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die AnchoredMomentum-Strategie berechnet das Verhältnis zwischen EMA und SMA der Schlusskurse von Kerzen. Wenn das Momentum über einen oberen Schwellenwert steigt, werden Long-Positionen eröffnet; wenn es unter einen unteren Schwellenwert fällt, werden Short-Positionen eröffnet. Entgegengesetzte Signale schließen die aktuellen Positionen.

## Details

- **Einstiegskriterien**: Momentum kreuzt `UpLevel` nach oben für Long, `DownLevel` nach unten für Short.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Das entgegengesetzte Signal schließt die Position.
- **Stops**: Nein.
- **Standardwerte**:
  - `SmaPeriod` = 8
  - `EmaPeriod` = 6
  - `UpLevel` = 0.025m
  - `DownLevel` = -0.025m
  - `CandleType` = TimeSpan.FromHours(4)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: SMA, EMA
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: 4h
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
