# SJ NIFTY-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Trendfolge-Strategie unter Verwendung von SuperTrend, VWAP, RSI und EMA200. Die Keltner-Kanal-Basis dient als optionaler Trendfilter. Die Positionsgröße wird aus dem Risikoprozentsatz des Kapitals mit Stop-Loss und risiko-basiertem Take-Profit berechnet.

## Details

- **Einstiegskriterien**:
  - **Long**: Schlusskurs > SuperTrend && Schlusskurs > VWAP && RSI > Überkauft && Schlusskurs > EMA200 && Keltner-Basis-Filter && Schlusskurs > vorheriges Hoch.
  - **Short**: Schlusskurs < SuperTrend && Schlusskurs < VWAP && RSI < Überverkauft && Schlusskurs < EMA200 && Keltner-Basis-Filter && Schlusskurs < vorheriges Tief.
- **Ausstiegskriterien**: Stop-Loss oder Take-Profit basierend auf dem Risikoverhältnis.
- **Positionsgröße**: Risikoprozentsatz des Portfolios geteilt durch den Stop-Abstand, gerundet auf die Lotgröße.
- **Indikatoren**: SuperTrend, VWAP, RSI, EMA, Keltner Channels.
