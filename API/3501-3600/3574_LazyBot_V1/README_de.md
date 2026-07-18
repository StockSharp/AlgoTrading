# LazyBot V1-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

LazyBot V1 ist eine tägliche Breakout-Strategie, die vom ursprünglichen MetaTrader 5 Expert Advisor übernommen wurde. An jedem Handelstag platziert es ein Paar ausstehender Stop-Orders um die Preisspanne des Vortages herum und verwendet einen Trailing-Stop, um offene Positionen zu schützen. Die Konvertierung nutzt das High-Level-StockSharp API mit Kerzenabonnements und automatischer Auftragsverwaltung.

## Handelslogik

1. Warten Sie auf eine abgeschlossene Kerze des konfigurierten Zeitrahmens (standardmäßig täglich).
2. Stellen Sie an einem neuen Tag optional sicher, dass die aktuelle Serverzeit innerhalb des zulässigen Handelsfensters liegt, und überspringen Sie Wochenenden.
3. Stornieren Sie alle bestehenden Breakout-Pending-Orders, die durch die Strategie erstellt wurden.
4. Platzieren Sie einen Kaufstopp über dem Hoch des Vortages und einen Verkaufsstopp unter dem Tief des Vortages. Der Parameter `Breakout Offset (pips)` fügt beiden Ausbruchsebenen zusätzliche Distanz hinzu.
5. Wenn eine Order ausgelöst wird, halten Sie den schützenden Stop-Loss auf einem festen Abstand und verfolgen ihn immer dann, wenn der Preis zu Gunsten des Handels um mehr als den konfigurierten Pip-Abstand steigt.
6. Berechnen Sie das Volumen für die nächsten Aufträge neu, indem Sie entweder eine feste Losgröße oder das risikobasierte Größenmodul verwenden.

## Parameter

| Name | Beschreibung |
| --- | --- |
| Kerzentyp | Zeitrahmen, der zum Sammeln der Referenzkerzen verwendet wird (standardmäßig täglich). |
| Bot-Name | Der Wert wird zur einfacheren Nachverfolgung in die Bestellkommentare geschrieben. |
| Stop-Loss (Pips) | Distanz, die sowohl für den ersten als auch für den hinteren Stopp verwendet wird. |
| Breakout-Offset (Pips) | Bei der Platzierung ausstehender Aufträge wird ein zusätzlicher Abstand zum vorherigen Hoch/Tief angewendet. |
| Maximaler Spread (Pips) | Maximal zulässiger Spread vor der Erstellung neuer Breakout-Orders. Auf 0 setzen, um die Prüfung zu deaktivieren. |
| Nutzen Sie die Handelszeiten | Aktiviert den Startstundenfilter ähnlich dem ursprünglichen EA. |
| Startstunde | Erste Stunde (einschließlich), in der neue Bestellungen aufgegeben werden können. |
| Endstunde | Stunde, zu der die Planung neuer Bestellungen endet. Bei Gleichheit mit der Startstunde fungiert der Filter als einfache Untergrenze. |
| Nutzungsrisiko % | Ermöglicht eine risikobasierte Volumenberechnung. |
| Risiko % | Prozentsatz des Portfolio-Eigenkapitals, der zur Größenbestimmung von Positionen verwendet wird, wenn `Use Risk %` aktiviert ist. |
| Feste Lautstärke | Festes Auftragsvolumen, das verwendet wird, wenn die Risikogrößenbestimmung deaktiviert ist. Bei Null greift die Strategie auf die globale Eigenschaft `Volume` zurück (Standardwert ist 0,01). |

## Risikomanagement

* Der Trailing-Stop spiegelt die MetaTrader-Trailing-Logik wider, indem er den Stop-Loss `Stop Loss (pips)` vom besten Geld-/Briefkurs fernhält und ihn nur dann verengt, wenn ein besserer Preis erreicht wird.
* Der Spread-Filter schützt die Strategie davor, neue Breakout-Orders einzureichen, wenn der Markt zu breit ist.
* Die risikobasierte Größenbestimmung dividiert das zulässige monetäre Risiko (`equity * Risk %`) durch die in Preiseinheiten ausgedrückte Stop-Distanz und unterschreitet niemals die feste Losgröße.

## Zusätzliche Hinweise

* Bestellkommentare haben das Format `BotName;SymbolId;YYYYMMDD`, wodurch ausstehende Orders, die an verschiedenen Tagen erstellt wurden, leicht unterschieden werden können.
* Die Strategie abonniert Level1-Daten, um den aktuellen Spread für den Filter auszuwerten und Trail-Stops mit den neuesten Geld-/Briefwerten zu ermitteln.
* Trailing Stops werden bei jeder Kerzenaktualisierung und unmittelbar nach Füllungen erneut angewendet, um dem ursprünglichen EA-Verhalten zu entsprechen.
