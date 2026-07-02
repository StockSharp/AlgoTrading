# Strategie PresentTrend RMI Synergy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

PresentTrend RMI Synergy kombiniert einen RSI-basierten Momentum-Filter mit einem SuperTrend-ähnlichen ATR-Trailing-Stop. Einstiege erfolgen, wenn das Momentum die Schwellenwerte überschreitet und der Preis mit dem Trend übereinstimmt. Der Stop verfolgt den Preis dynamisch mit einem gleitenden Durchschnitt und einem ATR-Band.

Backtests zeigen stabile Performance auf Trendmärkten wie Krypto.

## Details

- **Einstiegskriterien**: RMI über 60 mit Preis über dem gleitenden Durchschnitt für Longs; RMI unter 40 mit Preis unter dem gleitenden Durchschnitt für Shorts.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: ATR-basierter Trailing-Stop.
- **Stops**: Ja.
- **Standardwerte**:
  - `RmiPeriod` = 21
  - `SuperTrendLength` = 5
  - `SuperTrendMultiplier` = 4.0m
  - `Direction` = TradeDirection.Both
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: RSI, ATR, SMA
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
