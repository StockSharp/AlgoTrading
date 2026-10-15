# ExpXmaRangeBands-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie repliziert die Logik des MetaTrader-Beispiels „Exp_XMA_Range_Bands" mit der StockSharp High-Level API. Sie verwendet einen Keltner-Kanal, um dynamische Unterstützung und Widerstand basierend auf einem gleitenden Durchschnitt und dem Average True Range zu definieren. Trades werden ausgelöst, wenn der Preis nach einem Ausbruch wieder in den Kanal eintritt.

## Funktionsweise

1. Aufbau eines Keltner-Kanals mit:
   - EMA-Periode `MaLength`
   - ATR-Periode `RangeLength`
   - ATR-Multiplikator `Deviation`
2. Wenn eine Kerze oberhalb des vorherigen oberen Bandes schließt, wird eine bestehende Short-Position geschlossen. Schließt die nächste Kerze wieder innerhalb des Kanals (Schluss ≤ aktuelles oberes Band), wird eine Long-Position eröffnet.
3. Wenn eine Kerze unterhalb des vorherigen unteren Bandes schließt, wird eine bestehende Long-Position geschlossen. Schließt die nächste Kerze wieder innerhalb des Kanals (Schluss ≥ aktuelles unteres Band), wird eine Short-Position eröffnet.
4. Stop-Loss- und Take-Profit-Niveaus werden in Punkten ausgedrückt und nach Positionseröffnung angewendet.

## Parameter

- `MaLength` – EMA-Periode für die Kanalmitte.
- `RangeLength` – ATR-Periode für die Kanalbreite.
- `Deviation` – Multiplikator, der auf den ATR zur Bandberechnung angewendet wird.
- `StopLoss` – Stop-Loss in Punkten (wird über `Security.PriceStep` in Preis umgerechnet).
- `TakeProfit` – Take-Profit in Punkten (wird über `Security.PriceStep` in Preis umgerechnet).
- `CandleType` – Kerzen-Serie für Berechnungen.

## Indikatoren

- KeltnerChannels (EMA + ATR)
