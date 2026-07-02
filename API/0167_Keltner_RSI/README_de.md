# Strategie Keltner Rsi
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die Keltner Channels und RSI-Indikatoren kombiniert. Sucht nach Mean-Reversion-Möglichkeiten, wenn der Preis Kanalgrenzen berührt und der RSI überverkaufte/überkaufte Bedingungen bestätigt.

Tests zeigen eine durchschnittliche Jahresrendite von etwa 88%. Die Strategie funktioniert am besten auf dem Aktienmarkt.

Keltner Channels kartieren die jüngste Volatilität, während der RSI Momentum-Extreme misst. Einstiege erfolgen, wenn der RSI eine Bewegung über den Kanal hinaus unterstützt.

Gut für Bounce-Trader rund um Volatilitätshüllen. Stops basieren auf einem ATR-Multiplikator.

## Details

- **Einstiegskriterien**:
  - Long: `Close < LowerBand && RSI < RsiOversoldLevel`
  - Short: `Close > UpperBand && RSI > RsiOverboughtLevel`
- **Long/Short**: Beide
- **Ausstiegskriterien**:
  - Preis kehrt zum EMA zurück
- **Stops**: Prozentbasiert mit `StopLossPercent`
- **Standardwerte**:
  - `EmaPeriod` = 20
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 2.0m
  - `RsiPeriod` = 14
  - `RsiOverboughtLevel` = 70m
  - `RsiOversoldLevel` = 30m
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Beide
  - Indikatoren: Keltner Channel, RSI
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

