# Rendite-Asymmetrie-Strategie für Rohstoffe
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie **Rendite-Asymmetrie für Rohstoffe** nutzt den Unterschied zwischen positiven und negativen Renditen. Für jeden Rohstoff-Future summiert das rollende Fenster alle Aufwärts- und Abwärtsbewegungen separat. Ein hohes Verhältnis deutet auf einen anhaltend positiven Drift hin, während ein niedriges Verhältnis auf anhaltenden Verkaufsdruck hindeutet.

Zu Beginn jedes Monats werden Rohstoffe nach diesem Asymmetriemaß gerankt. Das System kauft die N besten Kontrakte und verkauft die N schwächsten leer, wobei das Kapital gleichmäßig verteilt wird. Das Rebalancing erfolgt monatlich.

## Details
- **Einstiegskriterien**: Monatliches Ranking der Asymmetrie der täglichen Renditen über ein Lookback-Fenster.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Positionen beim monatlichen Rebalancing angepasst.
- **Stops**: Kein expliziter Stop; Positionsgröße durch `MinTradeUsd` begrenzt.
- **Standardwerte**:
  - `WindowDays = 120`
  - `TopN = 5`
  - `MinTradeUsd = 200`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filter**:
  - Kategorie: Momentum
  - Richtung: Beide
  - Indikatoren: Preisbasiert
  - Stops: Nein
  - Komplexität: Mittel
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Ja
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
