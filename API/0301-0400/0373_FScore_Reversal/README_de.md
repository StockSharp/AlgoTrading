# F-Score-Umkehr-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie kombiniert den Piotroski F-Score-Fundamentalansatz mit kurzfristiger Preisumkehr. Jeden Monat kauft sie die am schlechtesten performende Aktie unter jenen mit hohem F-Score und geht optional bei der besten Aktie mit niedrigem F-Score short. Die Annahme ist, dass fundamental solide Unternehmen nach vorübergehenden Rückgängen zurückprallen, während schwache Unternehmen nach Rallyes umkehren.

Am ersten Handelstag des Monats stuft der Algorithmus das Universum nach der Einmonatsrendite ein. Er geht bei dem Wertpapier mit der niedrigsten Rendite und `FScore >= FHi` long und shortet, falls verfügbar, das Wertpapier mit der höchsten Rendite und `FScore <= FLo`. Positionen werden einen Monat gehalten.

## Details

- **Einstiegskriterien**:
  - Long: unter Wertpapieren mit `FScore >= FHi` dasjenige mit der niedrigsten `Lookback`-Rendite kaufen, wenn Handelsgröße >= `MinTradeUsd`.
  - Short (optional): unter Wertpapieren mit `FScore <= FLo` dasjenige mit der höchsten `Lookback`-Rendite leerverkaufen.
- **Long/Short**: Long und Short.
- **Ausstiegskriterien**: Alle Positionen beim nächsten monatlichen Rebalancing schließen.
- **Stops**: Keine.
- **Standardwerte**:
  - `Universe` – zu bewertende Wertpapiere.
  - `Lookback` = 21 Tage.
  - `FHi` = 7.
  - `FLo` = 3.
  - `CandleType` = 1 Tag.
  - `MinTradeUsd` – Mindesttransaktionswert.
- **Filter**:
  - Kategorie: Mean Reversion.
  - Richtung: Long und Short.
  - Zeitrahmen: Kurzfristig.
  - Rebalancing: Monatlich.

