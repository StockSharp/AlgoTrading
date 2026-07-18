# RSI + MACD Nur-Long-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie geht long, wenn RSI die Mittellinie nach oben kreuzt mit bullisher MACD-Bestätigung, oder wenn MACD seine Signallinie überschreitet während RSI über der Mittellinie bleibt. Ausstiege erfolgen, wenn RSI unter die Mittellinie fällt oder MACD die Signallinie mit einem nicht positiven Histogramm unterschreitet. Ein optionaler EMA-Trendfilter und überkaufter Kontext können Einstiege verfeinern.

## Details

- **Einstiegskriterien**: RSI kreuzt Mittellinie nach oben mit MACD bullish oder MACD kreuzt Signallinie nach oben mit RSI über der Mittellinie
- **Long/Short**: Nur Long
- **Ausstiegskriterien**: RSI kreuzt Mittellinie nach unten oder MACD kreuzt Signallinie nach unten mit Histogramm ≤ 0
- **Stops**: Optionaler prozentualer Take-Profit und Stop-Loss
- **Standardwerte**:
  - `RsiLength` = 14
  - `RsiOversold` = 30
  - `RsiMidline` = 50
  - `FastLength` = 12
  - `SlowLength` = 26
  - `SignalLength` = 9
  - `OversoldWindowBars` = 10
  - `EmaLength` = 200
  - `TakeProfitPercent` = 11.5
  - `StopLossPercent` = 2.5
- **Filter**:
  - Kategorie: Trend
  - Richtung: Nur Long
  - Indikatoren: RSI, MACD, EMA
  - Stops: Ja (optional)
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
