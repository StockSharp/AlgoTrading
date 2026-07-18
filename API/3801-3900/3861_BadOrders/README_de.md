# BadOrders-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **BadOrders-Strategie** ist eine direkte Portierung des MetaTrader 4 Expertenberaters `BadOrders.mq4`. Das ursprüngliche Skript wurde absichtlich geschrieben, um zu demonstrieren, wie eine fehlerhafte Auftragsverwaltung zu abgelehnten Geschäften führt. Bei jedem eingehenden Häkchen:

1. Schließt die zuletzt geöffnete Position zwangsweise zum aktuellen Geldkurs.
2. Platziert einen neuen Kaufstopp 100 Punkte über dem Gebot.
3. Ändert diese ausstehende Order sofort so, dass sie 100 Punkte *unter* dem Gebot liegt, was gegen die Abstandsregeln des Brokers verstößt und einen Fehler provoziert.

Die StockSharp-Version reproduziert dieses Verhalten mit der High-Level-Version API. Es abonniert Kurse der Stufe 1, um das beste Gebot zu überwachen, und spielt denselben Abschluss-Platz-Invalidierung-Zyklus immer dann ab, wenn ein Kurs eintrifft.

## Details zur Implementierung
- **Datenstrom**: `SubscribeLevel1()` wird verwendet, da das MT4-Skript auf jeden Tick und nicht auf Kerzenabschlüsse reagiert.
- **Auftragsverwaltung**: Offene Positionen werden mit dem `ClosePosition()`-Helfer geschlossen. Ausstehende Stopps werden über `BuyStop()` und `ReRegisterOrder()` verwaltet, sodass wir die Stop-Order sofort auf einen illegalen Preis verschieben können und so den fehlerhaften Workflow des Quellcodes nachahmen.
- **Preisnormalisierung**: Alle Preise werden über `Security.ShrinkPrice()` normalisiert und das MetaTrader-Konzept von `Point` wird durch das Instrument `PriceStep` emuliert. Wenn keine Tick-Größe verfügbar ist, fällt die Strategie auf `0.0001` zurück.
- **Schutzlogik**: Vor dem Aufruf von `ClosePosition()` prüft der Code, ob Liquidationsaufträge vorhanden sind, um zu vermeiden, dass doppelte Exit-Anfragen gestapelt werden.

## Parameter
| Name | Beschreibung | Standard |
| ---- | ----------- | ------- |
| `DistancePoints` | Distanz in MetaTrader „Punkten“, die über und unter dem aktuellen Gebot hinzugefügt wird, wenn die Stop-Order platziert oder erneut registriert wird. | `100` |

## Zusammenfassung des Verhaltens
- Immer wenn sich das Gebot ändert, versucht die Strategie, jede offene Position zu reduzieren.
- Ein Kaufstopp wird um `bid + DistancePoints * PointValue` übermittelt, nachdem die Position geschlossen wurde.
- Dieselbe Bestellung wird sofort erneut auf `bid - DistancePoints * PointValue` registriert, was gegen die Börsenregeln verstößt und voraussichtlich scheitern wird – was genau die absichtlichen Fehler in `BadOrders.mq4` widerspiegelt.

> **Hinweis**: Dieses Projekt dient lediglich der Parität mit dem MT4-Beispiel und ist nicht für den Live-Handel gedacht.
