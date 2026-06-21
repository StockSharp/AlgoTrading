# Post-Open Long ATR Stop Loss Take Profit-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie eröffnet eine Long-Position während der Marktöffnung nach einem Ausbruch aus dem Widerstand, während der Kurs nahe der Bollinger-Mittellinie bleibt. Sie verwendet EMA-, RSI-, ADX- und ATR-Filter und verlässt die Position über ATR-basierten Stop-Loss und Take-Profit.

## Details

- **Einstiegskriterien**:
  - **Long**: Ausbruch über den jüngsten Widerstand während der Marktöffnung, Kurs nahe der Bollinger-Mittellinie, RSI über dem Schwellenwert, ADX über dem Schwellenwert, kurzfristiger Aufwärtstrend und kein Rücksetzer.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**:
  - ATR-basierter Stop-Loss oder Take-Profit erreicht.
- **Stops**:
  - ATR-basierter Stop-Loss und Take-Profit.
- **Standardwerte**:
  - `BB Length` = 14
  - `BB Mult` = 1.5
  - `EMA Length` = 10
  - `EMA Long Length` = 200
  - `RSI Length` = 7
  - `RSI Threshold` = 30
  - `ADX Length` = 7
  - `ADX Threshold` = 10
  - `ATR Length` = 14
  - `ATR SL Mult` = 2.0
  - `ATR TP Mult` = 4.0
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Long
  - Indikatoren: Bollinger Bands, EMA, RSI, ADX, ATR
  - Stops: ATR
  - Komplexität: Mittel
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
