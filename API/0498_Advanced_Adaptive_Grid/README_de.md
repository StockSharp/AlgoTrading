# Fortgeschrittene adaptive Gitter-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die fortgeschrittene adaptive Gitter-Strategie verwendet mehrere technische Indikatoren zur Bewertung der Trendrichtung und baut ein dynamisches Gitter von Einstiegsniveaus auf. Die Gittergröße passt sich über ATR an die Volatilität an, und Orders werden platziert, wenn der Preis Gitterniveaus in Trendrichtung berührt. Zu den Risikokontrollen gehören fester Stop-Loss, Take-Profit, Trailing-Stop, zeitbasierter Ausstieg und tägliches Verlustlimit.

## Details

- **Einstiegskriterien**:
  - In Trendmärkten: Preis erreicht berechnete Gitterniveaus mit RSI-Bestätigung.
  - In Seitwärtsmärkten: überkaufter/überverkaufter RSI löst Gitter-Einstiege aus.
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - Stop-Loss, Take-Profit, Trailing-Stop, Trendumkehr oder zeitbasierter Ausstieg.
- **Stops**: Fest und Trailing.
- **Standardwerte**:
  - `BaseGridSize` = 1
  - `MaxPositions` = 5
  - `UseVolatilityGrid` = True
  - `AtrLength` = 14
  - `AtrMultiplier` = 1.5
  - `RsiLength` = 14
  - `RsiOverbought` = 70
  - `RsiOversold` = 30
  - `ShortMaLength` = 20
  - `LongMaLength` = 50
  - `SuperLongMaLength` = 200
  - `MacdFastLength` = 12
  - `MacdSlowLength` = 26
  - `MacdSignalLength` = 9
  - `StopLossPercent` = 2
  - `TakeProfitPercent` = 3
  - `UseTrailingStop` = True
  - `TrailingStopPercent` = 1
  - `MaxLossPerDay` = 5
  - `TimeBasedExit` = True
  - `MaxHoldingPeriod` = 48
- **Filter**:
  - Kategorie: Gitter / Trend
  - Richtung: Beide
  - Indikatoren: ATR, SMA, MACD, RSI, Momentum
  - Stops: Ja
  - Komplexität: Hoch
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Hoch
