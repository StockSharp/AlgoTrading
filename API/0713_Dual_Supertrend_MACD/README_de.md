# Duale Supertrend MACD-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Duale Supertrend MACD-Strategie kombiniert zwei Supertrend-Indikatoren mit einem MACD-Filter.
Eine Long-Position wird eröffnet, wenn der Preis über beiden Supertrend-Linien liegt und das MACD-Histogramm positiv ist.
Short-Positionen entstehen, wenn der Preis unter beiden Linien liegt und das Histogramm negativ ist.
Positionen werden geschlossen, sobald ein Supertrend die Richtung wechselt oder das MACD-Histogramm die Nulllinie kreuzt.

## Details
- **Daten**: Preiskerzen.
- **Einstiegskriterien**:
  - Long: `Close > Supertrend1 && Close > Supertrend2 && MACD Histogram > 0`
  - Short: `Close < Supertrend1 && Close < Supertrend2 && MACD Histogram < 0`
- **Ausstiegskriterien**:
  - Long: `Close < Supertrend1 || Close < Supertrend2 || MACD Histogram < 0`
  - Short: `Close > Supertrend1 || Close > Supertrend2 || MACD Histogram > 0`
- **Stops**: Standardmäßig keine.
- **Standardwerte**:
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `OscillatorMaType` = Exponential
  - `SignalMaType` = Exponential
  - `AtrPeriod1` = 10
  - `Factor1` = 3.0
  - `AtrPeriod2` = 20
  - `Factor2` = 5.0
  - `TradeDirection` = "Both"
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Konfigurierbar
  - Indikatoren: Supertrend, MACD
  - Komplexität: Mittel
  - Risikolevel: Mittel
