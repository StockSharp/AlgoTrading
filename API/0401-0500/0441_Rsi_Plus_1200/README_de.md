# RSI + 1200-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **RSI + 1200-Strategie** versucht, Trendumkehrungen zu erfassen, die durch
relative Stärke und einen Trendfilter aus einem höheren Zeitrahmen bestätigt werden. Sie kombiniert einen klassischen
14‑Perioden Relative Strength Index mit einem Exponential Moving Average, der
auf einer 120‑minütigen Multi‑Zeitrahmen-Serie berechnet wird ("1200" bezieht sich auf den höheren Zeitrahmen
im ursprünglichen Konzept). Handelssignale werden nur aufgenommen, wenn Momentum und
Trendfilter übereinstimmen.

Backtests auf liquiden Kryptowährungspaaren zeigen, dass die Methode in
anhaltenden Richtungsmärkten am besten abschneidet. Unruhige oder seitwärtsgerichtete Perioden können Fehlsignale erzeugen,
daher enthält die Strategie einen kleinen Kursspielraum um den EMA und einen
prozentualen Stop‑Loss zur Risikobegrenzung.

Ein Long-Trade wird eröffnet, wenn RSI von überverkauftem Territorium aufwärts kreuzt und
der Preis innerhalb von einem Prozent über dem Higher‑Timeframe EMA liegt. Das Short-Setup ist
die gespiegelte Bedingung. Positionen werden geschlossen, wenn RSI das entgegengesetzte
Extrem erreicht, was die Erschöpfung der Bewegung signalisiert. Ein Schutz-Stop wird auch bei
`stopLossPercent` Prozent vom Einstiegspreis platziert.

## Details

- **Einstiegsbedingungen**
  - **Long**: RSI kreuzt über `rsiOversold` und Schluss ist <= 1% über dem EMA.
  - **Short**: RSI kreuzt unter `rsiOverbought` und Schluss ist >= 1% unter dem EMA.
- **Ausstiegsbedingungen**
  - **Long**: RSI steigt über `rsiOverbought`.
  - **Short**: RSI fällt unter `rsiOversold`.
- **Stops**: Optionaler prozentualer Stop‑Loss über `stopLossPercent`.
- **Standardparameter**
  - `rsiLength` = 14
  - `rsiOverbought` = 72
  - `rsiOversold` = 28
  - `emaLength` = 150
  - `mtfTimeframe` = 120 Minuten
  - `stopLossPercent` = 0.10 (10%)
- **Filter**
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: RSI, EMA
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday / Multi‑Zeitrahmen
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Moderat
