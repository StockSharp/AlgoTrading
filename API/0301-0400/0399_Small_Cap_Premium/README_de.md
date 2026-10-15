# Small-Cap-Prämien-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie **Small-Cap-Prämie** nutzt die historische Tendenz von Aktien mit geringer Marktkapitalisierung, Large Caps zu übertreffen. Das Universum wird nach Marktkapitalisierung aufgeteilt, und das Portfolio hält einen Korb aus Small Caps, während ein Large-Cap-Index geshortet wird.

## Details
- **Einstiegskriterien**: Auswahl nach Marktkapitalisierungs-Ranking.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Periodisches Rebalancing.
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
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
