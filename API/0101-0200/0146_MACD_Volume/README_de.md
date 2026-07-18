# Strategie Macd Volume
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Strategie, die MACD (Moving Average Convergence Divergence) mit Volumenbestätigung kombiniert. Einstieg in Positionen, wenn die MACD-Linie die Signallinie kreuzt und dies durch erhöhtes Volumen bestätigt wird.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 175%. Sie funktioniert am besten im Aktienmarkt.

MACD-Kreuzungen werden durch einen Volumenanstieg zur Bestätigung des Impulses gefiltert. Kaufsignale entstehen bei bullischen Kreuzungen mit wachsendem Volumen; Verkäufe das Gegenteil.

Momentum-Trader, die auf Volumspitzen achten, können es wertvoll finden. Das Risiko wird mit einem ATR-Stop begrenzt.

## Details

- **Einstiegskriterien**:
  - Long: `MACD crosses above Signal && Volume > AvgVolume * VolumeMultiplier`
  - Short: `MACD crosses below Signal && Volume > AvgVolume * VolumeMultiplier`
- **Long/Short**: Beide
- **Ausstiegskriterien**:
  - MACD-Kreuzung in entgegengesetzter Richtung
- **Stops**: Prozentbasiert bei `StopLossPercent`
- **Standardwerte**:
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `VolumePeriod` = 20
  - `VolumeMultiplier` = 1.5m
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: MACD, Volume
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

