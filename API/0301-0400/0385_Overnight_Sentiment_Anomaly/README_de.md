# Übernacht-Stimmungsanomalie-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt einen Aktien-ETF nur über Nacht, wenn ein externer Stimmungsindikator extremen Optimismus signalisiert. Beim Marktschluss wird der ETF gekauft, wenn der Indikator einen Schwellenwert überschreitet, und am nächsten Morgen verkauft, um die Übernacht-Drift bei positivem Sentiment zu nutzen.

Intraday-Daten werden nicht verwendet; der Algorithmus reagiert auf Tagesschluss-Stimmungswerte und platziert Marktaufträge beim Schluss und der Eröffnung des nächsten Tages.

## Details

- **Instrument**: Aktien-ETF und Stimmungsdatenreihe.
- **Signal**: Stimmungswert über dem konfigurierbaren `Threshold`.
- **Halteperiode**: Marktschluss bis zur Eröffnung des nächsten Tages.
- **Positionierung**: Long wenn Stimmung hoch, sonst ohne Position.
- **Risikokontrolle**: Auftrag übersprungen, wenn der Handelswert unter `MinTradeUsd` liegt.
