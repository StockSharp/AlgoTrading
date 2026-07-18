# Neon Momentum-Wellen-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Neon Momentum-Wellen-Strategie nutzt MACD-Histogramm-Kreuzungen für Trades in beide Richtungen. Die Strategie geht Long, wenn das Histogramm das Einstiegsniveau (Standard: null) nach oben kreuzt, und Short, wenn es nach unten kreuzt. Positionen werden geschlossen, wenn das Histogramm die konfigurierten Ausstiegsniveaus erreicht.

## Details

- **Einstiegskriterien**: MACD-Histogramm kreuzt das Einstiegsniveau.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Histogramm kreuzt Long-/Short-Ausstiegsniveaus.
- **Stops**: Nein.
- **Standardwerte**:
  - `FastLength` = 12
  - `SlowLength` = 26
  - `SignalLength` = 20
  - `EntryLevel` = 0
  - `LongExitLevel` = 11
  - `ShortExitLevel` = -9
  - `CandleType` = 1 minute
- **Filter**:
  - Kategorie: Momentum
  - Richtung: Beide
  - Indikatoren: MACD
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (1m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
