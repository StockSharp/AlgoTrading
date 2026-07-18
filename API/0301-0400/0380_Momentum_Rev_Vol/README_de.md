# Momentum-Reversal-Volatilität-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Komposit-Faktor-Strategie kombiniert drei Signale: langfristiges Momentum,
kurzfristige Umkehr und niedrige Volatilität. Jeden Monat wird für jedes Wertpapier
ein Score berechnet, der das 12-Monats-Momentum, den Kehrwert der Einmonatsrenditen
und die nachgelagerte 60-Tage-Volatilität verwendet. Die einstellbaren Gewichte `WM`,
`WR` und `WV` steuern den Beitrag jeder Komponente.

Am ersten Handelstag jedes Monats werden die Wertpapiere nach dem Komposit-Score
gerankt. Das oberste Dezil wird gekauft und das unterste Dezil wird leerverkauft, mit
gleichen Dollar-Gewichten. Positionen werden bis zur nächsten Neugewichtung gehalten,
und es werden keine expliziten Stop-Loss-Regeln angewendet.

Durch die Kombination von Trendfolge, Mean Reversion und Risikoaversion strebt die
Strategie diversifizierte Renditen in verschiedenen Marktregimes an.

## Details

- **Einstiegskriterien**: Monatliches Ranking nach gewichteter Kombination aus Momentum,
  Umkehr und Volatilität; Long oberstes Dezil, Short unterstes Dezil
- **Long/Short**: Beide
- **Ausstiegskriterien**: Nächste monatliche Neugewichtung
- **Stops**: Nein
- **Standardwerte**:
  - `Lookback12` = 252
  - `Lookback1` = 21
  - `VolWindow` = 60
  - `WM` = 1.0
  - `WR` = 1.0
  - `WV` = 1.0
  - `MinTradeUsd` = 200
  - `CandleType` = TimeSpan.FromDays(1)
- **Filter**:
  - Kategorie: Multi-Faktor
  - Richtung: Beide
  - Indikatoren: Momentum, Umkehr, Volatilität
  - Stops: Nein
  - Komplexität: Fortgeschritten
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
