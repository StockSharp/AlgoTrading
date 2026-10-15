# Billy Expert-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
- Konvertiert vom ursprünglichen MetaTrader 4-Experten „Billy_expert.mq4“.
- Long-Only-Momentum-Strategie, die auf vier aufeinanderfolgende absteigende Hochs wartet und vor dem Einstieg eröffnet.
- Verwendet zwei stochastische Oszillatoren (schnell im Handelszeitrahmen, langsam in einem höheren Zeitrahmen), um zu bestätigen, dass sich die Dynamik nach oben verschiebt.
- Konzipiert für Spot-FX-Paare, kann aber auf jedes Instrument angewendet werden, das minutenbasierte Kerzen bereitstellt.

## Signallogik
### Preisaktionsfilter
1. Bewerten Sie fertige Kerzen im ersten Zeitrahmen.
2. Erfordert vier aufeinanderfolgende Kerzen, bei denen sowohl das Hoch als auch die Eröffnung sinken. Dadurch werden die MT4-Prüfungen `High[0] < High[1] < High[2] < High[3]` und `Open[0] < Open[1] < Open[2] < Open[3]` neu erstellt.
3. Das Muster deutet auf eine erschöpfte Abwärtsbewegung hin und bereitet die Strategie auf einen Umkehrhandel vor.

### Bestätigung des Oszillators
1. Berechnen Sie einen schnellen stochastischen Oszillator im Handelszeitrahmen und einen langsamen stochastischen Oszillator im Bestätigungszeitrahmen.
2. Fordern Sie für jeden Oszillator, dass die %K-Linie sowohl bei der aktuellen als auch bei der vorherigen abgeschlossenen Kerze (`%K(0) > %D(0)` und `%K(1) > %D(1)`) über der %D-Linie liegt.
3. Der Handel wird nur dann ausgelöst, wenn beide Oszillatoren gleichzeitig eine Aufwärtsdynamik bestätigen.

## Auftragsverwaltung
- Einträge: Marktkäufe in der Größe des Strategieparameters `Volume` (wenn eine Short-Position vorhanden ist, wird diese automatisch geschlossen und rückgängig gemacht).
- Stop-Loss: Fester Abstand unter dem Füllpreis mithilfe des Parameters `Stop Loss (pts)`. Ein Wert von `0` deaktiviert den Stopp.
- Take Profit: Fester Abstand über dem Füllpreis mithilfe des Parameters `Take Profit (pts)`. Ein Wert von `0` deaktiviert das Ziel.
- Positionsobergrenze: `Max Orders` begrenzt, wie viele lange Einträge gleichzeitig aktiv sein können. Da StockSharp eine Nettoposition behält, nähert sich die Strategie dem MT4-Verhalten an, indem sie zählt, wie viele `Volume`-Blöcke derzeit geöffnet sind.
- Trailing Stop: Das ursprüngliche EA hat eine Trailing Stop-Eingabe deklariert, diese jedoch nicht implementiert. In der konvertierten Version wird außerdem die abschließende Logik für die Parität weggelassen.

## Parameter
| Name | Beschreibung | Standard |
| --- | --- | --- |
| `Trading Candle` | Primärer Zeitrahmen für Preismuster und schnelle Stochastik. | 1 Minute |
| `Slow Stochastic Candle` | Für die Bestätigungsstochastik wird ein höherer Zeitrahmen verwendet. | 5 Minuten |
| `Stochastic Length` | Lookback-Fenster für %K. | 5 |
| `%K Smoothing` | Auf die %K-Linie angewendete Glättung. | 3 |
| `%D Period` | Auf die %D-Linie angewendete Glättung. | 3 |
| `Slowing` | Zusätzlicher Glättungsfaktor für %K. | 3 |
| `Stop Loss (pts)` | Stop-Loss-Distanz in Preisschritten. | 0 |
| `Take Profit (pts)` | Nehmen Sie die Gewinndistanz in Preisschritten. | 12 |
| `Max Orders` | Maximal gleichzeitige lange Einträge. | 1 |

## Nutzungshinweise
- Legen Sie die Eigenschaft `Volume` fest, bevor Sie mit der Strategie beginnen. StockSharp ist standardmäßig `0`, was die Auftragserteilung blockieren würde.
- Die Preisstufe wird ab `Security.PriceStep` gelesen (fällt auf `Security.Step` oder `1` zurück). Stellen Sie sicher, dass die Metadaten Ihres Instruments korrekt konfiguriert sind, um präzise Stopp-/Zielwerte zu erhalten.
- Wenn der Bestätigungszeitrahmen vom Handelszeitrahmen abweicht, wird die zuletzt abgeschlossene langsame Kerze wiederverwendet, bis eine neue erscheint, die dem Verhalten des ursprünglichen MT4-Skripts entspricht.
- Der EA schaffte keine Ausstiege über den Stop-Loss und die Gewinnmitnahme hinaus auf Brokerseite. Die Konvertierung spiegelt dieses Verhalten wider, indem bei Erreichen der Niveaus schützende Marktaufträge gesendet werden.
- Da StockSharp Positionen aggregiert, funktioniert `Max Orders > 1` am besten, wenn jeder Eintrag dieselbe `Volume`-Größe verwendet.

## Unterschiede zur MT4-Version
- Sicherheitsprüfung auf fehlende Preisschrittinformationen mit einer Protokollwarnung, anstatt stillschweigend `Point` zu verwenden.
- Es wurden Schutzklauseln hinzugefügt, um sicherzustellen, dass die Strategie nur dann gehandelt wird, wenn alle erforderlichen Daten (Preisverlauf und beide stochastischen Oszillatoren) verfügbar sind.
- Die Strategie läuft nur auf fertigen Kerzen, während MT4 Ticks verarbeitet, aber durch die Taktzeit gedrosselt wird. Durch diese Änderung werden doppelte Auswertungen vermieden und die Logik bleibt deterministisch.
