# Keltner Macd Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Strategie basierend auf Keltner Channels und MACD. Geht long, wenn der Preis über den oberen Keltner Channel ausbricht und MACD > Signal. Geht short, wenn der Preis unter den unteren Keltner Channel ausbricht und MACD < Signal. Ausstieg, wenn MACD seine Signallinie in der entgegengesetzten Richtung kreuzt.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 169%. Sie funktioniert am besten auf dem Kryptomarkt.

Keltner Channel-Ausbrüche dienen als Auslöser, und MACD-Momentum filtert die Richtung. Die Strategie initiiert Trades, sobald beide Signale übereinstimmen.

Gut für Trader, die Volatilitätsexpansionen mit Momentum-Rückenwind verfolgen. Ein ATR-basierter Stop begrenzt das Risiko.

## Details

- **Einstiegskriterien**:
  - Long: `Close > UpperBand && MACD > Signal`
  - Short: `Close < LowerBand && MACD < Signal`
- **Long/Short**: Beide
- **Ausstiegskriterien**: MACD-Kreuzung in entgegengesetzter Richtung
- **Stops**: ATR-basiert mit `AtrMultiplier`
- **Standardwerte**:
  - `EmaPeriod` = 20
  - `Multiplier` = 2m
  - `AtrPeriod` = 14
  - `MacdFastPeriod` = 12
  - `MacdSlowPeriod` = 26
  - `MacdSignalPeriod` = 9
  - `AtrMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(15).TimeFrame()
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Beide
  - Indikatoren: Keltner Channel, MACD
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

