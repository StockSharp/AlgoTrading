# BTC Chop Reversal-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt kurzfristige Umkehrungen bei BTC, wenn der Preis ATR-Bänder testet und sich der Momentum verschiebt, unter Verwendung von EMA, ATR, RSI, MACD-Histogramm und einem Volumen-Spike-Filter.

## Details

- **Einstiegskriterien**:
  - **Long**: `Low < EMA - ATR*Mult` && `RSI < Oversold` && `MACD hist rising` && `Close > Open` && kein Verkaufsvolumen-Spike.
  - **Short**: `High > EMA + ATR*Mult` && `RSI > Overbought` && `MACD hist falling` && `Close < Open`.
- **Long/Short**: Beide.
- **Ausstiegskriterien**:
  - Positionen werden durch Take-Profit und Stop-Loss geschützt.
- **Stops**: Take Profit 0.75%, Stop Loss 0.4%.
- **Standardwerte**:
  - `EMA Period` = 23.
  - `ATR Length` = 55.
  - `ATR Multiplier` = 4.4.
  - `RSI Length` = 9.
  - `RSI Overbought` = 68.
  - `RSI Oversold` = 28.
  - `MACD Fast` = 14.
  - `MACD Slow` = 44.
  - `MACD Signal` = 3.
  - `Volume MA Length` = 16.
  - `Sell Spike Multiplier` = 1.5.
  - `Take Profit (%)` = 0.75.
  - `Stop Loss (%)` = 0.4.
- **Filter**:
  - Kategorie: Umkehr.
  - Richtung: Beide.
  - Indikatoren: EMA, ATR, RSI, MACD, Volumen.
  - Stops: Ja.
  - Komplexität: Mittel.
  - Zeitrahmen: Kurzfristig.
  - Saisonalität: Nein.
  - Neuronale Netze: Nein.
  - Divergenz: Nein.
  - Risikolevel: Mittel.
