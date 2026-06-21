# Turn Around Tuesday on Steroids-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Eine saisonale Long-Strategie, die nach zwei aufeinanderfolgenden negativen Tagen zu Wochenbeginn kauft und beim Ausbruch über das vorherige Hoch aussteigt. Ein optionaler gleitender Durchschnitt bestätigt die Trendrichtung.

## Details

- **Einstiegskriterien**: erster oder zweiter Wochentag mit zweitägigem Rückgang
- **Long/Short**: Long
- **Ausstiegskriterien**: Schluss über dem vorherigen Hoch
- **Stops**: Keine
- **Standardwerte**:
  - `StartingDay` = Sunday
  - `MaPeriod` = 200
- **Filter**:
  - Kategorie: Saisonalität
  - Richtung: Nur Long
  - Indikatoren: SMA
  - Stops: Nein
  - Komplexität: Anfänger
  - Zeitrahmen: Täglich
  - Saisonalität: Ja
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Niedrig
