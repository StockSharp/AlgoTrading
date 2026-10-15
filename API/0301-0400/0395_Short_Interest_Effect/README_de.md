# Short-Interest-Effekt-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie **Short-Interest-Effekt** nutzt Short-Interest-Niveaus zur Vorhersage der Aktienperformance. Wertpapiere mit geringer Tage-bis-Eindeckung tendieren dazu, stark geshortete Titel zu übertreffen. In monatlichem Abstand werden Aktien nach Short Interest sortiert, und das Portfolio kauft die Gruppe mit dem niedrigsten Wert, während die Gruppe mit dem höchsten Wert geshortet wird.

## Details
- **Einstiegskriterien**: Monatliches Ranking nach Short-Interest-Ratio oder Tagen bis zur Eindeckung.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Monatliches Rebalancing.
- **Stops**: Kein expliziter Stop.
- **Standardwerte**:
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filter**:
  - Kategorie: Fundamental
  - Richtung: Beide
  - Indikatoren: Fundamentaldaten
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Ja
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
