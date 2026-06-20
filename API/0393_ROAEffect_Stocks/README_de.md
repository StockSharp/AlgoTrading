# ROA-Effekt-Strategie für Aktien
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie **ROA-Effekt für Aktien** zielt auf Aktien mit hoher Kapitalrendite (ROA) ab. Ein externer Fundamentaldaten-Feed liefert die ROA-Werte für das Handelsuniversum. Zu Beginn jedes Monats werden die Aktien nach ROA gerankt, und das Portfolio geht Long im oberen Dezil und Short im unteren Dezil.

Positionen werden gleichgewichtet und monatlich rebalanciert, um die Tendenz profitabler Unternehmen zur Outperformance zu nutzen.

## Details
- **Einstiegskriterien**: Monatliches Ranking anhand externer ROA-Daten.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Monatliches Rebalancing.
- **Stops**: Kein expliziter Stop.
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
