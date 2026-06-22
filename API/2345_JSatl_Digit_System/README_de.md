# JSatl Digit-System-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Dieses Beispiel demonstriert eine vereinfachte Portierung des MQL5-Expert-Advisors „JSatl Digit System" zu StockSharp.

Die Strategie verwendet den Jurik Moving Average (JMA), um einen digitalen Trendzustand zu erzeugen:

- Wenn der Schlusskurs über dem JMA liegt, wird der Zustand zu **aufwärts**.
- Wenn der Schlusskurs unter dem JMA liegt, wird der Zustand zu **abwärts**.

Wenn der Zustand auf aufwärts wechselt, können Short-Positionen geschlossen und/oder eine Long-Position geöffnet werden, abhängig von den Parametern. Wenn der Zustand auf abwärts wechselt, können Long-Positionen geschlossen und/oder eine Short-Position geöffnet werden.

**Parameter**

- `JmaLength` – JMA-Periode.
- `CandleType` – Kerzenserie für die Berechnungen.
- `StopLossPercent` – schützender Stop-Loss in Prozent.
- `TakeProfitPercent` – schützender Take-Profit in Prozent.
- `BuyPosOpen`, `SellPosOpen`, `BuyPosClose`, `SellPosClose` – Aktivieren oder Deaktivieren von Aktionen für die entsprechenden Signale.
