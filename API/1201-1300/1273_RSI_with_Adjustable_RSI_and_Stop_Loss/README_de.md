# RSI-Strategie mit einstellbarem RSI und Stop-Loss
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Kauft, wenn der RSI-Wert unter einen Schwellenwert fällt, und schließt die Long-Position, wenn der Preis über das Hoch der vorherigen Kerze ausbricht. Ein prozentualer Stop-Loss schützt jeden Trade.

## Details

- **Einstiegskriterien**:
  - Long: RSI unter `RsiThreshold`
- **Long/Short**: Long
- **Ausstiegskriterien**:
  - Schlusskurs über dem Hoch der vorherigen Kerze
  - Stop-Loss
- **Stops**: Ja
- **Standardwerte**:
  - `RsiLength` = 8
  - `RsiThreshold` = 28m
  - `StopLossPercent` = 5m
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filter**:
  - Kategorie: Oszillator
  - Richtung: Long
  - Indikatoren: RSI
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Kurzfristig
  - Saisonalität: Keine
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
