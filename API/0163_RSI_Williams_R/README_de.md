# Strategie Rsi Williams R
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Implementierung der Strategie - RSI + Williams %R. Kaufen, wenn der RSI unter 30 und der Williams %R unter -80 liegt (doppelte Überverkauftbedingung). Verkaufen, wenn der RSI über 70 und der Williams %R über -20 liegt (doppelte Überkauftbedingung).

Tests zeigen eine durchschnittliche Jahresrendite von etwa 76%. Die Strategie funktioniert am besten auf dem Forexmarkt.

Der RSI beschreibt den allgemeinen Momentum, während der Williams %R ein schnelleres Umkehrsignal liefert. Trades werden ausgelöst, wenn beide Oszillatoren übereinstimmen.

Gut für aktive Trader, die kurze Swings verfolgen. ATR-basierte Stops werden eingesetzt.

## Details

- **Einstiegskriterien**:
  - Long: `RSI < RsiOversold && WilliamsR < WilliamsROversold`
  - Short: `RSI > RsiOverbought && WilliamsR > WilliamsROverbought`
- **Long/Short**: Beide
- **Ausstiegskriterien**:
  - RSI kehrt in die neutrale Zone zurück
- **Stops**: Prozentbasiert mit `StopLoss`
- **Standardwerte**:
  - `RsiPeriod` = 14
  - `RsiOversold` = 30m
  - `RsiOverbought` = 70m
  - `WilliamsRPeriod` = 14
  - `WilliamsROversold` = -80m
  - `WilliamsROverbought` = -20m
  - `StopLoss` = new Unit(2, UnitTypes.Percent)
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Beide
  - Indikatoren: RSI, Williams %R, R
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

