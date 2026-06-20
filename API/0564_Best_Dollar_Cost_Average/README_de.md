# Optimale Dollar-Cost-Averaging-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie baut eine Position auf, indem sie einen festen Kapitalbetrag in regelmäßigen Abständen zwischen benutzerdefinierten Start- und Enddaten investiert. Jeder Kauf erfolgt zum Schlusskurs des gewählten Zeitrahmens unabhängig vom Preis und implementiert einen klassischen Dollar-Cost-Averaging-Ansatz.

## Details

- **Einstiegskriterien**:
  - In jedem Intervall (täglich, wöchentlich oder monatlich) zwischen Start- und Enddatum kauft die
    Strategie zum Schlusskurs für den konfigurierten Betrag.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**:
  - Positionen werden gehalten; keine automatische Ausstiegslogik enthalten.
- **Stops**: Keine.
- **Standardwerte**:
  - Betrag pro Periode = 100.
  - Intervall = Wöchentlich.
  - Startdatum = 2018-01-01, Enddatum = 2020-01-28.
- **Filter**:
  - Kategorie: Akkumulation.
  - Richtung: Long.
  - Indikatoren: Keine.
  - Stops: Nein.
  - Komplexität: Niedrig.
  - Zeitrahmen: Beliebig.
  - Saisonalität: Nein.
  - Neuronale Netze: Nein.
  - Divergenz: Nein.
  - Risikolevel: Niedrig.
