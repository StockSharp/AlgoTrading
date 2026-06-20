# Adaptive Squeeze-Momentum-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die adaptive Squeeze-Momentum-Strategie erkennt Volatilitätskontraktionen, wenn Bollinger Bänder innerhalb der Keltner Kanäle liegen, und wartet auf einen Ausbruch mit starkem Momentum. Die Momentumstärke wird anhand eines auf der Standardabweichung basierenden Schwellenwerts bewertet. Optionale RSI- und EMA-Trendfilter verfeinern die Einstiege. ATR kann zur Festlegung dynamischer Stop-Loss- und Take-Profit-Niveaus verwendet werden, und Positionen werden nach einer zeitbasierten Halteperiode geschlossen.

## Details

- **Einstiegskriterien**:
  - Squeeze löst sich (Bollinger Bänder außerhalb der Keltner Kanäle).
  - **Long**: Momentum > dynamischer Schwellenwert, RSI kreuzt über Überverkauft, Trend-EMA steigt (optional).
  - **Short**: Momentum < -dynamischer Schwellenwert, RSI kreuzt unter Überkauft, Trend-EMA fällt (optional).
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - Entgegengesetztes Signal, ATR-basierter Stop-Loss/Take-Profit oder zeitbasierter Ausstieg.
- **Stops**: Optionaler ATR Stop-Loss und Take-Profit.
- **Standardwerte**:
  - `BollingerPeriod` = 20
  - `BollingerMultiplier` = 2.0
  - `KeltnerPeriod` = 20
  - `KeltnerMultiplier` = 1.5
  - `MomentumLength` = 12
  - `TrendMaLength` = 50
  - `UseAtrStops` = True
  - `AtrMultiplierSl` = 1.5
  - `AtrMultiplierTp` = 2.5
  - `AtrLength` = 14
  - `MinVolatility` = 0.5
  - `HoldingPeriodMultiplier` = 1.5
  - `UseTrendFilter` = True
  - `UseRsiFilter` = True
  - `RsiLength` = 14
  - `RsiOversold` = 40
  - `RsiOverbought` = 60
  - `MomentumMultiplier` = 1.5
  - `AllowLong` = True
  - `AllowShort` = True
- **Filter**:
  - Kategorie: Volatilitätsausbruch
  - Richtung: Beide
  - Indikatoren: Bollinger Bands, Keltner Channels, Momentum, RSI, EMA, ATR
  - Stops: Optional
  - Komplexität: Hoch
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
