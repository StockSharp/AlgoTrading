# Doppel-MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Der Doppel-MACD verwendet zwei MACD-Indikatoren mit unterschiedlichen Geschwindigkeiten. Eine Position wird nur eröffnet, wenn beide MACDs in dieselbe Richtung zeigen.

Der erste MACD ist schnell und reagiert rasch. Der zweite ist langsamer und bestätigt den Trend vor dem Handel.

## Details
- **Daten**: Preiskerzen.
- **Einstiegskriterien**:
  - **Long**: Beide MACD-Linien über ihren Signallinien.
  - **Short**: Beide MACD-Linien unter ihren Signallinien.
- **Ausstiegskriterien**: Entgegengesetztes Signal oder Stop.
- **Stops**: Optionaler Stop-Loss.
- **Standardwerte**:
  - `FastLength1` = 12
  - `SlowLength1` = 26
  - `SignalLength1` = 9
  - `MaType1` = Ema
  - `FastLength2` = 24
  - `SlowLength2` = 52
  - `SignalLength2` = 9
  - `MaType2` = Ema
  - `StopLossPercent` = 2
  - `CandleType` = tf(5)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Long & Short
  - Indikatoren: MACD
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
