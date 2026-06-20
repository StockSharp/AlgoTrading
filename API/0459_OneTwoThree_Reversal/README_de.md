# Eins-Zwei-Drei Umkehr-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Eins-Zwei-Drei Umkehr-Strategie sucht nach einem bullischen 1-2-3-Muster in der Preisentwicklung. Eine Long-Position wird eröffnet, wenn das heutige Tief unter dem gestrigen liegt, das gestrige Tief unter dem Tief vor drei Bars liegt, das Tief vor zwei Bars unter dem Tief vor vier Bars liegt und das Hoch vor zwei Bars unter dem Hoch vor drei Bars liegt. Der Trade wird nach einer definierten Anzahl von Bars oder wenn der Preis über einem gleitenden Durchschnitt schließt, geschlossen.

## Details

- **Einstiegskriterien:**
  - Aktuelles Tief < vorheriges Tief.
  - Vorheriges Tief < Tief vor drei Bars.
  - Tief vor zwei Bars < Tief vor vier Bars.
  - Hoch vor zwei Bars < Hoch vor drei Bars.
- **Long/Short:** Nur Long.
- **Ausstiegskriterien:**
  - Halten für `DaysToHold` Bars oder Schlusskurs überquert den gleitenden Durchschnitt nach oben.
- **Stops:** Keine.
- **Standardwerte:**
  - `DaysToHold` = 7
  - `MaLength` = 200
- **Filter:**
  - Kategorie: Umkehr
  - Richtung: Nur Long
  - Indikatoren: Price action, SMA
  - Stops: Nein
  - Komplexität: Niedrig
  - Zeitrahmen: Täglich
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
