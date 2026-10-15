# Rsi-Teststrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
`RsiTestStrategy` wandelt den MetaTrader 4 Expertenberater **RSI_Test** in den hohen Level API von StockSharp um. Die Strategie kombiniert einen schnellen RSI-Momentumfilter mit einfacher Kerzenbestätigung und risikobewusster Positionsgrößenbestimmung. Es handelt ein einzelnes, durch die Host-Strategie definiertes Instrument und verwendet nur abgeschlossene Kerzen, was die Tick-to-Close-Logik des Originalcodes widerspiegelt.

## Handelsregeln
1. Berechnen Sie den Relative Strength Index mit dem konfigurierbaren `RsiPeriod`.
2. Gehen Sie long, wenn der RSI aus einem überverkauften Bereich (`BuyLevel`) steigt *und* die aktuelle Kerze über der vorherigen eröffnet.
3. Gehen Sie short, wenn der RSI aus einem überkauften Bereich (`SellLevel`) fällt *und* die aktuelle Kerze unterhalb der vorherigen eröffnet.
4. Beachten Sie das Limit von `MaxOpenPositions`. Ein Wert von `0` deaktiviert die Obergrenze; andernfalls darf das Nettorisiko `MaxOpenPositions * Volume` nicht überschreiten.
5. Verwalten Sie Ausstiege über einen treppenförmigen Trailing-Stop, der aktiviert wird, sobald der Preis um `TrailingDistanceSteps` Ticks über den durchschnittlichen Einstiegspreis steigt.
6. Es wird kein expliziter Take-Profit verwendet. Positionen werden geschlossen, wenn der Trailing Stop ausgelöst wird oder wenn die Handelssitzung die Strategie beendet.

## Positionsgröße und Risiko
* Die Strategie leitet eine vorläufige Ordergröße aus `RiskPercentage` des aktuellen Portfoliowerts ab. Wenn das Instrument Margendaten (`Security.MarginBuy`/`Security.MarginSell`) bereitstellt, wird das erforderliche Kapital pro Lot eingehalten; andernfalls wird der Betrag als konservativer Fallback durch den letzten Schlusskurs dividiert.
* Die Volumina werden auf `Security.VolumeStep` gerundet (oder auf zwei Dezimalstellen, wenn der Schritt unbekannt ist) und innerhalb des Bereichs `Security.MinVolume`/`Security.MaxVolume` eingegrenzt.
* Setzen Sie `RiskPercentage` auf Null, um die dynamische Größenanpassung zu deaktivieren und immer mit dem konfigurierten `Volume` zu handeln.

## Trailing-Stop-Verhalten
* `TrailingDistanceSteps` drückt die Distanz in Preisschritten (`Security.PriceStep`) aus. Wenn das Instrument keinen Schritt aufweist, wird die Distanz als direkter Preisversatz behandelt.
* Sobald das Schluss- oder Intrabar-Hoch das Aktivierungsniveau überschreitet (`entry + distance` für Long-Positionen, `entry - distance` für Short-Positionen), aktiviert die Strategie den Trailing Stop mit demselben Offset über dem Einstiegspreis.
* Der Schutzanschlag wird nur einmal pro Position angewendet, genau wie der ursprüngliche EA, der den Anschlag von der Gewinnschwelle zur ersten Stufe bewegt und dort hält.

## Parameter
| Name | Beschreibung | Standard |
| --- | --- | --- |
| `RsiPeriod` | RSI Lookback-Zeitraum. | `14` |
| `BuyLevel` | Überverkaufter Schwellenwert, der eine lange Einrichtung vorbereitet. | `12` |
| `SellLevel` | Überkaufter Schwellenwert, der ein kurzes Setup vorbereitet. | `88` |
| `RiskPercentage` | Portfolioanteil, der für die Positionsgrößenbestimmung verwendet wird. Stellen Sie `0` auf Ignorieren ein. | `10` |
| `TrailingDistanceSteps` | Distanz (in Preisschritten), die erforderlich ist, um den Trailing Stop zu aktivieren. | `50` |
| `MaxOpenPositions` | Maximale gleichzeitige Positionen; `0` entfernt das Limit. | `1` |
| `CandleType` | Primärer Zeitrahmen für Berechnungen. | `15` Minuten |
| `Volume` | Basisvolumen, wenn die Risikogröße nicht gelöst werden kann. | `1` |

## Nutzungshinweise
1. Hängen Sie die Strategie an ein Wertpapier an, das genaue `PriceStep`-, `VolumeStep`- und Margin-Metadaten offenlegt, um die beste Übereinstimmung mit dem MQL-Verhalten zu erzielen.
2. Der Algorithmus prüft nur abgeschlossene Kerzen (`CandleStates.Finished`), daher sollten Backtests denselben Zeitrahmen wie die Produktion verwenden.
3. `StartProtection()` aus der Basisklasse ist in `OnStarted` aktiviert, sodass die integrierte Risikokontrolle von StockSharp unerwartete Positionsreste verwalten kann.
4. Da der ursprüngliche Fachberater MetaTrader Optimierungen durch `GlobalVariableGet` eingeleitet hat, wird dieses Verhalten absichtlich weggelassen. Konfigurieren Sie die Parameter direkt in StockSharp.
5. Kombinieren Sie die Strategie mit einem Portfolio, das `Portfolio.CurrentValue` für eine dynamische Risikogrößenbestimmung aktualisiert. Ohne sie greift die Strategie elegant auf das statische `Volume` zurück.
