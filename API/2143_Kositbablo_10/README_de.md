# Kositbablo 10
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Multi-Zeitrahmen-EURUSD-Strategie auf Basis von RSI- und EMA-Signalen.
Sie prüft tägliche und stündliche Indikatoren und eröffnet Marktorders, wenn beide Trendfilter übereinstimmen.

## Parameter
- **Take Profit** – Take-Profit in Punkten.
- **Stop Loss** – Stop-Loss in Punkten.
- **Turbo Mode** – neue Trades erlauben, auch wenn bereits eine Position besteht.

## Regeln
- Long gehen, wenn Tages-RSI(11) < 60, Stunden-RSI(5) < 48 und EMA20 > EMA2.
- Short gehen, wenn Tages-RSI(22) > 38, Stunden-RSI(20) > 60 und EMA23 > EMA12.
- Trades erfolgen nur nach Abschluss der Stundenkerze.
