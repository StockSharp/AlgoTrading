# Backbone-Basket-Strategie (StockSharp)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Backbone Basket-Strategie** portiert den ursprünglichen Expertenberater MetaTrader 4 „Backbone.mq4“ auf die hohe Ebene StockSharp API. Das System sammelt Geld-/Brief-Extremwerte, um die anfängliche Handelsrichtung zu bestimmen, und wechselt dann zwischen Long- und Short-Körben. Jeder Korb wird schrittweise aufgebaut, wobei pro abgeschlossener Kerze eine Marktorder hinzugefügt wird, bis entweder die konfigurierte `MaxTrades`-Anzahl erreicht ist oder Schutzaufträge die Position schließen. Die Risikokontrolle wird durch ein fraktioniertes Risikomodell gewährleistet, das das Handelsvolumen anhand des Kontowerts und der Stop-Loss-Distanz skaliert.

## Marktdatenfluss
- **Kerzen (`CandleType`)** – abgeschlossene Kerzen beschleunigen die Entscheidungsfindung; Pro fertigem Barren kann nur eine Order erteilt werden, genau wie im MT4-Skript.
- **Orderbuch-Snapshots** – die besten Geld- und Briefwerte werden verfolgt, um Trailing-Stop-Berechnungen und die anfängliche „extreme“ Erkennungslogik zu reproduzieren.
- **Strategiestatus** – die Basis StockSharp `Strategy` behält die laufende Position, den durchschnittlichen Einstiegspreis und den PnL bei, der zur Verwaltung von Schutzaufträgen verwendet wird.

## Handelslogik
1. **Anfangskalibrierung** – obwohl keine Richtung definiert ist, zeichnet die Strategie den höchsten gesehenen Geld- und den niedrigsten Briefkurs auf. Wenn der Preis von diesen Extremwerten um `TrailingStopPoints * PriceStep` zurückgeht, wird die erste Korbrichtung gewählt.
2. **Auftragsreihenfolge** –
   - Wenn der letzte abgeschlossene Trade Short war (`_lastPositionDirection == -1`) und es keine offenen Trades gibt, wird ein neuer **Kaufmarkt**-Auftrag übermittelt.
   - Wenn der vorherige Trade long war (`_lastPositionDirection == 1`) und der Korb noch Kapazität hat, werden bei nachfolgenden Kerzen zusätzliche Kaufaufträge übermittelt.
   - Für Verkaufsaufträge gelten symmetrische Regeln, wenn der letzte Trade lang war.
3. **Volumengröße** – jede neue Bestellung ruft das MT4-inspirierte `Vol()`-Analogon auf. Der verfügbare Kontowert (aktueller Wert → Saldo → Startsaldo) wird mit `MaxRisk` multipliziert und durch die mit `PriceStepCost` in Geld umgerechnete Stop-Loss-Distanz dividiert. Das Ergebnis wird mit `VolumeStep` abgeglichen, durch `MinVolume`/`MaxVolume` begrenzt und abgelehnt, wenn es unter die Mindesthandelsgröße fällt.
4. **Schutzaufträge** – Sobald ein Trade ausgeführt wird, platziert die Strategie einen einzelnen Stop-Loss- und Take-Profit-Auftrag, der den gesamten Korb abdeckt. Entfernungen werden wie bei der MQL-Version in „Punkten“ (Preisschritten) ausgedrückt.
5. **Trailing Stop** – wenn sowohl `StopLossPoints` als auch `TrailingStopPoints` positiv sind, wird die Stop-Order erneut ausgegeben, um Gewinne zu sichern, wann immer sich der Preis um mehr als die Trailing-Distanz über den aufgezeichneten Einstiegspreis hinaus bewegt. Lange Körbe verwenden das beste Gebot als Referenz; Kurze Körbe verwenden die beste Frage.
6. **Korbabschluss** – wenn entweder die Stop-Loss- oder Take-Profit-Order ausgeführt wird, werden alle internen Zähler zurückgesetzt, sodass `LastPosition` unverändert bleibt, sodass die nächste Kerze einen Korb in die entgegengesetzte Richtung startet, was das ursprüngliche EA-Verhalten widerspiegelt.

## Money-Management
- Verwendet die gleiche Bruchformel `1 / (MaxTrades / MaxRisk - openTrades)` wie der MQL-Experte.
- Das Risikokapital wird ab `Portfolio.CurrentValue` geschätzt und fällt auf `CurrentBalance` oder `BeginBalance` zurück.
- Das Volumen wird verworfen, wenn die berechnete Größe nach der Ausrichtung an `VolumeStep` unter dem `MinVolume` des Instruments liegt.
- Stop-Loss- und Take-Profit-Orders werden bei jeder Volumenänderung neu erstellt, sodass der Schutz immer den gesamten Korb abdeckt.

## Parameter
| Name | Standard | Beschreibung |
| --- | --- | --- |
| `CandleType` | 15-minütiger Zeitrahmen | Kerzenintervall, das zum Auslösen neuer Entscheidungen verwendet wird. |
| `MaxRisk` | 0,5 | Bruchteil des Portfolios, der bei der Größenbestimmung der nächsten Order berücksichtigt wird. Muss positiv sein. |
| `MaxTrades` | 10 | Maximale Anzahl an Trades, die im aktuellen Korb gesammelt werden können. |
| `TakeProfitPoints` | 170 | Take-Profit-Distanz gemessen in Preisschritten. Zum Deaktivieren auf `0` setzen. |
| `StopLossPoints` | 40 | Stop-Loss-Distanz gemessen in Preisschritten. Erforderlich für Trailing- und Positionsgrößenbestimmung. |
| `TrailingStopPoints` | 300 | Trailing-Stop-Distanz in Preisschritten. Auf `0` setzen, um einen statischen Stopp beizubehalten. |

## Konvertierungshinweise
- Das Original EA ändert jede Bestellung einzeln; Die StockSharp-Version verwaltet einen aggregierten Stop-Loss und Take-Profit pro Korb, da StockSharp-Positionen standardmäßig saldiert werden.
- Die Volume-Größe hängt von `Security.PriceStepCost` ab. Wenn der Connector diesen Wert nicht bereitstellt, greift die Strategie auf die konfigurierte Eigenschaft `Volume` zurück.
- Nachlaufende Aktualisierungen werden angewendet, wenn eine neue Kerze eintrifft, was dem „Einmal pro Balken“-Verhalten des MT4-Skripts entspricht (das nur bei `Bars > PrevBars` funktionierte).
- Die alternierende Logik behält die zuletzt ausgeführte Richtung in `_lastPositionDirection` bei, sodass sobald ein Korb geschlossen wird, die nächste Kerze automatisch einen Korb in die entgegengesetzte Richtung öffnet, genau wie der Quellcode.
- Es wird nur die C#-Implementierung bereitgestellt. In diesem Verzeichnis gibt es keinen Python-Port.

## Nutzungstipps
- Weisen Sie Instrumente mit genauen `PriceStep`-, `PriceStepCost`- und Volumenmetadaten zu, um realistische Positionsgrößen zu erhalten.
- Stellen Sie beim Backtesting sicher, dass der Orderbuch-Feed verfügbar ist, damit die Trailing-Stop-Logik auf die besten Geld-/Briefwerte zugreifen kann.
- Um die aggressive Skalierung zu deaktivieren, erhöhen Sie `MaxTrades` oder verringern Sie `MaxRisk`, sodass die Ersetzung durch `Vol()` kleinere Volumina zurückgibt.
