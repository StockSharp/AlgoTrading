# Dollar-Carry-Trade
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **Dollar-Carry-Trade**-Strategie ordnet USD-Währungspaare nach Zinsdifferenzial und geht Long USD gegen Niedrig-Carry-Währungen und Short gegen Hoch-Carry-Währungen. Monatliches Rebalancing am ersten Handelstag.

## Details
- **Einstiegskriterien**: Ranking nach Carry; Long Niedrig-Carry, Short Hoch-Carry.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Monatliches Rebalancing.
- **Stops**: Kein expliziter Stop.
- **Standardwerte**:
  - `K = 3`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filter**:
  - Kategorie: Fundamental
  - Richtung: Beide
  - Indikatoren: Rates
  - Stops: Nein
  - Komplexität: Mittel
  - Zeitrahmen: Täglich
  - Saisonalität: Ja
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
