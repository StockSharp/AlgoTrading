# Volumenbasierte Gold-Einstiegs-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie kauft, wenn zwei aufeinanderfolgende bullische Volumenbars den Volumen-Moving-Average überschreiten. Der zweite Bar muss außerdem ein höheres Volumen als der erste aufweisen. Ein festes Gewinnziel schließt die Position, sobald sich der Preis um einen vordefinierten Betrag in die gewünschte Richtung bewegt.

## Details

- **Einstiegskriterien**:
  - Zwei bullische Volumenbars über dem Volumen-Moving-Average mit steigendem Volumen.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**:
  - Festes Gewinnziel bei `entry price + Target Move`.
- **Stops**: Keine.
- **Standardwerte**:
  - `Volume MA Period` = 20.
  - `Target Move` = 5.
- **Filter**:
  - Kategorie: Volumen
  - Richtung: Long
  - Indikatoren: Einzeln
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Niedrig
