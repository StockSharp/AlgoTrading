# Hull Ma Volume Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Strategie, die den Hull Moving Average zur Bestimmung der Trendrichtung und die Volumenbestätigung für Trade-Einstiege nutzt.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 169%. Sie funktioniert am besten im Kryptomarkt.

Der Hull Moving Average glättet Rauschen und steigendes Volumen bestätigt Überzeugung. Einstiege erfolgen, wenn sich der Preis mit dem Hull-Anstieg bewegt, unterstützt durch einen Volumensurge.

Diese Methode richtet sich an Trader, die auf starke Beteiligung bei Ausbrüchen achten. ATR-basierte Stops schützen vor plötzlichen Umkehrungen.

## Details

- **Einstiegskriterien**:
  - Long: `HullMA(t) > HullMA(t-1) && Volume > AvgVolume * VolumeMultiplier`
  - Short: `HullMA(t) < HullMA(t-1) && Volume > AvgVolume * VolumeMultiplier`
- **Long/Short**: Beide
- **Ausstiegskriterien**:
  - Long: `HullMA(t) < HullMA(t-1)`
  - Short: `HullMA(t) > HullMA(t-1)`
- **Stops**: `StopLossAtr` ATR vom Einstieg
- **Standardwerte**:
  - `HullPeriod` = 9
  - `VolumePeriod` = 20
  - `VolumeMultiplier` = 1.5m
  - `StopLossAtr` = 2.0m
  - `AtrPeriod` = 14
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: Hull MA, Moving Average, Volume
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

