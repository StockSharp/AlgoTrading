# Strategie Locker
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Gitterbasierte Hedging-Strategie, die Long- und Short-Marktorders abwechselt, um schwebende Verluste zu sichern und einen kleinen prozentualen Gewinn auf dem Kontosaldo zu erzielen.

## Handelslogik
* Öffnet die erste Long-Position mit dem konfigurierten Startvolumen, sobald die erste Kerze schließt.
* Verfolgt jeden nachfolgenden Einstieg und führt ein internes Hauptbuch der Kauf- und Verkaufsbeine, um den kombinierten unrealisierten und realisierten Gewinn zu schätzen.
* Wenn die Anzahl der aktiven Beine acht erreicht, schließt die Strategie das früheste verfügbare Kauf-/Verkaufspaar, um das Engagement unter Kontrolle zu halten, bevor auf dieser Kerze weitere Aktionen ausgeführt werden.
* Wenn der kombinierte Gewinn über den Zielprozentsatz des Portfoliowerts steigt, schließt sie alle verbleibenden Positionen und setzt den internen Zustand zurück.
* Wenn der kombinierte Gewinn unter das negative Ziel fällt, misst sie den Abstand zwischen dem letzten Einstiegspreis und dem aktuellen Marktpreis. Wenn sich der Preis um den konfigurierten Schritt nach oben bewegt hat, wird ein neues Short-Bein hinzugefügt; wenn sich der Preis um denselben Abstand nach unten bewegt hat, wird ein neues Long-Bein hinzugefügt.
* Jeder Abschluss verwendet Marktorders in der entgegengesetzten Richtung des aufgezeichneten Einstiegs, damit die Absicherung sofort neutralisiert wird.

## Parameter
* **Profit %** – Prozentsatz des aktuellen Portfoliowerts, der vor dem Glätten des Buches gesichert werden soll.
* **Start Volume** – Menge für den allerersten Long-Einstieg, der das Gitter startet.
* **Step Volume** – Menge für jede Hedging-Order, sobald die Verlustschwelle überschritten wird.
* **Step Points** – Anzahl der Preisschritte zwischen Gitterebenen; multipliziert mit dem Preisschritt des Instruments zur Berechnung des tatsächlichen Preisabstands.
* **Enable Automation** – Hauptschalter, der alle Handelslogik bei Deaktivierung pausiert.
* **Candle Type** – Kerzenserie, die zur Auslösung der Entscheidungslogik bei jeder abgeschlossenen Bar verwendet wird.

Die Konvertierung repliziert die ursprüngliche MetaTrader-Expertenlogik und passt dabei die Orderplatzierung an die StockSharp-High-Level-API an, während der detaillierte Handelszustand innerhalb der Strategie gespeichert wird, sodass die Gewinnberechnung der MQL-Version entspricht.
