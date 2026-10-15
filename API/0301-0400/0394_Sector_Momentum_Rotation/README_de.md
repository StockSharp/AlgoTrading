# Sektor-Momentum-Rotations-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie **Sektor-Momentum-Rotation** rotiert Kapital zwischen Sektor-ETFs. Am Ende jedes Monats wird die vergangene Rendite jedes Sektors über mehrere Lookback-Fenster berechnet. Das System kauft die stärksten Sektoren und verlässt schwächere, wobei die Exposition nur auf die Top-Performer beschränkt bleibt.

## Details
- **Einstiegskriterien**: Monatliches Ranking des Momentums von Sektor-ETFs.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**: Monatliches Rebalancing bei Rankingänderungen.
- **Stops**: Kein expliziter Stop.
- **Standardwerte**:
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filter**:
  - Kategorie: Momentum
  - Richtung: Long
  - Indikatoren: Preisbasiert
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Ja
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
