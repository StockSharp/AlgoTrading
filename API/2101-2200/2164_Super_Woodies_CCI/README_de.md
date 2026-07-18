# Super Woodies CCI-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist eine Umsetzung des originalen MQL5-Expert-Advisors *Exp_SuperWoodiesCCI*. Der Handel basiert auf der Richtung des Commodity Channel Index (CCI), der auf einem höheren Zeitrahmen berechnet wird.

## Logik

- CCI mit einer konfigurierbaren Periode berechnen.
- Wenn CCI über null kreuzt:
  - Optional Short-Positionen schließen.
  - Optional eine Long-Position eröffnen.
- Wenn CCI unter null kreuzt:
  - Optional Long-Positionen schließen.
  - Optional eine Short-Position eröffnen.

Es werden nur abgeschlossene Kerzen verarbeitet und die Strategie arbeitet mit einem festgelegten Kerzentyp.

## Parameter

- **CciPeriod** – Periode für die CCI-Berechnung.
- **CandleType** – Zeitrahmen der zu analysierenden Kerzen.
- **AllowLongEntry** – Long-Positionen eröffnen aktivieren.
- **AllowShortEntry** – Short-Positionen eröffnen aktivieren.
- **AllowLongExit** – Long-Positionen schließen aktivieren, wenn CCI negativ ist.
- **AllowShortExit** – Short-Positionen schließen aktivieren, wenn CCI positiv ist.

## Hinweise

Die Strategie verwendet die StockSharp-High-Level-API mit `SubscribeCandles` und Indikator-Binding. Die Handelsmethoden `BuyMarket` und `SellMarket` werden für das Positionsmanagement verwendet.
