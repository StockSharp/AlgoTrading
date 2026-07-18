# Schiefe-Strategie für Rohstoff-Futures
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie **Schiefe bei Rohstoffen** rankt Rohstoff-Futures nach der Schiefe ihrer Renditeverteilung. Kontrakte mit positiver Schiefe werden für Long-Positionen bevorzugt, während solche mit stark negativer Schiefe geshortet werden, in der Annahme, dass extreme Abwärtsbewegungen zum Mittelwert zurückkehren.

## Details
- **Einstiegskriterien**: Ranking nach historischer Renditeschiefe.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Periodisches Rebalancing.
- **Stops**: Kein expliziter Stop.
- **Standardwerte**:
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filter**:
  - Kategorie: Statistisch
  - Richtung: Beide
  - Indikatoren: Preisbasiert
  - Stops: Nein
  - Komplexität: Mittel
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
