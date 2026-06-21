# Wie man Backtest-Zeitbereiche festlegt
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie demonstriert die Einschränkung des Handels auf bestimmte Datums- und Intraday-Zeitfenster. Sie eröffnet eine Long-Position, wenn ein schneller SMA einen langsamen SMA von unten kreuzt, und schließt beim entgegengesetzten Crossover.

## Details
- **Daten**: Kurskerzen.
- **Einstiegskriterien**:
  - **Long**: Schneller SMA kreuzt langsamen SMA von unten innerhalb der ausgewählten Datums- und Eintrittszeitbereiche.
- **Ausstiegskriterien**: Schneller SMA kreuzt langsamen SMA von oben innerhalb der ausgewählten Datums- und Austrttszeitbereiche.
- **Stops**: Keine.
- **Standardwerte**:
  - `FastLength` = 14
  - `SlowLength` = 28
  - `FromDate` = 2021-01-01
  - `ThruDate` = 2112-01-01
  - `EntryStart` = 00:00
  - `EntryEnd` = 00:00
  - `ExitStart` = 00:00
  - `ExitEnd` = 00:00
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Long
  - Indikatoren: SMA
  - Komplexität: Niedrig
  - Risikolevel: Mittel
