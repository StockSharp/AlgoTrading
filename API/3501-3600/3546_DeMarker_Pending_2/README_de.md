# DeMarker Pending 2 Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die Strategie repliziert die Kernlogik des MetaTrader-Experten „DeMarker Pending 2“ unter Verwendung des StockSharp-High-Level-API. Es wertet einen DeMarker-Oszillator im Arbeitszeitraum aus und bereitet ausstehende Kauf- oder Verkaufseinträge vor, wenn der Indikator konfigurierbare Schwellenwerte überschreitet. Aufträge können als Stop- oder Limit-Anforderungen mit einem zusätzlichen Abstand zum aktuellen Marktpreis erstellt werden. Ein Sitzungsfilter, Spread Guard und Distanzkontrollen halten neue Einträge unter Kontrolle.

## Handelslogik

1. Abonnieren Sie die konfigurierte Kerzenserie und berechnen Sie den DeMarker-Indikator mit dem ausgewählten Zeitraum.
2. Wenn der vorherige Wert über dem unteren Niveau liegt und der aktuelle Wert darunter fällt, wird eine lange ausstehende Bestellung in die Warteschlange gestellt. Wenn der vorherige Wert unter dem oberen Niveau liegt und der aktuelle Wert darüber liegt, wird eine kurze Pending-Order in die Warteschlange gestellt. Es wird nur ein Signal pro Kerze verarbeitet.
3. Ausstehende Orders werden als Stop- oder Limit-Orders unter Verwendung der in Punkten ausgedrückten Einrückungsdistanz platziert. Bestehende Bestellungen können vor der neuen Anfrage storniert werden, wenn die Ersatzoption aktiviert ist. Die Strategie begrenzt die Gesamtzahl der offenen Positionen plus ausstehender Aufträge und erzwingt einen Mindestabstand zum aktuellen durchschnittlichen Positionspreis.
4. Long- und Short-Positionen nutzen optionale Stop-Loss-, Take-Profit- und Trailing-Logik. Schutzniveaus werden in Preispunkten berechnet und bei jeder geschlossenen Kerze überwacht. Trailing-Stops passen sich an, sobald die Position den Aktivierungsgewinn und die zusätzliche Trailing-Step-Distanz erzielt.
5. Ein Spread-Filter verhindert neue Orders, wenn die beste Geld-/Briefspanne den konfigurierten Schwellenwert überschreitet. Optionale Sitzungsgrenzen können neue Einträge außerhalb des zulässigen Handelsfensters deaktivieren.

## Parameter

| Name | Beschreibung |
| --- | --- |
| Funktionierende Kerzen | Zeitrahmen für Signale und Schutzkontrollen. |
| Bestellvolumen | Standardvolumen für ausstehende Orders. |
| Stop-Loss (Punkte) | Anfängliche Stop-Loss-Distanz in Preispunkten. |
| Take-Profit (Punkte) | Anfängliche Take-Profit-Distanz in Preispunkten. |
| Trailing-Aktivierung (Punkte) | Erforderlicher Gewinn, bevor der Trailing Stop greift. |
| Trailing Stop (Punkte) | Abstand zwischen Preis und Trailing Stop. |
| Trailing Step (Punkte) | Zusätzliche Verstärkung erforderlich, um den Trailing Stop zu bewegen. |
| Trail On Close | Aktualisieren Sie den Trailing Stop nur bei fertigen Kerzen, wenn er aktiviert ist. |
| Maximale Positionen | Maximale Anzahl offener Positionen plus ausstehende Aufträge. Null deaktiviert die Obergrenze. |
| Mindestentfernung (Punkte) | Mindestabstand vom aktuellen Positionspreis zu neuen ausstehenden Einträgen. |
| Verwenden Sie Stop-Orders | Platzieren Sie Stop-Orders, wenn dies zutrifft, andernfalls werden Limit-Orders verwendet. |
| Single ausstehend | Lassen Sie jeweils nur eine aktive ausstehende Bestellung zu. |
| Ausstehende ersetzen | Stornieren Sie ausstehende ausstehende Orders, bevor Sie eine neue aufgeben. |
| Ausstehender Offset (Punkte) | Einzug für neue ausstehende Preise im Verhältnis zum Markt. |
| Max Spread (Punkte) | Maximal zulässiger Spread, bevor die Auftragserteilung übersprungen wird. |
| Verwenden Sie den Sitzungsfilter | Aktivieren oder deaktivieren Sie den Handelsfensterfilter. |
| Startstunde/Minute, Endstunde/Minute | Sitzungsgrenzen, wenn der Sitzungsfilter aktiv ist. |
| DeMarker-Zeitraum | Mittelungszeitraum für den DeMarker-Oszillator. |
| Obere Ebene | Schwelle, die kurze Setups auslöst. |
| Untere Ebene | Schwelle, die lange Setups auslöst. |

## Notizen

* Auftragsablauf und Money-Management-Risikodimensionierung des ursprünglichen Experten werden nicht portiert. Stattdessen wird ein fester Volumenparameter verwendet.
* Stop-Loss- und Take-Profit-Level werden bei geschlossenen Kerzen anhand von Hoch-/Tiefpreisen bewertet, die von der Intrabar-Ausführung in MetaTrader abweichen können.
* Die Trailing-Logik wird nur bei geschlossenen Kerzen ausgewertet. Echtzeit-Tick-basiertes Trailing wird nicht reproduziert.
* Ausstehende Aufträge basieren auf den besten Geld-/Briefkursen, die von der Datenquelle bereitgestellt werden. Stellen Sie sicher, dass Abonnements der Stufe 1 verfügbar sind.
