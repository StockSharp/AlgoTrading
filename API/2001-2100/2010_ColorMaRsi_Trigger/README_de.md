# ColorMaRsi Trigger-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist ein StockSharp-Port des ursprünglichen MQL5-Experten `exp_colormarsi-trigger.mq5`. Sie vergleicht schnelle und langsame EMAs sowie schnelle und langsame RSI-Werte. Das kombinierte Signal nimmt die Werte -1, 0 oder +1 an. Eine Position wird eröffnet, wenn das vorherige Signal ein entgegengesetztes Vorzeichen zum aktuellen hat.

## Funktionsweise

- Wenn das Signal von positiv auf null oder negativ wechselt, wird eine Long-Position eröffnet und jede Short-Position geschlossen.
- Wenn das Signal von negativ auf null oder positiv wechselt, wird eine Short-Position eröffnet und jede Long-Position geschlossen.

## Parameter

- **Fast EMA** – Periode für den schnellen exponentiellen gleitenden Durchschnitt.
- **Slow EMA** – Periode für den langsamen exponentiellen gleitenden Durchschnitt.
- **Fast RSI** – Periode für den schnellen RSI.
- **Slow RSI** – Periode für den langsamen RSI.
- **Candle Type** – Zeitrahmen der für die Berechnung verwendeten Kerzen.

## Indikatoren

- Exponentieller gleitender Durchschnitt (schnell und langsam)
- Relative Stärke Index (schnell und langsam)

Es werden nur abgeschlossene Kerzen verarbeitet. Aufträge werden mit `BuyMarket` und `SellMarket` platziert.
