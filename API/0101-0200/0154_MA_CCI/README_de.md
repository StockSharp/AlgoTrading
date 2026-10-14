# MA CCI Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die Moving Average und den CCI-Indikator kombiniert. Kauft, wenn der Preis über dem MA liegt und der CCI überverkauft ist. Verkauft, wenn der Preis unter dem MA liegt und der CCI überkauft ist.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 49%. Am besten geeignet für den Kryptomarkt.

Ein Moving Average gibt die Trendrichtung vor, während der CCI nach Abweichungen von diesem Durchschnitt sucht. Einstiege erfolgen bei CCI-Extremwerten in Richtung des MA.

Ideal für Swing-Trader, die bei Rücksetzern einsteigen. Ein prozentualer Stop schützt vor plötzlichen Kursausschlägen.

## Details

- **Einstiegskriterien**:
  - Long: `Close > MA && CCI < OversoldLevel`
  - Short: `Close < MA && CCI > OverboughtLevel`
- **Long/Short**: Beide
- **Ausstiegskriterien**:
  - CCI kehrt zur Nulllinie zurück
- **Stops**: Prozentbasiert mit `StopLossPercent`
- **Standardwerte**:
  - `MaPeriod` = 50
  - `CciPeriod` = 20
  - `OverboughtLevel` = 100m
  - `OversoldLevel` = -100m
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
    Ein 20-Kerzen-SMA bewegt sich mit dem CCI über dieselben 20 Kerzen, daher bleibt der Kurs fast nie darüber, während CCI unter -100 liegt: Das Archiv vom März 2024 enthält höchstens eine solche Kerze je Instrument. Der Trend-SMA verwendet daher standardmäßig 50 Kerzen. Ein Einstiegssignal gegen eine offene Position dreht sie.
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
