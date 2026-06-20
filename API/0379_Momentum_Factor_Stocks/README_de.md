# Momentum-Faktor-Aktien-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Dieser systematische Ansatz nutzt den klassischen 12-1-Monats-Momentum-Faktor bei Aktien. Am Ende jedes Monats werden Aktien nach ihrer Performance über die vorangegangenen zwölf Monate eingestuft, wobei der jüngste Monat ausgelassen wird, um kurzfristige Umkehrungen zu umgehen. Wertpapiere im höchsten Quintil werden gekauft und jene im niedrigsten Quintil leerverkauft, wodurch ein marktneutraler Spread gebildet wird.

Das Rebalancing erfolgt am ersten Handelstag jedes Monats. Positionen sind gleichgewichtet und bleiben bis zum nächsten Rebalancing offen; es werden keine expliziten Stop-Losses verwendet.

Umfangreiche akademische und industrielle Forschung zeigt, dass Momentum beständige Überrenditen liefert und wertvolle Diversifikation bietet, wenn es mit anderen Faktoren kombiniert wird.

## Details

- **Einstiegskriterien**: Monatliches 12-1-Momentum-Ranking; Long oberstes Quintil,
  Short unterstes Quintil
- **Long/Short**: Beide
- **Ausstiegskriterien**: Nächstes monatliches Rebalancing
- **Stops**: Nein
- **Standardwerte**:
  - `LookbackDays` = 252
  - `SkipDays` = 21
  - `Quintile` = 5
  - `MinTradeUsd` = 200
  - `CandleType` = TimeSpan.FromDays(1)
- **Filter**:
  - Kategorie: Momentum
  - Richtung: Beide
  - Indikatoren: Kursveränderung
  - Stops: Nein
  - Komplexität: Mittel
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
