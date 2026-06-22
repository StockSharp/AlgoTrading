# SilverTrend Signal ReOpen-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie basierend auf dem SilverTrend-Indikator mit optionalem Wiedereinstieg. Öffnet eine Position, wenn der Indikator die Richtung ändert, und fügt bei jedem definierten Preisschritt zugunsten des Trades zusätzliche Positionen hinzu. Positionen können bei entgegengesetzten Signalen oder beim Erreichen von Stop-Loss / Take-Profit-Niveaus geschlossen werden.

## Details

- **Einstiegskriterien**:
  - Long: SilverTrend-Indikator wechselt von Abwärtstrend zu Aufwärtstrend
  - Short: SilverTrend-Indikator wechselt von Aufwärtstrend zu Abwärtstrend
- **Long/Short**: Beide
- **Ausstiegskriterien**:
  - Optional bei entgegengesetzten SilverTrend-Signalen schließen
  - Stop-Loss oder Take-Profit erreicht
- **Stops**: Absolute Preisniveaus
- **Standardwerte**:
  - `CandleType` = TimeSpan.FromHours(4).TimeFrame()
  - `Ssp` = 9
  - `Risk` = 3
  - `PriceStep` = 300m
  - `PosTotal` = 10
  - `StopLoss` = 1000m
  - `TakeProfit` = 2000m
  - `BuyPosOpen` = true
  - `SellPosOpen` = true
  - `BuyPosClose` = true
  - `SellPosClose` = true
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: SilverTrend
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
