# Multi-Currency-Vorlage MT5-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die **MultiCurrency Template MT5 Strategy** repliziert das Verhalten des gleichnamigen Expertenberaters MetaTrader. Es handelt ein einfaches Zwei-Kerzen-Muster im täglichen Zeitrahmen und ermöglicht es dem Benutzer gleichzeitig, einen Korb von Instrumenten zu bedienen. Die Strategie eröffnet eine Anfangsposition nur dann, wenn die vorherige Tageskerze bullisch oder bärisch genug ist, um das Muster auszulösen, und verwaltet den Handel dann in einem schnelleren Kontrollzeitrahmen. Ein Martingal-Mittelungsblock fügt zusätzliche Tickets hinzu, wenn sich der Preis gegenüber der Position um eine konfigurierbare Anzahl von MetaTrader Punkten bewegt, während die Exit-Logik feste Take-Profits, Break-Even-Mittelung und einen optionalen Trailing Stop kombiniert.

Der StockSharp-Port behält die Verwaltung mehrerer Symbole bei, indem er dem Benutzer die Definition einer durch Kommas getrennten Liste von Wertpapieren ermöglicht. Jedes Symbol wird unabhängig mit seinem eigenen Tracking-Kontext, seinem eigenen Positionskorb und seinen eigenen Geldverwaltungswerten behandelt. Wenn der Parameter `TradeMultipair` deaktiviert ist, handelt die Strategie den Haupt-`Security`, der an die Strategieinstanz angehängt ist.

## Signalerzeugung

* Die Strategie abonniert den `SignalCandleType` (standardmäßig täglich) und speichert zwei aufeinanderfolgende fertige Kerzen.
* Ein **Long**-Setup wird erkannt, wenn der letzte Schlusskurs unter dem vorherigen Eröffnungskurs liegt und die vorherige Kerze über ihrem Eröffnungskurs schloss.
* Ein **Short**-Setup wird erkannt, wenn der letzte Schlusskurs über dem vorherigen Eröffnungskurs liegt und die vorherige Kerze unter ihrem Eröffnungskurs schloss.
* Es kann immer nur eine Richtung aktiv sein. Neue Trades werden ignoriert, bis der aktuelle Korb vollständig geschlossen ist.

## Auftragsausführung

* Einträge werden am Markt mit dem durch `Lots` definierten Volumen eingereicht.
* Wenn `NewBarTrade` aktiviert ist, wartet die Strategie auf eine fertige Kerze am `TradeCandleType`, bevor sie einen neuen Eintrag aktiviert. Die Flagge wird bei der ersten Handelsentscheidung verwendet, um das Verhalten MetaTrader „Nur mit einem neuen Balken handeln“ zu reproduzieren.
* Stop-Loss- und Take-Profit-Ziele werden mit MetaTrader Pips (multipliziert mit der erkannten Pip-Größe) initialisiert, sodass der Abstand dem ursprünglichen Experten entspricht.
* Wenn `EnableMartingale` wahr ist, fügt die Strategie Durchschnittstickets hinzu, wenn der Preis um `StepPoints` vom besten Eintrag des aktuellen Warenkorbs abweicht. Die Volumina werden um `NextLotMultiplier` skaliert, erhöht auf die Anzahl der bereits offenen Tickets auf dieser Seite.

## Handelsmanagement

* Das Take-Profit-Verhalten hängt von `EnableTakeProfitAverage` ab:
  * Wenn die Option deaktiviert ist, bleibt der Take-Profit im anfänglichen Abstand, der durch `TakeProfitPips` vom besten Preis im Warenkorb definiert wird.
  * Wenn diese Option aktiviert ist und der Warenkorb mindestens zwei Tickets enthält, wird das Ziel auf den Break-Even-Preis plus `TakeProfitOffsetPoints` verschoben.
* Stop-Loss-Level werden nach jeder Füllung neu berechnet, sodass sie den schlechtesten Preis im Korb widerspiegeln.
* Ein Trailing Stop funktioniert, wenn nur ein Ticket geöffnet ist. Es reproduziert die MetaTrader-Logik, indem es zunächst zum Break-Even plus `TrailingStopPoints` springt, sobald die Bewegung `TrailingStopPoints + TrailingStepPoints` überschreitet, und dann dem Preis mit der gleichen Distanz folgt, sobald der Handel weiter voranschreitet.
* Risikoausstiege lösen eine Marktorder aus, die den gesamten Korb in einer Transaktion pro Seite schließt.

## Parameter

| Parameter | Beschreibung |
| --- | --- |
| `Lots` | Basishandelsvolumen für das erste Ticket in jedem Korb. |
| `StopLossPips` | Anfängliche Stop-Loss-Distanz, ausgedrückt in MetaTrader Pips. |
| `TakeProfitPips` | Anfängliche Take-Profit-Distanz in MetaTrader Pips. |
| `TrailingStopPoints` | Nachlaufdistanz (MetaTrader Punkte), wenn nur ein Ticket aktiv ist. |
| `TrailingStepPoints` | Zusätzlicher Puffer (Punkte) erforderlich, bevor der Trailing Stop erneut verschoben wird. |
| `SlippagePoints` | Reserviert für Analysen zur Nachahmung der Slippage-Eingabe MetaTrader (wird nicht zur Ausführung verwendet). |
| `NewBarTrade` | Aktiviert den Filter „Trade-on-new-bar“ basierend auf den Kerzen `TradeCandleType`. |
| `TradeCandleType` | Heartbeat-Zeitrahmen, der die Erkennung neuer Barren und das Geldmanagement vorantreibt. |
| `TradeMultipair` | Wenn true, wird der Multisymbolmodus aktiviert. |
| `PairsToTrade` | Durch Kommas getrennte Liste zusätzlicher Sicherheitskennungen, aufgelöst durch `GetSecurity`. |
| `Commentary` | Der Bestellkommentar wird als Referenz aufbewahrt. |
| `EnableMartingale` | Aktiviert den Mittelungsblock, der Tickets bei ungünstigen Bewegungen hinzufügt. |
| `NextLotMultiplier` | Der Multiplikator wird auf das vorherige Ticketvolumen angewendet, wenn eine neue Durchschnittsbestellung aufgegeben wird. |
| `StepPoints` | Entfernung in MetaTrader Punkten, die den nächsten Mittelungsauftrag auslöst. |
| `EnableTakeProfitAverage` | Aktiviert das Break-Even + Offset-Ziel für Körbe mit mehreren Tickets. |
| `TakeProfitOffsetPoints` | MetaTrader Punkte werden über (Long) oder unter (Short) dem Break-Even-Preis hinzugefügt, wenn die Mittelung aktiv ist. |
| `SignalCandleType` | Zeitrahmen, der zum Erstellen des Zwei-Kerzen-Musters verwendet wird (standardmäßig täglich). |

## Notizen

* Die Strategie basiert auf Marktaufträgen für Ein- und Ausstiege; Maklerseitige Schutzanordnungen von MetaTrader werden intern emuliert.
* `PairsToTrade` muss Bezeichner enthalten, die der verbundene Connector auflösen kann. Unbekannte Symbole werden stillschweigend übersprungen.
* Die Martingal- und Trailing-Blöcke funktionieren pro Symbolkontext, daher verwaltet jedes Wertpapier einen unabhängigen Korb.
* `SlippagePoints` wird der Vollständigkeit halber beibehalten, hat jedoch keinen Einfluss auf die Ausführung in StockSharp.
