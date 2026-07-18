# Three Down Three Up-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie kauft nach einer bestimmten Anzahl aufeinanderfolgender negativer Schlusskurse und schließt die Position nach einer Reihe positiver Schlusskurse. Ein optionaler EMA-Filter erlaubt Einstiege nur, wenn der Preis über dem gleitenden Durchschnitt liegt.

## Details

- **Einstiegskriterien**: Der Preis schließt N Bars lang tiefer als der vorherige Bar. Optionale Bedingung: Preis über der EMA.
- **Ausstiegskriterien**: Der Preis schließt M Bars lang höher als der vorherige Bar.
- **Long/Short**: Nur Long.
- **Stops**: Keine.
- **Standardwerte**: Kaufauslöser = 3, Verkaufsauslöser = 3, EMA-Periode = 200.
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Long
  - Indikatoren: EMA (optional)
  - Stops: Nein
  - Komplexität: Niedrig
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
