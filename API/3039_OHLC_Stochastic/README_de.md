# OHLC Stochastic-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Momentum-Folge-Strategie, die den klassischen %K/%D-Stochastic-Oszillator auf OHLC-Kerzen verwendet.
Der Algorithmus reagiert auf Kreuzungen in überkauften/überverkauften Zonen und schützt offene Trades mit einem konfigurierbaren Trailing Stop gemessen in Preisschritten.

## Details

- **Kernidee**: Den Momentum-Wechsel ausnutzen, wenn Stochastic %K %D an extremen Niveaus kreuzt.
- **Einstiegskriterien**:
  - **Long**:
    - %K kreuzt %D nach oben und mindestens eine der Linien liegt unter dem `LevelDown`-Schwellenwert.
    - Wenn eine Short-Position existiert, wird sie geschlossen und zu Long umgekehrt.
  - **Short**:
    - %K kreuzt %D nach unten und mindestens eine der Linien liegt über dem `LevelUp`-Schwellenwert.
    - Wenn eine Long-Position existiert, wird sie geschlossen und zu Short umgekehrt.
- **Ausstiegskriterien**:
  - Trailing Stop wird ausgelöst (basierend auf `TrailingStopSteps`-Distanz und `TrailingStepSteps`-Verbesserungsanforderung).
  - Entgegengesetztes Einstiegssignal erscheint, was eine Umkehr auslöst.
- **Trailing-Logik**:
  - Distanz und Schritt werden mit dem `PriceStep` des Instruments multipliziert, um Pips/Schritte in absolute Preise umzurechnen.
  - Stop rückt nur vor, nachdem der Trade über `TrailingStopSteps + TrailingStepSteps` vom Einstiegspreis hinaus bewegt hat.
  - Separate Trailing-Logik für Long- und Short-Seite.
- **Indikatoren**:
  - [StochasticOscillator](https://doc.stocksharp.com/html/T_StockSharp_Algo_Indicators_StochasticOscillator.htm) mit einstellbarem `KPeriod`, `DPeriod` und `Slowing`.
- **Long/Short**: Beide.
- **Stops**: Nur Trailing Stop (keine festen SL/TP-Orders).
- **Positionsgrößenbestimmung**: Verwendet den `Volume`-Parameter der Strategie; Umkehrungen senden `Volume + |Position|` zum Richtungswechsel.
- **Standardparameter**:
  - `CandleType` = `TimeSpan.FromHours(12).TimeFrame()`
  - `KPeriod` = 5
  - `DPeriod` = 3
  - `Slowing` = 3
  - `LevelUp` = 70
  - `LevelDown` = 30
  - `TrailingStopSteps` = 5 (Preisschritte)
  - `TrailingStepSteps` = 2 (Preisschritte)
- **Visualisierung**:
  - Zeichnet OHLC-Kerzen, Stochastic-Indikator und Trade-Marker, wenn Charts verfügbar sind.

## Nutzungshinweise

1. Konfigurieren Sie das zugrunde liegende Instrument und den Zeitrahmen vor dem Start der Strategie.
2. Passen Sie `TrailingStopSteps` entsprechend der Tick-Größe des Instruments an, um echte Pip-Distanzen widerzuspiegeln.
3. Die Strategie ruft `StartProtection()` auf, damit zusätzliche Risikoregeln extern angehängt werden können.
4. Funktioniert am besten in Trendregimes, wo Stochastic-Umkehrungen den Preis anführen.
5. Bei Intraday-Produkten erfordern niedrigere Zeitrahmen möglicherweise die Reduzierung der Trailing-Distanzen, um vorzeitige Ausstiege zu vermeiden.
