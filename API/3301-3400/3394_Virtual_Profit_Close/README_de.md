# Virtual-Profit-Close-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Virtual Profit Close repliziert das Verhalten des MetaTrader 4 Expertenberaters *Virtual_Profit_Close.mq4*. Die Strategie beobachtet die
aktuelle Position des konfigurierten Wertpapiers und beendet sich, sobald ein virtuelles Gewinnziel erreicht ist. Im Gegensatz zu einer regulären Take-Profit-Order
Das Exit-Level wird intern ausgewertet, sodass keine Gewinnaufträge mehr im Auftragsbuch verbleiben. Ein konfigurierbarer Trailing Stop kann den Ausgang verschieben
Der Preis nähert sich dem Markt an, wenn der Handel Gewinne erwirtschaftet. Im Testmodus kann die Strategie automatisch Probenpositionen öffnen
um seine Logik zu demonstrieren.

## Konvertierungshinweise

- Tick-Ereignisse werden durch `SubscribeTrades().Bind(ProcessTrade).Start()` verbraucht, um die ursprüngliche `OnTick`-Routine nachzuahmen.
- MetaTrader „Punkte“ werden in Pips umgewandelt, indem `Security.PriceStep` überprüft und für 3/5-stellige Symbole angepasst wird.
- Virtuelle Gewinn- und Trailing-Berechnungen verwenden den aktuellen Geldkurs für Long-Positionen und den Briefkurs für Short-Positionen, passend zu MQL.
Implementierung, die auf `Bid`- und `Ask`-Preisen beruhte.
- Die Trailing-Stop-Logik wird nach der konfigurierten Gewinnschwelle aktiviert und hält den Stop in einem festen Abstand zum Markt
Preis, ähnlich dem wiederholten Aufruf von `OrderModify` in MQL.
- Ein Demonstrationsmodus ersetzt den ursprünglichen Strategietester-Helfer (`SendTest`), indem er Marktaufträge entsprechend der Auswahl öffnet
Richtung und Lautstärke. Optionale Schutzstopps werden mit `SetStopLoss` platziert.

## Parameter

| Parameter | Beschreibung |
|-----------|-------------|
| `ProfitPips` | Virtuelles Take-Profit-Niveau, ausgedrückt in MetaTrader Pips. Die Strategie schließt die Position, sobald der Gewinn diese Distanz überschreitet. |
| `UseTrailingStop` | Aktiviert das Nachlaufverhalten, wenn es auf `true` eingestellt ist. |
| `TrailingOffsetPips` | Abstand zwischen dem aktuellen Preis und dem Trailing Stop, sobald dieser aktiv ist. |
| `TrailingActivationPips` | Mindestgewinn in Pips erforderlich, bevor der Trailing Stop aktiviert wird. |
| `EnableDemoMode` | Öffnet jedes Mal automatisch Demonstrationsaufträge, wenn die Position flach wird. Nützlich für Backtests. |
| `DemoOrderDirection` | Richtung der Demo-Bestellungen (`Buy` oder `Sell`). |
| `DemoOrderVolume` | Für Demobestellungen eingereichtes Volumen. |
| `DemoStopPips` | Optionaler Schutzstopp für Demo-Orders, ausgedrückt in Pips. |

## Verhalten

1. Wenn die Strategie startet, berechnet sie die Pip-Größe und die Abstände für Gewinn-, Trailing- und Demo-Stops.
2. Jeder über `ProcessTrade` empfangene Tick wertet die aktuelle Position aus:
   - Long-Positionen werden geschlossen, wenn der Geldkurs den konfigurierten virtuellen Gewinn liefert.
   - Short-Positionen werden geschlossen, wenn der Briefkurs die gleiche Distanz in die entgegengesetzte Richtung zurücklegt.
3. Wenn Trailing aktiviert ist und der Aktivierungsschwellenwert erreicht ist, bewegt sich der Trailing Stop zusammen mit der günstigen Preisbewegung. Einmal
Wenn der Markt das nachlaufende Niveau überschreitet, sendet die Strategie einen Marktauftrag zum Ausstieg.
4. Der Demomodus kann automatisch eine neue Position eröffnen, wenn die Strategie flach wird, und stellt so die Nur-Tester-Funktion von wieder her
ursprünglicher Experte.

## Anforderungen

- Die Strategie benötigt Marktdaten auf Tick-Ebene, um präzise auf Preisänderungen reagieren zu können.
- Der Strategieinstanz sollte nur ein Symbol zugewiesen werden. Mehrere gleichzeitige Symbole werden nicht unterstützt und entsprechen dem Original
MQL-Implementierung, die das aktuelle Diagrammsymbol überwacht.
