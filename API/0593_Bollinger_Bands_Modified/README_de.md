# Modifizierte Bollinger-Bands-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die Bollinger-Bands-Ausbrüche mit einem optionalen EMA-Trendfilter handelt. Geht long, wenn der Preis über das obere Band kreuzt, und short, wenn er unter das untere Band kreuzt.

Der Stop Loss wird beim jüngsten Hoch oder Tief platziert, und das Take Profit ist ein Vielfaches des Risikos.

## Details

- **Einstiegskriterien**:
  - Long: Preis kreuzt über das obere Bollinger Band
  - Short: Preis kreuzt unter das untere Bollinger Band
- **Long/Short**: Beide
- **Ausstiegskriterien**:
  - Long: Stop beim jüngsten Tief, Ziel bei Risiko * Faktor
  - Short: Stop beim jüngsten Hoch, Ziel bei Risiko * Faktor
- **Stops**: Höchst-/Tiefstwert der letzten N Kerzen
- **Standardwerte**:
  - `BollingerLength` = 20
  - `BollingerDeviation` = 0.38m
  - `EmaLength` = 80
  - `HighestLength` = 7
  - `LowestLength` = 7
  - `TargetFactor` = 1.6m
  - `EmaTrend` = true
  - `CrossoverCheck` = false
  - `CrossunderCheck` = false
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: Bollinger Bands, EMA, Highest, Lowest
  - Stops: Ja
  - Komplexität: Anfänger
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
