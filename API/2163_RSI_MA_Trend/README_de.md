# RSI MA Trend-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie kombiniert den Relative Strength Index (RSI) mit einem Trendfilter aus gleitenden Durchschnitten.
Eine Long-Position wird eröffnet, wenn der RSI unter ein festgelegtes Kaufniveau fällt und der schnelle gleitende Durchschnitt über dem langsamen liegt.
Eine Short-Position wird eröffnet, wenn der RSI über ein festgelegtes Verkaufsniveau steigt und der schnelle gleitende Durchschnitt unter dem langsamen liegt.

## Parameter

- `RSI Period` – Länge des RSI-Indikators.
- `RSI Buy Level` – RSI-Wert, unterhalb dessen eine Long-Position eröffnet wird.
- `RSI Sell Level` – RSI-Wert, oberhalb dessen eine Short-Position eröffnet wird.
- `Fast MA Period` – Periode des schnellen gleitenden Durchschnitts.
- `Slow MA Period` – Periode des langsamen gleitenden Durchschnitts.
- `Candle Type` – Kerzenserie für die Berechnungen.

## Logik

1. Abonnieren der ausgewählten Kerzenserie.
2. RSI, schnellen MA und langsamen MA für jede abgeschlossene Kerze berechnen.
3. Aufwärtstrend erkennen, wenn schneller MA über langsamem MA liegt, Abwärtstrend wenn darunter.
4. Long einsteigen, wenn RSI < Kaufniveau und Trend aufwärts, dabei Short-Positionen schließen falls vorhanden.
5. Short einsteigen, wenn RSI > Verkaufsniveau und Trend abwärts, dabei Long-Positionen schließen falls vorhanden.

## Hinweise

- Die Strategie verwendet Marktaufträge für Einstiege.
- Handelssignale werden nur auf abgeschlossenen Kerzen verarbeitet.
- Parameter sind für die Optimierung in der Benutzeroberfläche verfügbar.
