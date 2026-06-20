# Residualer Momentum-Faktor-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie **Residualer Momentum-Faktor** bewertet Wertpapiere anhand eines externen residualen Momentum-Scores.
Jeden Monat am ersten Handelstag werden Long-Positionen im oberen Dezil und Short-Positionen im unteren Dezil eingegangen.

## Details
- **Einstiegskriterien**: externer Datenfeed für residuales Momentum.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Monatliches Rebalancing.
- **Stops**: Keine explizite Stop-Logik.
- **Standardwerte**:
  - `Decile = 10`
  - `MinTradeUsd = 200`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filter**:
  - Kategorie: Fundamental
  - Richtung: Beide
  - Indikatoren: Fundamentaldaten
  - Stops: Nein
  - Komplexität: Mittel
  - Zeitrahmen: Täglich
  - Saisonalität: Ja
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
