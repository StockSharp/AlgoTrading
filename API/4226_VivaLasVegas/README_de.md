# Viva Las Vegas-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Viva Las Vegas ist ein spielerischer Experte für Geldmanagement, der das beigefügte Instrument nach dem Zufallsprinzip kauft oder verkauft und dann eines von fünf Wettsystemen über die Höhe des nächsten Einsatzes entscheiden lässt. Der Port StockSharp behält das ursprüngliche Verhalten von MetaTrader bei:
- Auswahl einer Handelsrichtung durch einen pseudozufälligen Münzwurf bei jedem neuen Versuch.
- Sofortige Platzierung symmetrischer Stop-Loss- und Take-Profit-Schutzmaßnahmen, ausgedrückt in Pips.
- Aktualisierung der Fortschrittssequenz, sobald die vorherige Position geschlossen wird, und Eröffnung einer neuen Position sofort.

Die Strategie bleibt daher ständig offen (jeweils eine offene Position) und zeigt, wie sich mehrere klassische Wettsysteme innerhalb des Handelsrahmens von StockSharp verhalten.

## Module zur Geldverwaltung
Der Parameter `MoneyManagement` wählt eines der folgenden Absteckmodelle aus, die alle `BaseVolume` als Ankerlosgröße verwenden:

1. **Martingale** – Verdoppelung der Lotgröße nach jedem verlorenen Trade und Zurücksetzen auf das Basisvolumen nach einem profitablen Trade.
2. **Negative Pyramide** – Verdoppelung der Lotgröße nach einem Verlust, aber Halbierung des Volumens nach einem Gewinn (niemals Unterschreiten des Basisvolumens).
3. **Labouchere** – Behalten Sie eine Zahlenfolge bei (Standard `1-2-3`), setzen Sie die Summe der ersten und letzten Zahlen, entfernen Sie sie nach einem Gewinn und hängen Sie ihre Summe nach einer Niederlage an.
4. **Oscar’s Grind** – Erhöhen Sie den Einsatz nach jedem Gewinn um das Basislos, bis ein Basislos Gewinn angesammelt wurde, und setzen Sie ihn dann zurück; Verluste mindern nur das laufende Ergebnis.
5. **31-System** – Durchlaufen Sie die Serie `1,1,1,2,2,4,4,8,8`, verdoppeln Sie das aktuelle Element nach dem ersten Sieg und setzen Sie es nach dem zweiten Sieg in Folge auf den Anfang zurück.

Alle Module orientieren sich eng an der ursprünglichen MQL-Implementierung, einschließlich der Reaktion von Volumenverläufen auf Unentschieden (Null-Gewinn-Trades werden als Verluste behandelt).

## Handelsablauf
1. Beim Start aktiviert die Strategie den Pseudozufallsgenerator (zeitbasiert bei `Seed = 0`) und aktiviert die Schutzmaschine von StockSharp mit symmetrischen Stopps und Zielen.
2. Wenn keine Position offen ist und kein Auftrag aussteht, fragt die Strategie das aktive Einsatzmodul nach der nächsten Lotgröße, rundet sie auf den `VolumeStep` des Instruments und wirft eine Münze, um zwischen `BuyMarket` und `SellMarket` zu wählen.
3. Sobald die Position ermittelt ist, verwaltet das Schutzmodul den Ausgang mithilfe des konfigurierten Pip-Abstands.
4. Wenn die Position wieder flach ist, wird das realisierte PnL-Delta ausgewertet:
   - Gewinn > 0 → das Modul erhält eine **Gewinnbenachrichtigung**.
   - Gewinn ≤ 0 → das Modul erhält eine **Verlustmeldung**.
5. Der Vorgang wird sofort in einer Schleife ausgeführt, sodass sich das Konto immer entweder in einem Handel befindet oder auf eine neue Auffüllung wartet.

Da immer nur eine Position existiert, lässt sich die Strategie leicht auf einem Diagramm verfolgen und spiegelt perfekt das Single-Ticket-Verhalten des ursprünglichen Expertenberaters wider.

## Parameter
| Name | Typ | Standard | Beschreibung |
| --- | --- | --- | --- |
| `StopTakePips` | `int` | `50` | Distanz (in Pips), angewendet auf Stop-Loss- und Take-Profit-Orders über `StartProtection`. |
| `BaseVolume` | `decimal` | `1` | Die Ankerlosgröße floss in den Fortschritt des Geldmanagements ein. |
| `MoneyManagement` | `MoneyManagementMode` | `Martingale` | Absteckalgorithmus, der steuert, wie die nächste Ordergröße berechnet wird. |
| `Seed` | `int` | `0` | Pseudozufallsgenerator-Seed. Ein Wert von Null schaltet auf einen zeitabhängigen Startwert um, sodass jeder Lauf anders ist. |

## Hinweise zur Implementierung
- Die Volumina werden auf `VolumeStep` des Instruments normalisiert und mit `MinVolume` / `MaxVolume` verglichen, um abgelehnte Bestellungen zu vermeiden.
- Stop/Take-Abstände werden mithilfe der klassischen MetaTrader-Regel in Preisschritte umgewandelt (`Digits` gleich 3 oder 5 impliziert zehn Ticks pro Pip).
- Der realisierte Gewinn wird über die `PnL`-Eigenschaft der Strategie gemessen, wodurch sichergestellt wird, dass Schutzexits und manuelle Schließungen die Abstecksequenz genau wie im Originalcode beeinflussen.
- Englische Inline-Kommentare heben die Entscheidungspunkte hervor und erleichtern so die Anpassung der Vorlage für Bildungszwecke oder kontrollierte Risikoexperimente.

## Anwendungstipps
- Wählen Sie einen Demo-Connector oder eine Wiedergabeumgebung. Der Algorithmus ist absichtlich riskant und zum Experimentieren gedacht.
- Passen Sie `BaseVolume` an die Kontraktgröße des Instruments an, bevor Sie mit der Strategie beginnen.
- Kombinieren Sie die Strategie mit StockSharp-Diagrammen, um zu beobachten, wie jedes Einsatzsystem die Positionsgröße im Laufe der Zeit erhöht oder verringert.
