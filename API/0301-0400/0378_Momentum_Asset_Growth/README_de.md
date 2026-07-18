# Momentum-Vermögenswachstum-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese hybride Faktorstrategie verbindet Kurs-Momentum mit dem Vermögenswachstumseffekt. Unternehmen, die ihre Bilanzen schnell ausbauen und gleichzeitig starke Kurstrends zeigen, werden vom Markt oft belohnt. Der Ansatz filtert zunächst das Universum nach Unternehmen im obersten Dezil des Vermögenswachstums.

Die in Frage kommenden Aktien werden dann nach Zwölf-Monats-Momentum eingestuft, wobei der jüngste Monat ausgeschlossen wird, um kurzfristige Umkehrungen zu vermeiden. Das oberste Momentum-Quintil wird gekauft, während das unterste Quintil leerverkauft wird. Das Rebalancing findet am ersten Handelstag jedes Monats statt, außer im Januar, wenn die Strategie pausiert. Zwischen den Reviews werden keine Stop-Losses angewendet.

Backtests auf entwickelten Aktien zeigen, dass die Kombination aus Vermögensausweitung und Momentum robuste Renditen bei moderatem Umsatz liefert.

## Details

- **Einstiegskriterien**: Monatlich; oberstes Vermögenswachstums-Dezil auswählen, dann nach
  Momentum einordnen; Long oberstes Quintil, Short unterstes Quintil
- **Long/Short**: Beide
- **Ausstiegskriterien**: Nächstes monatliches Rebalancing (Januar übersprungen)
- **Stops**: Nein
- **Standardwerte**:
  - `MomLook` = 252
  - `SkipMonths` = 1
  - `AssetDecile` = 10
  - `Quintile` = 5
  - `MinTradeUsd` = 200
  - `CandleType` = TimeSpan.FromDays(1)
- **Filter**:
  - Kategorie: Momentum, Fundamentaldaten
  - Richtung: Beide
  - Indikatoren: Kurs-Momentum, Vermögenswachstum
  - Stops: Nein
  - Komplexität: Fortgeschritten
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Ja
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
