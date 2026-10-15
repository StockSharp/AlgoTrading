# RSI & Rückwärts-gewichteter MA-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie verwendet den Relative Strength Index und einen rückwärts gewichteten gleitenden Durchschnitt mit einem Änderungsraten-Filter. Long-Positionen öffnen, wenn RSI den Schwellenwert überschreitet und MA ROC unter dem festgelegten Niveau liegt, während Short-Positionen bei entgegengesetzten Bedingungen öffnen. Das System verwendet einen ATR-basierten Trailing-Stop und festes Verhältnis-Positionsgrößenbestimmung.

## Details

- **Einstiegskriterien**:
  - **Long**: `RSI >= RsiLongSignal` und `MA ROC <= RocMaLongSignal`
  - **Short**: `RSI <= RsiShortSignal` und `MA ROC >= RocMaShortSignal`
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Entgegengesetztes Signal, Stop-Loss oder Trailing-Stop.
- **Stops**: Ja, ATR-Trailing-Stop und maximaler Verlustprozentsatz.
- **Standardwerte**:
  - `RsiLength` = 20
  - `MaType` = RWMA
  - `MaLength` = 19
  - `RsiLongSignal` = 60
  - `RsiShortSignal` = 40
  - `TakeProfitActivation` = 5
  - `TrailingPercent` = 3
  - `MaxLossPercent` = 10
  - `FixedRatio` = 400
  - `IncreasingOrderAmount` = 200
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: RSI, Moving Average, ATR
  - Stops: Ja
  - Komplexität: Hoch
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
