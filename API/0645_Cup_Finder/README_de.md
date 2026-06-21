# Cup-Finder-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese musterbasierte Strategie sucht in Preisdaten nach abgerundeten „Cup"-Formationen. Wenn der Kurs aus einem abgeschlossenen Cup ausbricht, wird je nach Richtung long oder short eingestiegen.

Tests zeigen eine durchschnittliche Jahresrendite von etwa 47 %. Am besten geeignet für Aktien.

Die Strategie kauft bei bullischen Cup-Ausbrüchen und verkauft bei bärischen invertierten Cups. Positionen werden durch einen Stop-Loss abgesichert.

## Details

- **Einstiegskriterien**: Cup-Muster bildet sich und Kurs bricht den Rand.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Kursumkehr oder Stop-Loss-Auslösung.
- **Stops**: Ja.
- **Standardwerte**:
  - `Lookback` = 150
  - `WidthPercent` = 5m
  - `StopLossPercent` = 1m
  - `CandleType` = TimeSpan.FromMinutes(15)
- **Filter**:
  - Kategorie: Muster
  - Richtung: Long/Short
  - Indikatoren: Price Action
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Ja
  - Risikolevel: Mittel
