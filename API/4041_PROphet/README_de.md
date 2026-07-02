# PROphet-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie ist eine StockSharp-Portierung des MetaTrader 4-Expertenberaters „PROphet“. Das Original EA wertet den letzten Handelsdurchgang aus
g über vier historische Kerzen und nutzt diese gewichteten Bereiche, um neue Trades auszulösen. Es hält Positionen nur zwischen den offen
Europäische und US-amerikanische Sitzungen und folgt dem Stop-Loss, wenn sich der Preis um eine festgelegte Distanz zugunsten des Handels bewegt. Der StockSharp
Die Implementierung behält alle diese Mechanismen bei und passt sie gleichzeitig an das Netting-Modell an, das von StockSharp-Portfolios verwendet wird.

## Handelslogik
- Abonnieren Sie den konfigurierten Zeitrahmen (`CandleType`, Standard M5) und verarbeiten Sie nur fertige Kerzen.
- Behalten Sie die drei zuletzt abgeschlossenen Kerzen bei, um die von der Version MQL verwendete Indexierung `High[i]` und `Low[i]` zu reproduzieren.
- Berechnen Sie den langen Trigger `Qu(X1, X2, X3, X4)` und den kurzen Trigger `Qu(Y1, Y2, Y3, Y4)` für jeden Balken. Jeder Term multipliziert a
gewichteter Bereich (zum Beispiel `|High[1] - Low[2]|`) durch die entsprechende Gewichtung minus einhundert, genau wie im Originalcode.
- Erlauben Sie neue Einträge nur, wenn die aktuelle Stunde zwischen `TradeStartHour` und `TradeEndHour` (einschließlich) liegt. Dies ahmt den Mann nach
Aktuelles Handelsfenster vom MQL-Experten (standardmäßig 10:00 bis 18:00 Uhr).
- Verwenden Sie eine einzelne Marktorder, deren Volumen jegliches gegenteilige Risiko neutralisiert, bevor Sie die neue Position eröffnen. Dies spiegelt das Mag wider
IC-Nummernfilter aus der MetaTrader-Implementierung.

## Risikomanagement und Trailing
- Die Strategie rechnet die MetaTrader punktbasierten Stoppdistanzen über das Instrument `PriceStep` in Preiseinheiten um. Die Standardeinstellungen („B
uyStopLossPoints = 68`, `SellStopLossPoints = 72`) entsprechen den externen Variablen MQL.
- Sobald sich der Bid (für Long-Trades) oder der Ask (für Short-Trades) um `spread + 2 * stopDistance` über den bestehenden Stop hinausbewegt, th
Der Trailing Stop wird auf `currentPrice ± stopDistance` vorgezogen, wobei Live-Level-1-Daten verwendet werden, sofern verfügbar.
- Offene Geschäfte werden nach `ExitHour` zwangsweise geschlossen. Der Standardwert (18) reproduziert das ursprüngliche Verhalten beim Schließen der Position
s nach 18:00 Uhr Serverzeit.
- Bei Schutzausstiegen werden Market Orders verwendet, da die High-Level-Orders API von StockSharp nicht automatisch Stop-Orders generieren. Das hält
Verhalten bei allen Brokern deterministisch.

## Parameter
| Parameter | Beschreibung |
|-----------|-------------|
| `AllowBuy` | Ermöglicht lange Trades. |
| `AllowSell` | Ermöglicht Short-Trades. |
| `X1`, `X2`, `X3`, `X4` | Gewichtungen, die auf die Long-Side-Range-Komponenten innerhalb der `Qu`-Formel angewendet werden. |
| `BuyStopLossPoints` | Stop-Loss-Distanz für Long-Trades, ausgedrückt in MetaTrader Punkten. |
| `Y1`, `Y2`, `Y3`, `Y4` | Gewichtungen, die auf die Short-Side-Range-Komponenten innerhalb der `Qu`-Formel angewendet werden. |
| `SellStopLossPoints` | Stop-Loss-Distanz für Short-Trades, ausgedrückt in MetaTrader Punkten. |
| `TradeVolume` | Grundvolumen (Lots), das für neue Einträge verwendet wird. Zusätzliches Volumen wird automatisch hinzugefügt, um die gegenüberliegende Belichtung zu schließen. |
| `TradeStartHour` | Erste Stunde des Handelsfensters (einschließlich). |
| `TradeEndHour` | Letzte Stunde des Handelsfensters (einschließlich). |
| `ExitHour` | Stunde, nach der alle offenen Geschäfte geschlossen werden. |
| `CandleType` | Zeitrahmen der zur Analyse verwendeten Kerzen. |

## Notizen
- StockSharp Portfolios nutzen standardmäßig ein Netting. Wenn ein neues Signal erscheint, fügt die Strategie das erforderliche Volumen hinzu, um den Ex abzuflachen
Ist-Position vor dem Öffnen des neuen Handels, der das Einzelpositions-pro-Richtungs-Design aus dem MetaTrader-Expe reproduziert
rt.
- Das MQL-Skript verwendete die von `MarketInfo` gemeldete Symbolverteilung. Der Port ruft die Spanne aus Level-1-Daten ab, sofern verfügbar
andernfalls fällt er auf eine einzelne Preisstufe zurück.
- Da der Trailing-Stop am Ende jeder fertigen Kerze ausgewertet wird, kann es im Vergleich zum Tick-Level-Stop zu einem Slippage kommen
Aktualisierungen, die vom ursprünglichen EA durchgeführt wurden.
