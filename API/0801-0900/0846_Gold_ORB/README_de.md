# Gold-ORB-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie erfasst das Hoch und Tief der Asiensitzung und handelt Ausbrüche in den folgenden Stunden. Stops und Ziele werden aus der Bandbreitengröße mit einem Ertragsvielfachen abgeleitet.

## Details

- **Einstiegskriterien**:
  - Während des Handelsfensters Long gehen, wenn der Preis über das erfasste Asienhoch schließt.
  - Short gehen, wenn der Preis unter das erfasste Asientief schließt.
- **Long/Short**: Beide
- **Ausstiegskriterien**:
  - Stop und Ziel basierend auf der Bandbreitengröße und dem Ertragsvielfachen.
- **Stops**: Ja
- **Standardwerte**:
  - `AsiaStart` = 00:00
  - `AsiaEnd` = 06:00
  - `TradeStart` = 06:00
  - `TradeEnd` = 10:00
  - `RewardMultiplier` = 2.0
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: Keine
  - Stops: Ja
  - Komplexität: Niedrig
  - Zeitrahmen: 5m
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

