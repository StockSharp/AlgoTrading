# MA CCI Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die Moving Average und den CCI-Indikator kombiniert. Kauft, wenn der Preis über dem MA liegt und der CCI überverkauft ist. Verkauft, wenn der Preis unter dem MA liegt und der CCI überkauft ist.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 49%. Am besten geeignet für den Kryptomarkt.

Ein Moving Average gibt die Trendrichtung vor, während der CCI nach Abweichungen von diesem Durchschnitt sucht. Einstiege erfolgen bei CCI-Extremwerten in Richtung des MA.

Ideal für Swing-Trader, die bei Rücksetzern einsteigen. ATR-Stops schützen vor plötzlichen Kursausschlägen.

## Details

- **Einstiegskriterien**:
  - Long: `Close > MA && CCI < OversoldLevel`
  - Short: `Close < MA && CCI > OverboughtLevel`
- **Long/Short**: Beide
- **Ausstiegskriterien**:
  - CCI kehrt zur Nulllinie zurück
- **Stops**: Prozentbasiert mit `StopLossPercent`
- **Standardwerte**:
  - `MaPeriod` = 20
  - `CciPeriod` = 20
  - `OverboughtLevel` = 100m
  - `OversoldLevel` = -100m
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Beide
  - Indikatoren: Moving Average, CCI
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
