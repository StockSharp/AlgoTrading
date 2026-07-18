# 2:45 Uhr Kerzen-Ausbruch-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Intraday-Strategie beobachtet die 2:45-Uhr-Kerze und handelt Ausbrüche ihres Hochs oder Tiefs innerhalb der nächsten paar Bars. Wenn der Preis das Hoch der Kerze übersteigt, wird eine Long-Position eingegangen; wenn der Preis unter das Tief der Kerze fällt, wird eine Short-Position eröffnet. Positionen werden am Ende des Beobachtungsfensters geschlossen, wenn kein entgegengesetzter Ausbruch auftritt.

## Details

- **Einstiegskriterien**:
  - **Long**: Preis bricht innerhalb der nächsten `LookForwardBars` Kerzen über das Hoch der 2:45-Uhr-Kerze.
  - **Short**: Preis bricht innerhalb der nächsten `LookForwardBars` Kerzen unter das Tief der 2:45-Uhr-Kerze.
- **Long/Short**: Beide.
- **Ausstiegskriterien**:
  - Ende des Beobachtungsfensters oder entgegengesetzter Ausbruch.
- **Stops**: Keine.
- **Standardwerte**:
  - `TargetHour` = 2
  - `TargetMinute` = 45
  - `LookForwardBars` = 2
  - `CandleType` = 45-Minuten-Kerzen
- **Filter**:
  - Kategorie: Zeitbasierter Ausbruch
  - Richtung: Beide
  - Indikatoren: Keine
  - Stops: Nein
  - Komplexität: Niedrig
  - Zeitrahmen: Intraday
  - Saisonalität: Ja
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
