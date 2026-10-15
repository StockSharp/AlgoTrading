# IBS Internal Bar Strength-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

IBS Internal Bar Strength ist eine Mean-Reversion-Strategie, die den Schlusskurs der vorherigen Kerze innerhalb ihrer Range nutzt, um überkaufte oder überverkaufte Bedingungen zu erkennen. Ein optionaler EMA-Filter richtet die Trades an der Trendrichtung aus, und Einstiege werden nur erlaubt, wenn sich der Preis um einen Mindestprozentsatz vom letzten Einstieg entfernt. Positionen werden geschlossen, wenn der IBS den entgegengesetzten Schwellenwert kreuzt oder die maximale Haltedauer erreicht wird.

## Details
- **Daten**: Kurskerzen.
- **Einstiegskriterien**:
  - **Long**: IBS unterhalb des Einstiegs-Schwellenwerts, EMA-Bedingung erfüllt und Richtung erlaubt.
  - **Short**: IBS oberhalb des Ausstiegs-Schwellenwerts, EMA-Bedingung erfüllt und Richtung erlaubt.
- **Ausstiegskriterien**: IBS kreuzt den entgegengesetzten Schwellenwert oder Handelsdauerlimit.
- **Stops**: Zeitbasierter Ausstieg.
- **Standardwerte**:
  - `IbsEntryThreshold` = 0.09
  - `IbsExitThreshold` = 0.985
  - `EmaPeriod` = 220
  - `MinEntryPct` = 0
  - `MaxTradeDuration` = 14
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Long & Short
  - Indikatoren: IBS, EMA
  - Komplexität: Niedrig
  - Risikolevel: Mittel
