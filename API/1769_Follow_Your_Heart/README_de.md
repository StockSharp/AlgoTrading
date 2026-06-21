# Follow Your Heart-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Diese Strategie ist eine StockSharp-Portierung des MetaTrader-Expert-Advisors „Follow Your Heart". Sie analysiert die letzten mehreren Kerzen und summiert die relativen Änderungen ihrer Eröffnungs-, Schluss-, Hoch- und Tiefspreise. Eine Long-Position wird eröffnet, wenn alle Änderungen über einem Schwellenwert liegen und der Gesamtwert positiv ist. Eine Short-Position wird bei den entgegengesetzten Bedingungen eröffnet. Es kann immer nur eine Position gleichzeitig existieren.

Positionen werden durch Gewinn- und Verlustlimits in Kontowährung sowie durch Take-Profit/Stop-Loss in Punkten geschützt. Optionale Handelssitzungen erlauben Signale nur innerhalb festgelegter Stunden.

## Parameter
- `Bars` – Anzahl der Kerzen zur Akkumulation von Preisänderungen. Standard: 6.
- `Level` – Schwellenwert für Eröffnungs- und Schlussänderungen. Standard: 2.3.
- `ProfitBuy` – Geld-Gewinnziel zum Schließen der Long-Position. Standard: 75.
- `ProfitSell` – Geld-Gewinnziel zum Schließen der Short-Position. Standard: 56.
- `LossBuy` – Geld-Verlustschwelle zum Schließen der Long-Position. Standard: -54.
- `LossSell` – Geld-Verlustschwelle zum Schließen der Short-Position. Standard: -51.
- `TakeProfit` – Take-Profit in Punkten. Standard: 550.
- `StopLoss` – Stop-Loss in Punkten. Standard: 550.
- `TradingHoursOn` – Sitzungsfilterung aktivieren. Standard: true.
- `OpenHourBuy` / `CloseHourBuy` – erlaubte Stunden für Kaufsignale. Standard: 6 / 12.
- `OpenHourSell` / `CloseHourSell` – erlaubte Stunden für Verkaufssignale. Standard: 4 / 10.
- `CandleType` – Kerzen-Zeitrahmen. Standard: 1 Minute.

## Strategielogik
1. Für jede abgeschlossene Kerze werden die relativen Änderungen von Eröffnung, Schluss, Hoch und Tief im Vergleich zur vorherigen Kerze berechnet und die gleitenden Summen aktualisiert.
2. Wenn keine Position besteht:
   - **Kauf**, wenn die Gesamtsumme positiv ist, sowohl Eröffnungs- als auch Schlussänderungen über `Level` liegen und die Schlussänderung größer als die Eröffnungsänderung während der Kaufsitzung ist.
   - **Verkauf**, wenn die Gesamtsumme negativ ist, sowohl Eröffnungs- als auch Schlussänderungen unter `-Level` liegen und die Schlussänderung kleiner als die Eröffnungsänderung während der Verkaufssitzung ist.
3. Wenn eine Position besteht, wird sie geschlossen, wenn Gewinn oder Verlust die konfigurierten Geldlimits überschreitet oder wenn der Preis sich um `TakeProfit`/`StopLoss` Punkte bewegt.

## Hinweise
- Es werden nur Marktorders verwendet.
- Das Geldmanagement aus dem Originalcode ist vereinfacht; das Positionsvolumen wird aus der `Volume`-Eigenschaft der Strategie entnommen.
