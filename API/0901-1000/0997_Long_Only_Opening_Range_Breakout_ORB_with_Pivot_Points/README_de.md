# Nur-Long Eröffnungsbereich-Ausbruch (ORB) mit Pivot-Punkten
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie kauft, wenn der Kurs das Hoch der Eröffnungsrange durchbricht und der erste Widerstand (R1) aus den täglichen Pivot-Punkten über diesem Hoch liegt. Ein Trailing-Stop folgt den Pivot-Niveaus.

## Details

- **Einstiegskriterien**:
  - Nach der Eröffnungsrange Long-Einstieg beim Ausbruch über das Sitzungshoch, wenn R1 höher liegt.
- **Long/Short**: Nur Long
- **Ausstiegskriterien**:
  - Trailing-Stop an Pivot-Niveaus und Tagesschluss angepasst.
- **Stops**: Ja
- **Standardwerte**:
  - `RangeMinutes` = 15
  - `SessionStart` = 09:30
  - `MaxTradesPerDay` = 1
  - `StopLossPercent` = 3
  - `InitialSlType` = Percentage
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Long
  - Indikatoren: Pivot Points
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
