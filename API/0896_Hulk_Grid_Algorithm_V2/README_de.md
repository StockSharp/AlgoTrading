# Hulk Grid Algorithm V2 Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Grid-Handelsstrategie, die zehn gestaffelte Kauf-Limit-Orders um einen benutzerdefinierten Mittelpreis platziert. Die Ordergröße nimmt in Richtung des Mittelniveaus zu. Die Strategie schließt alle Positionen und storniert verbleibende Orders, wenn der Preis einen Stop-Loss unterhalb des untersten Grids oder einen Take-Profit oberhalb des oberen Grids erreicht.

## Details

- **Einstiegskriterien**: Grid aus zehn Kauf-Limit-Orders vom untersten bis zum höchsten Niveau.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**: Stop-Loss unterhalb des untersten Grids oder Take-Profit oberhalb des oberen Grids.
- **Stops**: Prozentbasierter Stop-Loss und Take-Profit.
- **Standardwerte**:
  - `MidPrice` = 0
  - `StopLossPercent` = 2.0
  - `TakeProfitPercent` = 2.0
  - `GridStep` = 200
  - `Lot` = 50
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filter**:
  - Kategorie: Grid
  - Richtung: Long
  - Indikatoren: Keine
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
