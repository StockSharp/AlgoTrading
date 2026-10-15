# Hull Suite by MRS Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Eine Trendfolge-Strategie, die den ausgewählten Hull-Typ gleitenden Durchschnitt mit seinem Wert vor zwei Bars vergleicht. Long-Positionen werden eröffnet, wenn der Durchschnitt über seinen Zwei-Bar-alten Wert steigt, und Short-Positionen, wenn er darunter fällt.

## Details

- **Einstiegskriterien**:
  - **Long**: `MA > MA[2]`.
  - **Short**: `MA < MA[2]`.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Umkehr bei entgegengesetztem Signal.
- **Stops**: Keine.
- **Standardwerte**:
  - `Length` = 55
  - `Mode` = Hma
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Hull MA
  - Stops: Keine
  - Komplexität: Einfach
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
