# Swaper-Strategie (API 3751)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

The **Swaper Strategy** replicates the MetaTrader expert advisor "Swaper 1.1" using StockSharp's high-level strategy API. Die
original system accumulates swap gains by constantly rebalancing a synthetic portfolio between long and short exposure. Dies
Bei der Konvertierung bleibt die Geldflusslogik erhalten, indem das virtuelle Guthaben des Experten rekonstruiert und ein fairer Wert für das berechnet wird
Basiswert zu ermitteln und die offene Position an diesem Zielwert auszurichten.

## Kernlogik

1. **Rekonstruktion des synthetischen Kapitals.** Die Strategie stellt den Akkumulator MetaTrader `money` durch Kombination des Anfangs neu her
Saldo (`BaseUnits * BeginPrice`), realisierter Gewinn aus ausgeführten Aufträgen und der nicht realisierte Teil der aktuellen Position
skaliert um `ContractMultiplier`.
2. **Nenner des beizulegenden Zeitwerts.** Der MQL-Experte verwaltet eine `com`-Variable, die mit dem aktiven Volumen wächst oder schrumpft. Der StockSharp
Port spiegelt dieses Verhalten durch `BaseUnits + ContractMultiplier * Position` wider.
3. **Target volume calculation.** The algorithm evaluates the maximum of the last two candle highs (adjusted by the market spread)
und das Minimum der letzten beiden Tiefs, um die MetaTrader-Leitplanke zu reproduzieren. Ein `Experts / (Experts + 1)`-Faktor steuert, wie
aggressively the strategy moves towards the fair value.
4. **Positionsanpassungen.** Abhängig vom berechneten `dt`-Wert der Strategie entweder
   - closes positions when the calculated adjustment is below one tenth of a lot, or
   - verkauft zusätzliches Volumen, wenn `dt < 0`, oder
   - kauft zusätzliches Volumen, wenn `dt >= 0`.
5. **Margenbewusste Losgröße.** Die Hilfsmethode `GetTradableVolume` nähert sich `AccountFreeMargin()` Prüfungen an, indem sie die prüft
konfiguriert `MarginPerLot` mit dem verfügbaren Portfoliokapital. Wenn die angeforderte Größe die verfügbare Marge überschreitet, wird das Los
Der Betrag wird auf das nächste Zehntel gerundet.

The entire loop is executed on finished candles, replacing the original tick-based function while keeping the economic logic
intakt.

## Parameter

| Parameter | Standard | Beschreibung |
|-----------|---------|-------------|
| `Experts` | `1` | Auf die synthetische Anpassung des beizulegenden Zeitwerts angewendete Gewichtung. |
| `BeginPrice` | `1.8014` | Starting price used to rebuild the virtual balance. |
| `MagicNumber` | `777` | Behaltene Kennung zur Kompatibilität mit der MetaTrader-Version (bei Bedarf angemeldete Bestellungen). |
| `BaseUnits` | `1000` | Vom Nenner der Fair-Value-Gleichung verwendete Anfangskapitaleinheiten. |
| `ContractMultiplier` | `10` | Multiplikator, der Preisunterschiede in die Kontowährung umrechnet. |
| `MarginPerLot` | `1000` | Ungefährer Kapitalbedarf zur Unterstützung eines Grundstücks; regelt die Losreduzierungslogik. |
| `FallbackSpreadSteps` | `1` | Spread in Preisschritten, wenn Notierungen der ersten Stufe fehlen. |
| `CandleType` | `1 Hour` | Primärer Zeitrahmen, der die Rebalancing-Schleife speist. |

## Handelsablauf

1. Abonnieren Sie die konfigurierten Kerzenserien und Level-One-Daten.
2. Track best bid/ask quotes to obtain an accurate spread. Wenn der Feed stumm ist, greifen Sie auf zurück
`FallbackSpreadSteps * PriceStep`.
3. Recalculate the synthetic capital and denominator on every finished candle.
4. Berechnen Sie `dt` mithilfe des Hochpreispfads. Wenn `dt < 0`, wechseln Sie in den Niedrigpreiszweig, um den ursprünglichen Schutz zu emulieren
Logik.
5. Use `AdjustShort` or `AdjustLong` to shrink or expand the position. When the target size is smaller than one tenth of a lot,
Schließen Sie die Position vollständig, um das Verhalten von `closeby` von MetaTrader zu kopieren.
6. Aktualisieren Sie den realisierten PnL innerhalb von `OnOwnTradeReceived`, sodass nachfolgende Iterationen den neuesten Saldo verwenden.

## Unterschiede zur MQL4-Version

- Die Tick-gesteuerte `start()`-Schleife wird durch Kerzenverarbeitung ersetzt, wodurch geschäftiges Warten vermieden wird und gleichzeitig die Strategie erhalten bleibt
Absicht.
- Die Orderhistorie und das Scannen offener Trades werden durch den eigenen Trade-Stream der Strategie angenähert, statt durch `OrdersHistoryTotal()`.
und `OrdersTotal()`.
- Margin-Prüfungen verwenden `Portfolio.CurrentValue` mit einer konfigurierbaren `MarginPerLot`-Konstante, da es sich um eine Broker-spezifische Marge handelt
Funktionen sind in StockSharp nicht verfügbar.
- Pair-closing via `OrderCloseBy` is emulated by simply flattening the net position, consistent with the netting model of most
StockSharp Anschlüsse.

## Nutzungshinweise

- Konfigurieren Sie `MarginPerLot` gemäß den Vertragsspezifikationen des Connectors, um zu verhindern, dass die Strategie eine anfordert
undurchführbares Volumen.
- Die Strategie geht davon aus, dass Kerzendaten zuverlässige Hochs und Tiefs liefern; Verwenden Sie einen Zeitrahmen, der dem vom Broker verwendeten Feed entspricht
MetaTrader version if you want identical behaviour.
- Da Kurse der ersten Stufe asynchron eintreffen können, speichert die Strategie den neuesten Spread. Stellen Sie sicher, dass sowohl Kerzen als auch Wasserwaage vorhanden sind
one subscriptions are enabled for precise replication.
