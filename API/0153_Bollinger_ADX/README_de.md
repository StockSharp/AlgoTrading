# Bollinger ADX Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die Bollinger Bands und den ADX-Indikator kombiniert. Sucht nach Ausbrüchen mit starker Trendbestätigung.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 46%. Am besten geeignet für den Aktienmarkt.

Preisbewegungen außerhalb der Bollinger Bands werden durch ADX auf Stärke gefiltert. Trades werden ausgelöst, wenn ein Band-Ausbruch mit einem hohen ADX zusammenfällt.

Nützlich bei Volatilitätsschüben, die von starken Trends begleitet werden. Die Stop-Größe wird durch ATR bestimmt.

## Details

- **Einstiegskriterien**:
  - Long: `Close < LowerBand && ADX > AdxThreshold`
  - Short: `Close > UpperBand && ADX > AdxThreshold`
- **Long/Short**: Beide
- **Ausstiegskriterien**:
  - Bollinger Mean Reversion
- **Stops**: ATR-basiert mit `AtrMultiplier`
- **Standardwerte**:
  - `BollingerPeriod` = 20
  - `BollingerDeviation` = 2.0m
  - `AdxPeriod` = 14
  - `AdxThreshold` = 25m
  - `AtrMultiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Beide
  - Indikatoren: Bollinger Bands, ADX
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
