# Bollinger Ausbruch-Momentum-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Aus der ursprünglichen MQL-Strategie konvertiert. Handelt Bollinger-Band-Ausbrüche, die durch EMA, MACD und RSI bestätigt werden. Die Strategie tritt pro Volatilitätsexpansion nur einmal ein und bewegt den Stop entlang des mittleren Bandes, während ein fester Take-Profit in Pips verwendet wird.

## Details

- **Einstiegskriterien**:
  - Long: Bandbreite über `BreakoutFactor`, MACD > 0, RSI > 50, EMA über dem mittleren Band, vorheriger Schluss über dem vorherigen oberen Band
  - Short: Bandbreite über `BreakoutFactor`, MACD < 0, RSI < 50, EMA unter dem mittleren Band, vorheriger Schluss unter dem vorherigen unteren Band
- **Long/Short**: Beide
- **Ausstiegskriterien**:
  - Long: Preis berührt den Trailing-Stop am mittleren Band oder erreicht den Take-Profit
  - Short: Preis berührt den Trailing-Stop am mittleren Band oder erreicht den Take-Profit
- **Stops**: Stop-Level ist das aktuelle mittlere Bollinger-Band, jede Kerze aktualisiert
- **Take Profit**: Feste Distanz in Pips
- **Standardwerte**:
  - `BollingerLength` = 18
  - `BollingerDeviation` = 2m
  - `BreakoutFactor` = 0.0015m
  - `TakeProfitPips` = 100
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: Bollinger Bands, EMA, MACD, RSI
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
