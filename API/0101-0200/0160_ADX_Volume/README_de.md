# ADX Volume Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Implementierung der Strategie ADX + Volume. Trades werden eröffnet, wenn der ADX über dem Schwellenwert liegt und das Volumen überdurchschnittlich ist. Die Richtung wird durch den Vergleich von DI+ und DI- bestimmt.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 67%. Am besten geeignet für den Aktienmarkt.

Ein hoher ADX weist auf einen starken Trend hin, und Volumenspitzen bestätigen das Engagement. Einstiege erfolgen, wenn beide Indikatoren gleichzeitig Stärke zeigen.

Ideal zum Erfassen energischer Ausbrüche. Ein ATR-basierter Stop hält das Risiko unter Kontrolle.

## Details

- **Einstiegskriterien**:
  - Long: `ADX > AdxThreshold && Volume > AvgVolume`
  - Short: `ADX > AdxThreshold && Volume > AvgVolume`
- **Long/Short**: Beide
- **Ausstiegskriterien**: Trend schwächt sich unter den Schwellenwert ab
- **Stops**: ATR-basiert mit `StopLoss`
- **Standardwerte**:
  - `AdxPeriod` = 14
  - `AdxThreshold` = 25m
  - `VolumeAvgPeriod` = 20
  - `StopLoss` = new Unit(2, UnitTypes.Absolute)
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: ADX, Volumen
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
