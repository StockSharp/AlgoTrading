# YinYang RSI Volumen-Trend-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die YinYang RSI Volumen-Trend-Strategie nutzt volumengewichtete Preiszonen und einen RSI-Filter, um Trendumkehrungen zu erkennen. Die Strategie kauft, wenn der Preis die untere Zone verlässt, und verkauft, wenn er die obere Zone verlässt. Optionale Stop-Loss- und Take-Profit-Niveaus basieren auf dynamischen Zonen.

## Details

- **Einstiegskriterien**: Preis verlässt die berechneten Kaufzonen mit optionalem Verfügbarkeits-Reset.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Preis erreicht die entgegengesetzte Zone oder löst optionalen Stop-Loss/Take-Profit aus.
- **Stops**: Optional.
- **Standardwerte**:
  - `TrendLength` = 80
  - `UseTakeProfit` = true
  - `UseStopLoss` = true
  - `StopLossMultiplier` = 0.1
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: VWMA, EMA, RSI
  - Stops: Optional
  - Komplexität: Mittel
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
