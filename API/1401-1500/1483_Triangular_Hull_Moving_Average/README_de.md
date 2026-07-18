# Triangularer Hull Moving Average-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie basierend auf dem Kreuzung des Hull Moving Average mit einem Zwei-Balken-Verzug.

Die Strategie vergleicht den Hull Moving Average mit seinem Wert vor zwei Balken. Ein Aufwärtskreuzung eröffnet eine Long-Position, eine Abwärtskreuzung eine Short-Position. Die Richtung kann auf Long-only oder Short-only beschränkt werden.

## Details
- **Einstiegskriterien**: HMA-Kreuzung mit 2-Balken-Verzug.
- **Long/Short**: Konfigurierbar.
- **Ausstiegskriterien**: Gegensignal oder Richtungsfilter.
- **Stops**: Nein.
- **Standardwerte**:
  - `Length` = 40
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `EntryMode` = EntryDirection.LongAndShort
- **Filter**:
  - Kategorie: Trend
  - Richtung: Konfigurierbar
  - Indikatoren: MA
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
