# Schlusskurs-vs-Vorherigen-Eröffnungskurs-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie vergleicht den Schlusskurs der letzten abgeschlossenen Kerze mit dem Eröffnungskurs der vorherigen Kerze.
Eine Long-Position wird eröffnet, wenn der letzte Schlusskurs über dem vorherigen Eröffnungskurs liegt, und eine Short-Position, wenn der letzte Schlusskurs unter dem vorherigen Eröffnungskurs liegt.

## Einstiegsregeln
- **Long**: Der Schlusskurs der zuletzt abgeschlossenen Kerze ist höher als der Eröffnungskurs der Kerze davor.
- **Short**: Der Schlusskurs der zuletzt abgeschlossenen Kerze ist niedriger als der Eröffnungskurs der Kerze davor.

## Risikomanagement
- Optionaler Stop-Loss und Take-Profit, gemessen in Punkten.
- Optionales Trailing des Stop-Loss.

## Parameter
- `Volume` – Ordervolumen.
- `UseStopLoss` – Stop-Loss aktivieren.
- `StopLoss` – Stop-Loss-Abstand in Punkten.
- `UseTakeProfit` – Take-Profit aktivieren.
- `TakeProfit` – Take-Profit-Abstand in Punkten.
- `UseTrailingStop` – Stop-Loss mit der Kursbewegung nachziehen.
- `CandleType` – Kerzenserie für Berechnungen.

## Hinweise
- Handelt nur auf vollständig ausgebildeten Kerzen.
- Dreht die Position um, wenn das entgegengesetzte Signal erscheint.
