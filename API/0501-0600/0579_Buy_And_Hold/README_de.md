# Kaufen-und-Halten-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie eröffnet eine einzelne Long-Position zum angegebenen Startdatum und hält sie bis zum Enddatum, wodurch ein einfacher Kaufen-und-Halten-Ansatz umgesetzt wird.

## Details

- **Einstiegskriterien**:
  - Wenn die Kerzenzeit am oder nach dem Startdatum liegt, kauft die Strategie einmalig.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**:
  - Wenn die Kerzenzeit das Enddatum erreicht oder überschreitet, wird die Position geschlossen.
- **Stops**: Keine.
- **Standardwerte**:
  - Startdatum = 2018-01-01.
  - Enddatum = 2069-12-31.
- **Filter**:
  - Kategorie: Buy and Hold.
  - Richtung: Long.
  - Indikatoren: Keine.
  - Stops: Nein.
  - Komplexität: Niedrig.
  - Zeitrahmen: Beliebig.
  - Saisonalität: Nein.
  - Neuronale Netze: Nein.
  - Divergenz: Nein.
  - Risikolevel: Hoch.
