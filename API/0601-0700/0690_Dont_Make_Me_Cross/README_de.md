# Bring Mich Nicht zum Kreuzen
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

EMA-Crossover-Strategie mit vertikaler Verschiebung.

## Details

- **Einstiegskriterien**:
  - **Long**: Verschobene kurze EMA kreuzt über die verschobene lange EMA.
  - **Short**: Verschobene kurze EMA kreuzt unter die verschobene lange EMA.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Entgegengesetzter Crossover.
- **Stops**: Nein.
- **Standardwerte**:
  - `ShortEmaLength` = 9
  - `LongEmaLength` = 21
  - `ShiftAmount` = -50
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: EMA
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (1m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
