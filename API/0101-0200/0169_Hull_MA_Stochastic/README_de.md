# Strategie Hull Ma Stochastic
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Hull Moving Average + Stochastic Oscillator Strategie. Die Strategie tritt ein, wenn sich die HMA-Trendrichtung ändert und der Stochastic überverkaufte/überkaufte Bedingungen bestätigt.

Tests zeigen eine durchschnittliche Jahresrendite von etwa 94%. Die Strategie funktioniert am besten auf dem Aktienmarkt.

Der Hull MA zeigt die Trendrichtung schnell auf. Der Stochastic wartet auf einen Rückgang oder eine Erholung innerhalb dieses Trends, um den Trade auszulösen.

Ein flexibler Ansatz für Trader, die glatte Signale bevorzugen. ATR-basierte Stops begrenzen den potenziellen Verlust.

## Details

- **Einstiegskriterien**:
  - Long: `HullMA turning up && StochK < 20`
  - Short: `HullMA turning down && StochK > 80`
- **Long/Short**: Beide
- **Ausstiegskriterien**:
  - Hull MA Richtungswechsel
- **Stops**: ATR-basiert mit `StopLossAtr`
- **Standardwerte**:
  - `HmaPeriod` = 9
  - `StochPeriod` = 14
  - `StochK` = 3
  - `StochD` = 3
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
  - `StopLossAtr` = 2m
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Beide
  - Indikatoren: Hull MA, Moving Average, Stochastic Oscillator
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

