# Smart Ass Trade-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Smart Ass Trade ist eine Multi-Zeitrahmen-Trendfolgestrategie, die aus der MQL-Implementierung konvertiert wurde.
Sie analysiert das MACD-Histogramm (OsMA) und einfache gleitende Durchschnitte mit Periode 20 auf 5-, 15- und 30-Minuten-Charts.
Ein täglicher Williams-%R-Filter blockiert Trades bei überkauften oder überverkauften Bedingungen.

## Algorithmus
1. MACD-Histogramm und SMA(20) auf den Zeitrahmen 5m, 15m und 30m berechnen.
2. Aufwärtstrend definieren, wenn das Histogramm steigt und SMA auf allen drei Zeitrahmen steigt.
3. Abwärtstrend definieren, wenn das Histogramm fällt und SMA auf allen drei Zeitrahmen fällt.
4. Täglichen Williams %R (Periode 26) verwenden, um Käufe über -2 oder Verkäufe unter -98 zu vermeiden.
5. Wenn alle Bedingungen übereinstimmen, einen Marktauftrag in der entsprechenden Richtung eröffnen.
6. Positionsgröße kann fest oder aus dem Kontowert optimiert werden.

## Parameter
- **Hedging** – ermöglicht das Eröffnen entgegengesetzter Positionen.
- **LotsOptimization** – aktiviert dynamische Lot-Berechnung.
- **Lots** – festes Handelsvolumen, wenn Optimierung deaktiviert ist.
- **AutomaticTakeProfit** – Platzhalter für dynamischen Take-Profit, derzeit nicht verwendet.
- **MinimumTakeProfit** – Gewinnziel in Punkten für manuellen Modus.
- **AutomaticStopLoss** – Platzhalter für dynamischen Stop-Loss, derzeit nicht verwendet.
- **StopLoss** – Stop-Loss in Punkten für manuellen Modus.
- **CandleType** – Basis-Zeitrahmen für Abonnements (Standard: 5 Minuten).

## Hinweise
Die Strategie verwendet die High-Level-API mit `SubscribeCandles`- und `Bind`-Aufrufen.
Take-Profit- und Stop-Loss-Werte sind für eine weitere Erweiterung vorgesehen; die aktuelle Version konzentriert sich auf
Signalgenerierung und Orderausführung.
