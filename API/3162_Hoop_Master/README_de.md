# Hoop Master-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht

Die Hoop Master-Strategie ist ein ausstehendes Ausbruch-System, das kontinuierlich zwei Stop-Orders um den aktuellen Preis hält. Der ursprüngliche MetaTrader 5 Expert Advisor platziert einen Buy-Stop oberhalb des Marktes und einen Sell-Stop unterhalb. Wenn eine Seite auslöst, wird die entgegengesetzte Order storniert und beide Seiten werden mit einem größeren Volumen neu erstellt. Der StockSharp-Port folgt derselben Idee, indem er Stop-Orders und optionale Martingal-Größenbestimmung innerhalb einer einzigen Strategieklasse verwaltet.

Die Strategie kann auch schützende Stop-Loss- und Take-Profit-Orders an jede offene Position anhängen. Ein Trailing-Stop-Modul bewegt den schützenden Stop schrittweise, wenn der Markt in Tradingrichtung voranschreitet.

## Handelslogik

1. Bei jeder abgeschlossenen Kerze berechnet die Strategie die Platzierungsniveaus für die Ausbruch-Stops neu.
2. Wenn keine Position offen ist, werden sowohl ein Buy-Stop als auch ein Sell-Stop bei einem konfigurierbaren Pip-Abstand vom aktuellen Bid/Ask registriert.
3. Wenn einer der ausstehenden Stops gefüllt wird, wird der entgegengesetzte Stop entfernt. Neue Ausbruch-Stops werden sofort mit dem doppelten Basisvolumen gesendet.
4. Nachdem ein Trade eröffnet wird, erstellt die Strategie unabhängige Stop-Loss- und Take-Profit-Orders. Eine Trailing-Engine kann den Stop zum Preis bewegen sobald die Bewegung groß genug ist.
5. Wenn die Position geschlossen wird, werden alle Schutzorders storniert und die Ausbruch-Orders beim nächsten Signal mit dem Basisvolumen neu initialisiert.

## Parameter

| Parameter | Beschreibung |
| --- | --- |
| **Candle Type** | Kerzendatentyp für die Kerzen-für-Kerzen-Logik. |
| **Order Volume** | Basisvolumen für jede Ausbruch-Order. Der Martingal-Schritt verwendet das Doppelte davon. |
| **Stop Loss (pips)** | Abstand in Pips zwischen Einstiegspreis und schutzender Stop-Order. Auf 0 setzen zum Deaktivieren. |
| **Take Profit (pips)** | Abstand in Pips zwischen Einstiegspreis und schützender Ziel-Order. Auf 0 setzen zum Deaktivieren. |
| **Trailing Stop (pips)** | Abstand beim Bewegen des Trailing Stops. Auf 0 setzen um Trailing zu deaktivieren. |
| **Trailing Step (pips)** | Minimale Preisverbesserung (in Pips) erforderlich bevor der Trailing Stop aktualisiert wird. |
| **Indent (pips)** | Offset, in Pips gemessen, oberhalb des Ask und unterhalb des Bid beim Platzieren von Ausbruch-Stops. |

## Order-Verwaltungsdetails

- Die Strategie verfolgt kontinuierlich die besten Bid/Ask-Kurse. Wenn Kurse nicht verfügbar sind, fällt sie auf den letzten Handelspreis oder Kerzenschlusskurs zurück.
- Alle Orders werden am Kursschritt des Instruments ausgerichtet um ungültige Preise zu vermeiden.
- Schützende Stop- und Take-Profit-Orders werden ersetzt wann immer eine neue Position erscheint.
- Trailing funktioniert nur wenn sowohl der Trailing-Abstand als auch die Schritt-Parameter über null liegen. Der Stop wird in Tradingrichtung bewegt wenn die gewünschte Verbesserung groß genug ist.

## Hinweise

- Stelle sicher dass der verbundene Broker oder Simulator Stop- und Limit-Orders für das gewählte Instrument unterstützt.
- Der Martingal-Schritt kann das Engagement schnell erhöhen. Passe das Basisvolumen an um innerhalb akzeptabler Risikolimits zu bleiben.
- Die Strategie erwartet Level1-Daten (Bid/Ask) zusammen mit Kerzendaten zu erhalten damit Ausbruchpreise genau berechnet werden können.
