# MACD CCI Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Implementierung der Strategie MACD + CCI. Kaufen, wenn der MACD über der Signallinie liegt und der CCI unter -100 (überverkauft) ist. Verkaufen, wenn der MACD unter der Signallinie liegt und der CCI über 100 (überkauft) ist.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 70%. Am besten geeignet für den Aktienmarkt.

MACD-Schwankungen heben Impulswechsel hervor; der CCI hilft dabei, Pullback-Einstiege in diese Richtung zu timen. Sowohl Long- als auch Short-Trades sind möglich.

Trader, die Momentum mit Oszillatoren kombinieren, könnten diese Technik mögen. Die Risikokontrolle verwendet einen ATR-Stop.

## Details

- **Einstiegskriterien**:
  - Long: `MACD > Signal && CCI < CciOversold`
  - Short: `MACD < Signal && CCI > CciOverbought`
- **Long/Short**: Beide
- **Ausstiegskriterien**: MACD-Kreuzung in entgegengesetzte Richtung
- **Stops**: Prozentbasiert mit `StopLoss`
- **Standardwerte**:
  - `FastPeriod` = 12
  - `SlowPeriod` = 26
  - `SignalPeriod` = 9
  - `CciPeriod` = 20
  - `CciOversold` = -100m
  - `CciOverbought` = 100m
  - `StopLoss` = new Unit(2, UnitTypes.Percent)
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Beide
  - Indikatoren: MACD, CCI
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
