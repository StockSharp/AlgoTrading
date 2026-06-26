# Two Per Bar Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Der ursprüngliche MetaTrader-Experte "Two PerBar" eröffnet zu Beginn jedes neuen Balkens eine Long- und eine Short-Position, schließt den gesamten Korb auf dem nächsten Balken und wendet optional einen martingalähnlichen Volumenmultiplikator an. Der StockSharp-Port hält denselben Rhythmus, indem er beide gehedgten Beine explizit verfolgt und einmal pro abgeschlossener Kerze reagiert. Alle Orders werden über die High-Level-API erstellt und respektieren die Instrument-Metadaten (Preisschritt, Volumenschritt und Min/Max-Lot-Beschränkungen).

## Handelszyklus
1. **Neue Kerze erkennen** – die Strategie abonniert die konfigurierte Kerzenserie über `SubscribeCandles`. Wenn die Kerze mit `State == CandleStates.Finished` eintrifft, hat ein neuer Balken begonnen und der Zyklus läuft.
2. **Take-Profit-Treffer auswerten** – jedes gespeicherte Bein trägt seinen eigenen Einstiegspreis und Take-Profit-Level. Wenn das Hoch oder Tief der abgeschlossenen Kerze diesen Level berührt, wird das Bein sofort mit einer Marktorder geschlossen und aus der Verfolgungsliste entfernt.
3. **Zwangsliquidation von Überbleibseln** – alle Beine, die den Take-Profit-Scan überlebt haben, werden vor dem Öffnen des nächsten Paares zum Markt liquidiert. Dies spiegelt den MetaTrader-Code wider, der `PositionClose` bei jedem Balkenöffnen aufruft.
4. **Nächste Losgröße bestimmen** –
   - Wenn ein vorheriger Zyklus noch offene Beine hatte, wird das größte Volumen unter ihnen mit `VolumeMultiplier` multipliziert.
   - Wenn der Korb flach endete (zum Beispiel, beide Beine haben ihren Take-Profit getroffen), wird der Zyklus auf `InitialVolume` zurückgesetzt.
   - `PrepareVolume` normalisiert das Kandidaten-Lot, indem es auf zwei Dezimalstellen rundet, es an den Instrument-`VolumeStep` anpasst, gegen das Börsen-`MinVolume` prüft, und schließlich auf `InitialVolume` zurücksetzt, wenn es entweder das benutzerdefinierte `MaxVolume` oder das `Security.MaxVolume` übersteigt.
5. **Standardwerte aktualisieren** – das berechnete Lot wird in `_lastCycleVolume` gespeichert und in `Strategy.Volume` geschrieben, damit Hilfsmethoden denselben Betrag wiederverwenden.
6. **Ein neues gehedgtes Paar öffnen** – `BuyMarket(volume)` eröffnet das Long-Bein und `SellMarket(volume)` das Short-Bein. Jedes Bein merkt sich den Schlusskurs der abgeschlossenen Kerze und den absoluten Take-Profit-Level (`entry ± TakeProfitPoints * pointSize`). Ein null oder negativer `TakeProfitPoints` deaktiviert den Take-Profit und nur der Zwangsliquidationsschritt schließt den Korb.

Das Ergebnis ist ein perpetueller Straddle: jede Kerze beginnt mit einem Long+Short-Paar, beide werden während des Balkens auf Gewinnziele überprüft, und alles ist flach vor dem nächsten Zyklus.

## Geldmanagement und Schutz
- **Martingalähnliche Skalierung** – `VolumeMultiplier` repliziert den MetaTrader-Multiplikator. Wenn ein Bein bis zum Zwangsliquidationsschritt überlebt, verwendet der nächste Zyklus die Größe des schwersten Beins multipliziert mit diesem Wert. Ein abgeschlossener profitabler Zyklus (beide Beine über Take-Profit geschlossen) setzt das Lot auf `InitialVolume` zurück.
- **Volumendeckelung** – `MaxVolume` ist eine harte Obergrenze, die das Lot auf `InitialVolume` zurückzwingt, sobald der Multiplikator es überschreiten würde. Dasselbe Reset passiert, wenn das Instrument ein engeres `Security.MaxVolume` meldet.
- **Börsenkonformität** – alle Volumina werden an das `Security.VolumeStep` angepasst und abgelehnt, wenn sie unter `MinVolume` fallen. Das Setzen von `InitialVolume` auf eine handelbare Größe garantiert, dass der Reset-Pfad immer gültig bleibt.
- **Punktberechnung** – der Take-Profit-Offset verwendet `Security.PriceStep` (oder `MinPriceStep` als Fallback). Instrumente ohne definierten Schritt deaktivieren den Take-Profit effektiv, da der berechnete Offset null ist.

## Parameter
| Name | Typ | Standard | Beschreibung |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | 1-Minuten-Zeitrahmen | Primärer Zeitrahmen, der den einmal-pro-Balken-Workflow auslöst. |
| `InitialVolume` | `decimal` | `1` | Losgröße beim Start eines neuen Zyklus ohne überlebende Beine. |
| `VolumeMultiplier` | `decimal` | `2` | Multiplikator auf das größte überlebende Bein aus dem vorherigen Zyklus. |
| `MaxVolume` | `decimal` | `10` | Maximale erlaubte Losgröße vor dem Zurücksetzen auf `InitialVolume`. |
| `TakeProfitPoints` | `int` | `50` | Abstand in Preispunkten für das Take-Profit-Ziel pro Bein. `0` deaktiviert den Take-Profit und verlässt sich ausschließlich auf die Balkenschluss-Liquidation. |

## Implementierungshinweise und Unterschiede
- Gehedgte Beine werden manuell in `_legs` verfolgt, damit die Strategie über individuelle Long/Short-Exposures nachdenken kann, obwohl StockSharp nur die Nettoposition meldet.
- Anstatt sich auf einzelne Ticks zu verlassen, prüft die Take-Profit-Logik den Hoch/Tief-Bereich der abgeschlossenen Kerze. Dies hält die Implementierung deterministisch, während sie dem ursprünglichen "pro Balken"-Verhalten treu bleibt.
- Die MetaTrader-Slippage- und Magic-Number-Einstellungen sind nicht exponiert; StockSharp verarbeitet die Order-Routing-Details, und die Strategie läuft im Portfolio der übergeordneten Strategieinstanz.
- Die Orderplatzierung verwendet die `Strategy`-Hilfsmethoden (`BuyMarket`, `SellMarket`) ohne Indikatoren direkt zu `Strategy.Indicators` hinzuzufügen, gemäß den Repository-Richtlinien.

## Verwendungstipps
- Passen Sie `InitialVolume` an den Lot-Schritt des Instruments an, bevor Sie die Strategie starten. Der Konstruktor versucht nicht, Ihre Eingabe automatisch zu runden.
- Wenn das Instrument einen sehr kleinen Preisschritt hat, erwägen Sie, `TakeProfitPoints` zu reduzieren; andernfalls kann der berechnete Take-Profit unrealistisch weit entfernt liegen.
- Da die Strategie gleichzeitig Aufträge in entgegengesetzte Richtungen öffnet, führen Sie sie auf Konnektoren/Börsen aus, die gehedgte Positionen erlauben. In Umgebungen, die Positionen sofort netten, spiegelt die `_legs`-Liste noch die beabsichtigte Logik wider, aber das tatsächliche Broker-Verhalten kann abweichen.
- Fügen Sie die Strategie einem Chart hinzu, um Kerzen und ausgeführte Trades zu visualisieren (`DrawCandles` + `DrawOwnTrades` sind in `OnStarted` aktiviert).
