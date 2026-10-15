# Bar-Range-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Bar Range Strategie geht Long, wenn die Range der aktuellen Bar zu den höchsten der letzten Bars gehört und die Kerze unterhalb ihres Eröffnungskurses schließt. Die Position wird nach einer festen Anzahl von Bars geschlossen.

## Details

- **Einstiegskriterien**:
  - Range = High − Low
  - Prozentrang der Range über `LookbackPeriod` ≥ `PercentRankThreshold`
  - Close < Open
- **Ausstiegskriterien**: Position nach `ExitBars` Bars schließen.
- **Standardwerte**:
  - `LookbackPeriod` = 50
  - `PercentRankThreshold` = 95
  - `ExitBars` = 1
- **Filter**:
  - Kategorie: Volatilität
  - Richtung: Long
  - Indikatoren: Percent Rank
  - Stops: Nein
  - Komplexität: Niedrig
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
