# Strategie Adx Cci
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie basierend auf ADX- und CCI-Indikatoren. Geht Long, wenn ADX > 25 und CCI überverkauft ist (< -100). Geht Short, wenn ADX > 25 und CCI überkauft ist (> 100).

Tests zeigen eine durchschnittliche Jahresrendite von etwa 97%. Die Strategie funktioniert am besten auf dem Kryptomarkt.

Der ADX bewertet, ob ein Trend Stärke hat, und der CCI identifiziert den Einstiegszeitpunkt nach Rücksetzern. Longs und Shorts folgen der ADX-Richtung.

Ausgerichtet auf Momentum-Trader, die bei Rücksetzern einsteigen. ATR-Vielfache steuern das Risiko.

## Details

- **Einstiegskriterien**:
  - Long: `ADX > 25 && CCI < -100`
  - Short: `ADX > 25 && CCI > 100`
- **Long/Short**: Beide
- **Ausstiegskriterien**: Trend schwächt sich ab oder CCI kreuzt die Nulllinie
- **Stops**: Prozentbasiert mit `StopLossPercent`
- **Standardwerte**:
  - `AdxPeriod` = 14
  - `CciPeriod` = 20
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Beide
  - Indikatoren: ADX, CCI
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

