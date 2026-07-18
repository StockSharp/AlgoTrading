# Locker Hedging Grid-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie repliziert den MetaTrader 4 Expert Advisor **Locker.mq4**. Es beginnt jeden Zyklus mit einem Marktkauf und verwaltet dann ein abgesichertes Raster von Kauf- und Verkaufsaufträgen. Sobald der kombinierte nicht realisierte Gewinn aller offenen Geschäfte einen festgelegten Bruchteil des Kontokapitals erreicht, wird jede Position geschlossen und ein neuer Zyklus beginnt. Wenn der variable Verlust den gleichen Bruchteil in die negative Richtung übersteigt, fügt die Strategie schrittweise Rettungsaufträge in festen Punktintervallen hinzu und blockiert Preisschwankungen durch abwechselnde Long- und Short-Einstiege.

## Parameter

| Parameter | Beschreibung | Standard |
|-----------|-------------|---------|
| `NeedProfitRatio` | Anteil des Portfolio-Eigenkapitals, der vor dem Schließen/Hinzufügen von Aufträgen verdient (oder verloren) werden muss. `0.001` entspricht 0,1 % des Kontos. | `0.001` |
| `InitialVolume` | Volumen der allerersten Marktkauforder zu Beginn jedes Zyklus. | `0.5` |
| `StepVolume` | Volumen für jeden Rettungsauftrag, der hinzugefügt wird, während sich die Strategie in einer Drawdown-Phase befindet. | `0.2` |
| `StepPoints` | Abstand in MetaTrader Punkten zwischen Rettungsbefehlen. Wird intern mithilfe von `Security.PriceStep` (Pip)-Informationen in einen Preis umgewandelt. | `50` |
| `EnableRescue` | Aktiviert das Mittelungsraster, wenn der gleitende Verlust den negativen Schwellenwert überschreitet. Wenn die Strategie deaktiviert ist, führt sie nur den ersten Handel durch und wartet auf den Gewinn. | `true` |

## Handelslogik

1. **Zyklusstart**
   - Beim ersten eingehenden Handelskurs wird ein Marktkauf mit `InitialVolume` gesendet.
   - Der Einstiegspreis wird zum Referenzkontrollpunkt, und sowohl der höchste Kauf- als auch der niedrigste Verkaufs-Tracker werden auf diesen Preis zurückgesetzt.

2. **Gewinnsperre**
   - Bei jedem Tick summiert die Strategie die nicht realisierten Gewinne und Verluste aller Long- und Short-Strecken. Lange Beine tragen `(price - averageBuyPrice) * longVolume` bei, während kurze Beine `(averageSellPrice - price) * shortVolume` hinzufügen.
   - Sobald der variable Gewinn `NeedProfitRatio * equity` erreicht, werden alle Positionen durch entgegengesetzte Marktaufträge abgeflacht. Ein neuer Zyklus beginnt, nachdem die Füllungen bestätigt wurden.

3. **Rettungsgitter**
   - Wenn der nicht realisierte Gewinn unter `-NeedProfitRatio * equity` fällt und `EnableRescue` wahr ist, wartet das System darauf, dass sich der Preis um `StepPoints` bewegt (umgerechnet in Preisdistanz). Jedes neue Hoch über dem letzten Kontrollpunkt löst einen weiteren Marktkauf aus, während jedes neue Tief einen Marktverkauf auslöst. Die Volumina sind immer gleich `StepVolume`.
   - Kontrollpunkt- und Richtungsextreme werden nach jedem Rettungsauftrag aktualisiert, sodass die nächste Hinzufügung einen weiteren vollen Preisschritt erfordert.

4. **Zyklus zurücksetzen**
   - Nachdem sowohl die Long- als auch die Short-Bestände auf Null gesunken sind (bestätigt durch eigene Handelsbenachrichtigungen), werden der Kontrollpunkt und die Extreme auf den neuesten Handelspreis zurückgesetzt und die Strategie ist bereit, mit dem ersten Kauf einen neuen Zyklus einzuleiten.

## Implementierungshinweise

- Verwendet `SubscribeTrades().Bind(ProcessTrade)`, um mit Tick-für-Tick-Preisen zu arbeiten, und spiegelt das ursprüngliche MQL EA wider, das auf das aktuelle Gebot/Brief reagiert hat.
- Wandelt MetaTrader „Punkte“ über eine von `Security.PriceStep` abgeleitete Pip-Größe in StockSharp-Preise um. Symbole, die mit 3 oder 5 Dezimalstellen angegeben werden, erhalten die Standardanpassung *x10*.
- Verfolgt Long- und Short-Bestände separat in `OnOwnTradeReceived` und ermöglicht so ein abgesichertes Engagement genau wie bei der MT4-Version (Kauf- und Verkaufspositionen können nebeneinander bestehen).
- Das Portfolioeigenkapital wird auf `Portfolio.CurrentValue` geschätzt, mit Rückschlägen auf `CurrentBalance` oder `BeginValue`. Der erste positive Messwert wird zwischengespeichert, sodass die Gewinnschwelle auch dann stabil bleibt, wenn der Anbieter den Wert nicht mehr meldet.
- Jedes Marktauftragsvolumen durchläuft einen `AlignVolume`-Helfer, der die Einschränkungen `Security.VolumeStep`, `VolumeMin` und `VolumeMax` berücksichtigt.

## Nutzungstipps

- Stellen Sie sicher, dass die Instrumentenmetadaten einen korrekten `PriceStep` liefern; Andernfalls ist die Punkt-zu-Preis-Umrechnung ungenau und die Gitterabstände stimmen nicht mit dem MetaTrader-Verhalten überein.
- Da die Rettungslogik eine Martingal-Mittelwertbildung widerspiegelt, wählen Sie `StepVolume` sorgfältig aus und überwachen Sie das Risiko. Durch die Erhöhung von `StepPoints` und `StepVolume` wird die Anzahl der offenen Trades verringert, aber die Präsenz erhöht.
- Setzen Sie `EnableRescue` auf `false`, um eine konservative Variante zu reproduzieren, die einfach darauf wartet, dass die erste Position das Gewinnziel erreicht, ohne jemals den Durchschnitt zu senken.
- Backtesting für Forex-Symbole sollte mit Tick-Daten durchgeführt werden, die der ursprünglichen Granularität von EA entsprechen.

## Unterschiede zum MQL Expert

- Das ursprüngliche Skript versuchte, perfekt kompensierende Orderpaare zu schließen, wenn mehr als acht Trades aktiv waren. Dieser Block wurde aufgrund eines Ticketfilterfehlers nie ausgeführt und wurde weggelassen.
- `StepLot` Neuberechnung basierend auf bereits vorhandenen Bestellungen bei der Initialisierung wird nicht repliziert; Die Lautstärke wird vollständig über die in StockSharp bereitgestellten Parameter gesteuert.
- Orderkommentare, Alarm-Popups und manuelle Stopp-Flags von EA sind nicht vorhanden – die StockSharp-Version konzentriert sich ausschließlich auf autonome Handelslogik.
