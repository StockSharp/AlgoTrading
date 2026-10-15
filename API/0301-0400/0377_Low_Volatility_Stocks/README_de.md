# Strategie für Aktien mit Niedriger Volatilität
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Dieser defensive Aktienfaktor sucht die "Low-Volatility-Anomalie" — die Beobachtung, dass Aktien mit ruhigeren Kursbewegungen oft überlegene risikoadjustierte Renditen liefern. Die Volatilität wird als Standardabweichung der Tagesrenditen über ein zurückliegendes Fenster (standardmäßig 60 Handelstage) berechnet.

Am ersten Handelstag eines jeden Monats wird das Universum nach realisierter Volatilität eingestuft. Die Strategie geht im niedrigsten Volatilitätsdezil long und im höchsten Dezil short, mit gleichen Dollar-Gewichtungen innerhalb jedes Bereichs. Positionen werden bis zum nächsten monatlichen Rebalancing gehalten, und es werden keine expliziten Stop-Losses verwendet.

Backtests zeigen eine glattere Ertragskurve und geringere Drawdowns als der breite Markt, was den Ansatz für Anleger attraktiv macht, die Aktienengagement mit reduziertem Risiko suchen.

## Details

- **Einstiegskriterien**: Monatliche Sortierung nach zurückliegender Volatilität; Long niedrigstes Dezil,
  Short höchstes Dezil
- **Long/Short**: Beide
- **Ausstiegskriterien**: Nächstes monatliches Rebalancing
- **Stops**: Nein
- **Standardwerte**:
  - `VolWindowDays` = 60
  - `Deciles` = 10
  - `MinTradeUsd` = 200
  - `CandleType` = TimeSpan.FromDays(1)
- **Filter**:
  - Kategorie: Volatilität
  - Richtung: Beide
  - Indikatoren: Standardabweichung
  - Stops: Nein
  - Komplexität: Mittel
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Niedrig
