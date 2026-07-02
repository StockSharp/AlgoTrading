# Einfache Engulfing-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Simple Engulfing Strategy** repliziert das Verhalten der MetaTrader 4 Experten „Simple Engulf MT4 Buy“ und „Simple Engulf MT4 Sell“. Beide Experten erkennen Engulfing-Candlestick-Muster und eröffnen Trades in eine Richtung. Der StockSharp-Port führt beide Berater in einer konfigurierbaren Strategie zusammen, sodass der Händler das ursprüngliche Nur-Kauf-, Nur-Verkauf- oder kombinierte Verhalten innerhalb des StockSharp-Frameworks reproduzieren kann.

Die Strategie lauscht nur auf abgeschlossene Kerzen, was dem von der MetaTrader-Version verwendeten Bar-Close-Ausführungsstil entspricht. Bei der gesamten Auftragserteilung werden die übergeordneten StockSharp API (`SubscribeCandles`, `Bind`, `BuyMarket`, `SellMarket` und `StartProtection` verwendet, um den StockSharp-Codierungsrichtlinien nahe zu bleiben.

## Handelslogik
1. Erstellen Sie Kerzen basierend auf dem konfigurierten `CandleType`.
2. Warten Sie, bis die aktuelle Kerze fertig ist, und behalten Sie die zuvor abgeschlossene Kerze im Speicher.
3. Berechnen Sie die aktuelle Körpergröße der Kerze in Pips. Lehnen Sie das Muster ab, wenn es unter `MinBodyPips` oder über `MaxBodyPips` liegt (wenn der Maximalfilter mit einem positiven Wert aktiviert ist).
4. Erkennen Sie ein **bullisches Engulfing-Muster**, wenn:
   - Die vorherige Kerze ist bärisch (Schlusskurs unter Eröffnungswert).
   - Die aktuelle Kerze ist bullisch (Schlusskurs über Eröffnung).
   - Der aktuelle Eröffnungskurs liegt unter oder gleich dem vorherigen Schlusskurs.
   - Der aktuelle Schlusskurs liegt über oder gleich dem vorherigen Eröffnungskurs.
5. Erkennen Sie anhand der gespiegelten Bedingungen ein **bärisches Engulfing**-Muster.
6. Wenn ein gültiges Muster erscheint, stellen Sie sicher, dass automatisierter Handel zulässig ist (`IsFormedAndOnlineAndAllowTrading()`) und dass die konfigurierte Richtung den Handel zulässt:
   - `BuyOnly` repliziert den „simple engulf mt4 buy“-Roboter.
   - `SellOnly` repliziert den „Simple Engulf MT4 Sell“-Roboter.
   - `Both` ermöglicht den bidirektionalen Handel.
7. Verwenden Sie für jeden Eintrag den konfigurierten `TradeVolume`. Wenn die Strategie derzeit auf der gegenüberliegenden Seite positioniert ist, schließt sie die Position und dreht sie um, indem sie die absolute Positionsgröße zur Einstiegsreihenfolge hinzufügt, was dem MetaTrader-Verhalten beim Wechsel von Short zu Long (oder umgekehrt) entspricht.
8. Optionale Stop-Loss- und Take-Profit-Level werden über `StartProtection` unter Verwendung preisbasierter Einheiten angewendet. Sie wandeln die Pip-Abstände in Instrumentenpreisinkremente um, sodass StockSharp Schutzaufträge auf die gleiche Weise verwaltet wie die ursprünglichen Experten.

## Parameter
| Name | Standard | Beschreibung |
| --- | --- | --- |
| `CandleType` | `TimeFrame(15 minutes)` | Kerzentyp und Aggregationsintervall zur Erkennung von Mustern. |
| `TradeVolume` | `0.01` | Bestellvolumen pro Eintrag, identisch mit den MetaTrader-Experten. |
| `StopLossPips` | `20` | Stop-Loss-Distanz, ausgedrückt in Pips. Auf `0` setzen, um die Schutzanordnung zu deaktivieren. |
| `TakeProfitPips` | `20` | Take-Profit-Distanz, ausgedrückt in Pips. Auf `0` setzen, um die Schutzanordnung zu deaktivieren. |
| `MinBodyPips` | `0` | Mindestkerzenkörper (in Pips), der für ein gültiges Engulfing-Muster erforderlich ist. |
| `MaxBodyPips` | `50` | Maximal zulässiger Kerzenkörper (in Pips) für ein gültiges Engulfing-Muster. Verwenden Sie `0`, um den oberen Filter zu entfernen. |
| `Direction` | `BuyOnly` | Definiert, welche Seite(n) der ursprünglichen Berater ausgeführt werden sollen (`BuyOnly`, `SellOnly` oder `Both`). |

## Praktische Hinweise
- Die Pip-Größe passt sich automatisch an das gehandelte Instrument an, indem die `PriceStep` des Instruments und die Anzahl der Dezimalstellen analysiert werden. Dadurch wird sichergestellt, dass sich die Pip-Filter und Schutzbefehle wie die MetaTrader-Eingaben sowohl für 4-stellige als auch für 5-stellige Forex-Symbole verhalten.
- Schutzanordnungen werden nur gesendet, wenn `StopLossPips` oder `TakeProfitPips` positiv sind. Andernfalls überlässt die Strategie Ausgänge dem diskretionären Management oder anderen Automatisierungsmodulen.
- Da die Strategie auf vollständig abgeschlossene Kerzen wartet, werden am Ende jedes Balkens Signale generiert, wodurch ein Neuzeichnen innerhalb des Balkens vermieden wird.
- Durch hochrangige API-Aufrufe bleibt die Implementierung prägnant und sie folgen der Projektrichtlinie, vorgefertigte StockSharp-Komponenten der manuellen Auftragsabwicklung vorzuziehen.

## Unterschiede zum Original
- Beide MetaTrader-Berater werden in einer einzigen Strategie mit einem `Direction`-Parameter anstelle von zwei separaten Dateien kombiniert.
- Für eine bessere Sichtbarkeit bei der Ausführung in StockSharp-Terminals wurden Protokollierungs- und Diagrammhilfsfunktionen von StockSharp (optionale Kerzen- und Handelsdiagramme) hinzugefügt.
- Das Risikomanagement nutzt den `StartProtection`-Helfer von StockSharp, der Stop-Loss- und Take-Profit-Orders intern über die StockSharp-Engine verwaltet. Das resultierende Verhalten entspricht der Verwendung von Hardstopps in MetaTrader.
