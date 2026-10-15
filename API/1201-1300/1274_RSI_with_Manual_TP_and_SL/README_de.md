# RSI-Strategie mit manuellem TP und SL
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Implementiert eine RSI-Strategie, die Long geht, wenn der RSI das Überverkauft-Niveau nach oben kreuzt und der Schlusskurs über 70% des höchsten Schlusskurses der letzten 50 Kerzen liegt. Geht Short, wenn der RSI das Überkauft-Niveau nach unten kreuzt und der Schlusskurs unter 130% des niedrigsten Schlusskurses der letzten 50 Kerzen liegt. Positionen werden mit prozentualem Take-Profit und Stop-Loss abgesichert.

## Parameter

- **Candle Type** – Kerzenzeitrahmen.
- **RSI Length** – Periode des RSI.
- **Oversold Level** – RSI-Schwellenwert für Long-Einstiege.
- **Overbought Level** – RSI-Schwellenwert für Short-Einstiege.
- **Lookback** – Periode für die Hoch-/Tief-Berechnung.
- **Take Profit %** – Take-Profit-Prozentsatz.
- **Stop Loss %** – Stop-Loss-Prozentsatz.
