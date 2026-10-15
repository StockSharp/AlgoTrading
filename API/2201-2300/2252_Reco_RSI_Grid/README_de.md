# Reco RSI-Gitter-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie reproduziert das Verhalten des originalen MetaTrader-Expertenberaters „Reco" mithilfe der High-Level-API von StockSharp. Der Algorithmus eröffnet eine erste Position auf Basis des Relative Strength Index (RSI) und platziert anschließend Gegenpositionen, die ein Gitter bilden. Der Abstand zwischen Gitterorders und deren Volumen wachsen geometrisch. Alle offenen Positionen werden gemeinsam geschlossen, wenn der kumulierte Gewinn oder Verlust vordefinierte Schwellenwerte erreicht.

## Handelslogik
- **Erstsignal** – RSI überschreitet die konfigurierten überkauften oder überverkauften Zonen. Eine Short-Position wird eröffnet, wenn RSI über dem Verkaufsniveau liegt, und eine Long-Position, wenn er unter dem Kaufniveau liegt.
- **Gittererweiterung** – nach der ersten Order beobachtet die Strategie die Preisbewegung gegenüber dem letzten Handel. Wenn sich der Preis um eine berechnete Distanz bewegt, wird eine entgegengesetzte Marktorder gesendet. Die Distanz erhöht sich mit dem *Distance Multiplier* bei jedem neuen Schritt und kann durch *Max Distance* und *Min Distance* begrenzt werden.
- **Volumenskalierung** – die Größe jeder neuen Order entspricht dem initialen *Lot* multipliziert mit *Lot Multiplier* potenziert mit der Anzahl bereits geöffneter Orders. Maximale und minimale Volumenlimits werden ebenfalls unterstützt.
- **Ausstiegsregeln** – wenn *Use Close Profit* aktiviert ist, werden alle Positionen geschlossen, wenn der aggregierte Gewinn größer ist als *Profit First Order* multipliziert mit *Profit Multiplier* für jede zusätzliche Order. Wenn *Use Close Lose* aktiviert ist, wird dieselbe Logik mit *Lose First Order* und *Lose Multiplier* auf Verluste angewendet.

## Parameter
| Name | Beschreibung |
|------|--------------|
| `RsiPeriod` | RSI-Indikator-Periode. |
| `RsiSellZone` | RSI-Niveau, das ein Verkaufssignal auslöst. |
| `RsiBuyZone` | RSI-Niveau, das ein Kaufsignal auslöst. |
| `StartDistance` | Anfangsdistanz zur letzten Order in Punkten. |
| `DistanceMultiplier` | Multiplikator, der auf die Distanz für jede zusätzliche Order angewendet wird. |
| `MaxDistance` | Obergrenze für das Distanzwachstum (0 deaktiviert). |
| `MinDistance` | Untergrenze für das Distanzwachstum (0 deaktiviert). |
| `MaxOrders` | Maximale Anzahl gleichzeitig offener Orders (0 bedeutet kein Limit). |
| `Lot` | Basis-Order-Volumen. |
| `LotMultiplier` | Multiplikator für die Volumenskalierung. |
| `MaxLot` | Maximal erlaubtes Volumen pro Order (0 deaktiviert). |
| `MinLot` | Minimal erlaubtes Volumen pro Order (0 deaktiviert). |
| `UseCloseProfit` | Schließen aller Positionen bei Gewinnziel aktivieren. |
| `ProfitFirstOrder` | Gewinnziel für die erste Order. |
| `ProfitMultiplier` | Gewinnmultiplikator für nachfolgende Orders. |
| `UseCloseLose` | Schließen aller Positionen bei Verlustschwelle aktivieren. |
| `LoseFirstOrder` | Verlustschwelle für die erste Order. |
| `LoseMultiplier` | Verlustmultiplikator für nachfolgende Orders. |
| `PointMultiplier` | Multiplikator, der auf den Preisschritt des Instruments angewendet wird, um einen Punkt zu berechnen. |
| `CandleType` | Kerzentyp für die Indikatorberechnungen. |

## Hinweise
- Die Strategie arbeitet mit Marktorders und setzt sofortige Ausführung voraus.
- Positionen werden genetzt: Eine entgegengesetzte Order kann die aktuelle Position reduzieren oder umkehren.
- Die Strategie verwendet Tabulatoren zur Einrückung und englische Kommentare gemäß den Projektkonventionen.
