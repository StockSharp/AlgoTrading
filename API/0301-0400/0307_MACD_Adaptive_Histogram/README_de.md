# MACD-adaptives-Histogramm-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **MACD Adaptive Histogram**-Strategie basiert auf dem MACD mit adaptivem Histogramm-Schwellenwert.

Tests zeigen eine durchschnittliche Jahresrendite von etwa 184%. Sie funktioniert am besten auf dem Kryptomarkt.

### Tatsächliche MACD-Histogramm-Regeln (C# und Python)

Gehandelt wird auf **abgeschlossenen 15-Minuten-Kerzen** mit dem MACD(12,26,9)-Histogramm `H = MACD - Signal`. Aus den letzten 20 H-Werten werden SMA `A` und Standardabweichung `D` berechnet. Bei fertigen Indikatoren und `D > 0`: aus Flat **kaufen** bei `H >= A + 2*D` und `H > 0`, **verkaufen** bei `H <= A - 2*D` und `H < 0`. Long bei `H <= A` schließen, Short bei `H >= A` schließen. Nach einer Signalorder: **16 abgeschlossene Kerzen** Pause. Ein **nativer Stop-Loss von 2%** schützt ausgeführte Positionen zusätzlich, kein ATR-Stop. Die Renditeangabe oben ist nicht unabhängig verifiziert.



## Details
- **Einstiegskriterien**: Implementierung für Indikatorbedingungen prüfen.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Gegensignal oder Stop-Logik.
- **Stops**: Ja, mit indikatorbasierten Berechnungen.
- **Standardwerte**:
  - `FastPeriod = 12`
  - `SlowPeriod = 26`
  - `SignalPeriod = 9`
  - `HistogramAvgPeriod = 20`
  - `StdDevMultiplier = 2.0m`
  - `StopLossPercent = 2.0m`
  - `CandleType = TimeSpan.FromMinutes(15).TimeFrame()`
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Histogram
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday (15m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
