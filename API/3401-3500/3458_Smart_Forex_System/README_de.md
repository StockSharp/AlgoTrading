# Intelligente Forex-Systemstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die Smart Forex System Strategy ist eine StockSharp-Portierung des MetaTrader Expertenberaters „Smart Forex System“. Der Roboter kombiniert einen Einzelkerzen-Impulsfilter mit einem Mittelungsgitter im Martingal-Stil. Der erste Trade wird eröffnet, wenn die vorherige Kerze einen starken Richtungsschluss zeigt und sich der aktuelle Preis ausreichend vom Referenzschluss entfernt hat. Zusätzliche Einträge werden in festen Pip-Intervallen in die entgegengesetzte Richtung hinzugefügt, wobei die Positionsgröße um einen konfigurierbaren Multiplikator zunimmt. Die Strategie verwaltet Ausstiege durch durchschnittliche Take-Profit-Niveaus und einen Sicherheits-Stop-Loss, der an die letzte Netzbestellung gekoppelt ist.

## Handelslogik
- **Signalerzeugung**
  - Bewerten Sie die letzte abgeschlossene Kerze im ausgewählten Zeitrahmen.
  - Berechnen Sie ein Impulsverhältnis: `(current close - previous close) / previous close * 10,000`.
  - Wenn die vorherige Kerze bärisch ist und das Momentum unter dem negativen Schwellenwert liegt, kann ein Long-Korb beginnen.
  - Wenn die vorherige Kerze bullisch ist und das Momentum den positiven Schwellenwert überschreitet, kann ein Short-Basket beginnen.
  - Der Handel kann über den Parameter `Trading Mode` auf Long-Only, Short-Only oder beide Richtungen beschränkt oder vollständig deaktiviert werden.
- **Netzausbau**
  - Sobald ein Warenkorb vorhanden ist, werden neue Einträge immer dann hinzugefügt, wenn sich der Preis gegenüber der Position um mindestens `Grid Step` Pips gegenüber dem Preis der letzten Bestellung bewegt.
  - Jedes neue Bestellvolumen wird mit `Lot Multiplier` multipliziert. Die Volumina sind auf die Broker-Limits und den konfigurierten `Max Volume` beschränkt.
  - Der Warenkorb hört auf zu wachsen, wenn die Anzahl der Bestellungen `Max Trades` erreicht.
- **Exit-Management**
  - Ein harter Stop-Loss wird `Stop Loss` Pips vom Preis der letzten Order entfernt platziert. Durch das Überschreiten dieser Distanz wird der gesamte Korb geschlossen.
  - Die Take-Profit-Höhe hängt von der Warenkorbgröße ab:
    - Für eine einzelne Bestellung werden `First Take Profit` Pips vom volumengewichteten durchschnittlichen Einstiegspreis verwendet.
    - Bei mehreren Orders werden `Grid Take Profit` Pips vom gleichen durchschnittlichen Einstiegspreis verwendet, um kleinere Rebounds zu erzielen.
  - Exits werden an fertigen Kerzen verarbeitet, um sicherzustellen, dass die Indikatoren endgültige Werte haben.

## Hinweise zum Risikomanagement
- Die Martingal-ähnliche Positionsgröße erhöht das Risiko bei ungünstigen Trends dramatisch. Verwenden Sie konservative Multiplikatoren und Korbgrößen bei stark volatilen Instrumenten.
- Der Standard-Stop-Loss (400 Pips) ist absichtlich breit, um den ursprünglichen EA widerzuspiegeln. Erwägen Sie eine Ausrichtung auf den ATR des Instruments, wenn geringere Verluste erforderlich sind.
- Der Grid-Handel verbraucht schnell Marge. Stellen Sie sicher, dass die Kontohebelwirkung, die Vertragsgröße und die `Start Volume`-Parameter mit den Brokerspezifikationen übereinstimmen.

## Parameter
| Name | Beschreibung | Standard |
| --- | --- | --- |
| Handelsmodus | Zulässige Handelsrichtung (nur Long, nur Short, beides oder deaktiviert). | Lang und kurz |
| Impulsschwelle | Mindestimpuls in Pseudo-Pips, der zum Auslösen eines Signals erforderlich ist. | 1 |
| Lautstärke starten | Volumen der allerersten Bestellung in einem neuen Warenkorb. | 0,01 |
| Maximale Lautstärke | Eine feste Obergrenze gilt für jedes einzelne Bestellvolumen. | 2 |
| Lot-Multiplikator | Multiplikator, der bei der Dimensionierung nachfolgender Rasteraufträge verwendet wird. | 1.5 |
| Rasterschritt | Mindestabstand in Pips vor dem Hinzufügen der nächsten Bestellung. | 26 |
| Max Trades | Maximal zulässige Anzahl von Bestellungen pro Richtung. | 12 |
| Nehmen Sie zuerst den Gewinn mit | Take-Profit-Distanz in Pips, wenn nur eine Order offen ist. | 30 |
| Grid-Take-Profit | Take-Profit-Distanz in Pips, sobald der Warenkorb mehrere Aufträge enthält. | 7 |
| Stop-Loss | Stop-Distanz in Pips vom letzten Orderpreis. | 400 |
| Kerzentyp | Zeitrahmen, der für die Signalauswertung verwendet wird. | 1-Stunden-Kerzen |

## Empfohlene Verwendung
1. Verknüpfen Sie die Strategie mit einem Forex-Symbol mit ausreichender Liquidität und vorhersehbarem Spread.
2. Stellen Sie den `Candle Type` so ein, dass er mit dem Betriebszeitraum des ursprünglichen EA übereinstimmt (standardmäßig H1), oder passen Sie ihn an Ihren bevorzugten Horizont an.
3. Optimieren Sie den Rasterabstand, den Multiplikator und den Impulsfilter für historische Daten vor der Live-Bereitstellung.
4. Überwachen Sie die Margennutzung genau. Der Warenkorb kann schnell wachsen. Erwägen Sie daher die Kombination der Strategie mit einem kontoweiten Aktienschutz.
5. Vermeiden Sie Überschneidungen mit anderen netzbasierten Systemen auf demselben Instrument, um das Risiko zunehmender Verluste zu verringern.

## Unterschiede im Vergleich zur MetaTrader-Version
- Der StockSharp-Port arbeitet mit fertigen Kerzen statt mit Tick-für-Tick-Aktualisierungen, was das Rauschen reduziert und die Logik deterministisch macht.
- Das Auftragsvolumen wird mithilfe von StockSharp-Sicherheitsmetadaten (Min., Max. und Schritt) angepasst, um die Kompatibilität mit einer Vielzahl von Brokern sicherzustellen.
- Take-Profit- und Stop-Loss-Prüfungen werden innerhalb der Strategielogik durchgeführt, anstatt individuelle Orderänderungen für jede Rasterebene einzureichen.
