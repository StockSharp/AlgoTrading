# Triple RVI-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt mit dem **Relative Vigor Index (RVI)** auf drei verschiedenen Zeitrahmen. Die längerfristigen RVI-Trends fungieren als Filter, während der kürzeste Zeitrahmen für Einstiege verwendet wird. Eine Long-Position wird eröffnet, wenn der kurzfristige RVI seine Signallinie nach unten kreuzt, während beide höheren Zeitrahmen bullisch bleiben. Eine Short-Position wird eröffnet, wenn der kurzfristige RVI seine Signallinie nach oben kreuzt und beide höheren Zeitrahmen bärisch sind. Positionen werden geschlossen, wenn ein Zeitrahmen einen Trendwechsel gegen die aktuelle Position anzeigt.

## Parameter
- **RviPeriod** – Periode zur Berechnung des RVI.
- **CandleType1** – Zeitrahmen des obersten RVI-Filters.
- **CandleType2** – Zeitrahmen des mittleren RVI-Filters.
- **CandleType3** – Handelszeitrahmen, in dem Einstiegssignale generiert werden.
- **Volume** – Ordergröße für Marktorders.

## Hinweise
- Es werden nur abgeschlossene Kerzen verarbeitet.
- Die Strategie verwendet die High-Level-API von StockSharp.
- Standard-Zeitrahmen entsprechen 30-, 15- und 5-Minuten-Kerzen.
