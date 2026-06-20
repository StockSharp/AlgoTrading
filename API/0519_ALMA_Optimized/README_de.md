# ALMA Optimized-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie kombiniert eine Arnaud Legoux Moving Average mit einem langfristigen EMA, ADX, RSI und Bollinger-Bändern. Ein ATR-basierter Filter stellt ausreichende Volatilität sicher. Positionen verwenden ATR-Multiplikatoren für Stop-Loss und Take-Profit, mit einem optionalen zeitbasierten Ausstieg.

## Details

- **Einstiegskriterien**:
  - **Long**: ATR über dem Schwellenwert, Schlusskurs über EMA und ALMA, RSI > 30, ADX > 30, Schlusskurs unter dem oberen Bollinger-Band und Abkühlzeit abgelaufen.
  - **Short**: Schlusskurs kreuzt unter die schnelle EMA unter demselben Volatilitätsfilter.
- **Ausstiegskriterien**:
  - Stop-Loss oder Take-Profit basierend auf ATR-Multiplikatoren.
  - Optionaler zeitbasierter Ausstieg in Bars.
- **Standardwerte**:
  - Schneller EMA = 20.
  - ATR-Länge = 14.
  - EMA-Länge = 72.
  - ADX-Länge = 10.
  - RSI-Länge = 14.
  - Abkühlzeit = 7 Bars.
  - Bollinger-Multiplikator = 3.0.
  - Stop-ATR-Multiplikator = 5.0.
  - Take-ATR-Multiplikator = 4.0.
  - Zeitausstieg = 0.
  - Minimaler ATR = 0.005.
- **Filter**:
  - Kategorie: Trend + Momentum
  - Richtung: Beide
  - Indikatoren: EMA, ALMA, ADX, RSI, ATR, Bollinger Bands
  - Stops: ATR-basiert
  - Komplexität: Moderat
  - Zeitrahmen: Kurz-/mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
