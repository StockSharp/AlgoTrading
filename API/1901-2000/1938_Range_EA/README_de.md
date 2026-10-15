# Range-EA-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die Preisabweichungen von einem gleitenden Durchschnitt innerhalb einer festen Spanne handelt. Öffnet Long- oder Short-Positionen, wenn der Preis eine bestimmte Distanz vom Durchschnitt abweicht. Unterstützt optionalen Trailing-Stop, schrittweise Mittelwertbildung, Umkehrmodul und Handelssitzungsfilter.

## Details

- **Einstiegskriterien**:
  - Long: Schlusskurs über gleitendem Durchschnitt + `Range`
  - Short: Schlusskurs unter gleitendem Durchschnitt - `Range`
- **Long/Short**: Beide
- **Ausstiegskriterien**:
  - `TakeProfit` oder `StopLoss` erreicht
  - Trailing-Stop greift, wenn aktiviert
  - Optionale Umkehr nach Bewegung von `Turn`
- **Stops**: Fester Wert
- **Standardwerte**:
  - `MaLength` = 21
  - `Range` = 250m
  - `TakeProfit` = 500m
  - `StopLoss` = 250m
  - `UseTrailingStop` = true
  - `TrailingStop` = 250m
  - `UseTurn` = true
  - `Turn` = 250m
  - `LotMultiplicator` = 1.65m
  - `TurnTakeProfit` = 500m
  - `UseStepDown` = false
  - `StepDown` = 150m
  - `UseTradeTime` = false
  - `OpenTradeTime` = 08:00:00
  - `CloseTradeTime` = 21:30:00
  - `OrderVolume` = 0.1m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: MA
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
