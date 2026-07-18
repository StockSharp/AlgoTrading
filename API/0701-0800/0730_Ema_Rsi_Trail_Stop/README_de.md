# EMA-RSI-Trailing-Stop-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt Crossover des kurzen und mittleren EMA, gefiltert durch einen langen EMA. RSI-Niveaus schließen Trades, und ein Trailing-Stop mit festem Stop-Loss verwaltet das Risiko. Trades können optional nach einer Anzahl von Bars geschlossen werden, wenn sie profitabel sind.

## Details

- **Einstiegskriterien**: EMA A kreuzt EMA B, Trend durch EMA C und Kerzenrichtung bestätigt.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: RSI-Schwellenwerte, Trailing-Stop oder zeitbasierter Ausstieg.
- **Stops**: Fester prozentualer Stop, der nach Preisbewegung um `TrailOffset` zu einem Trailing-Stop wird.
- **Standardwerte**:
  - `EmaALength` = 10
  - `EmaBLength` = 20
  - `EmaCLength` = 100
  - `RsiLength` = 14
  - `ExitLongRsi` = 70
  - `ExitShortRsi` = 30
  - `TrailPoints` = 50
  - `TrailOffset` = 10
  - `FixStopLossPercent` = 5
  - `CloseAfterXBars` = true
  - `XBars` = 24
  - `ShowLong` = true
  - `ShowShort` = false
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: EMA, RSI
  - Stops: Trailing
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
