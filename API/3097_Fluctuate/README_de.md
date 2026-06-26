# Fluktuations-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **Fluktuations-Strategie** ist ein StockSharp-Port des MetaTrader Expert Advisors "Fluctuate". Sie reproduziert das gitterartige Verhalten des Originals mithilfe der High-Level-API: Eine Kerzen-Subscription steuert alle Entscheidungen, Markteinstiege werden mit `BuyMarket` / `SellMarket` durchgeführt und Wiederherstellungsaufträge werden mit Stop-Orders platziert. Long- und Short-Exposure werden separat verfolgt, um die Hedging-Positionsbuchhaltung aus MetaTrader nachzuahmen, während die tatsächliche StockSharp-Position netto bleibt.

## Grundidee

1. Jedes Mal wenn eine neue Kerze schließt, vergleicht die Strategie die letzten beiden Schlusskurse. Ein höherer Schluss eröffnet einen Marktkauf, ein niedrigerer Schluss eröffnet einen Marktverkauf. Wenn beide Schlusskurse gleich sind, wird der Balken ignoriert.
2. Jede ausgeführte Position erhält einen festen Stop-Loss und Take-Profit (in Pips ausgedrückt). Die Strategie zeichnet auch den genauen Ausführungspreis und das netto durch den Trade hinzugefügte Volumen auf.
3. Nach einem Einstieg wird eine **entgegengesetzte** Stop-Order `StepPips` weit vom letzten Preis entfernt aktiviert (plus ein kleiner Spread-Puffer). Ihr Volumen leitet sich vom vorherigen Trade und dem `LotCoefficient` ab, optional unter Verwendung des kumulierten Exposures, wenn `MultiplyLotCoefficient = true`.
4. Wenn die Stop-Order ausgelöst wird, storniert sie die vorherige ausstehende Order, aktualisiert die internen Exposure-Statistiken und plant sofort eine neue Wiederherstellungs-Stop-Order in die andere Richtung. Dies reproduziert die Averaging/Martingale-Schleife der MQL-Implementierung.
5. Der Trailing-Schutz hebt (oder senkt) den Stop, sobald sich der Preis mindestens `TrailingStopPips + TrailingStepPips` zugunsten der Position bewegt. Dies emuliert den ursprünglichen EA, der einen zusätzlichen Gewinnpuffer benötigte, bevor der Stop angezogen wurde.

## Handelsablauf

- **Signalerkennung.** Der Kerzenfeed wird via `SubscribeCandles` abonniert. Es werden nur abgeschlossene Kerzen verarbeitet. Die Strategie weigert sich außerhalb des Zeitfensters `[StartHour, EndHour)` zu handeln oder wenn der Equity-Wächter ausgelöst wird.
- **Anfängliche Positionsgröße.** Je nach `PositionSizingMode` verwendet der erste Trade einer Sequenz entweder ein festes Los (`FixedVolume`) oder ein risikobasiertes Los (`RiskPercent`). Im Risikomodus wird das erlaubte Risiko (Prozentsatz des aktuellen Eigenkapitals) durch den monetären Verlust dividiert, der eintreten würde, wenn der Stop-Loss ausgelöst wird. Preisschritt und Schrittpreis werden verwendet, um Pips in Währung umzurechnen.
- **Exposure-Buchhaltung.** Separate Akkumulatoren verfolgen Long- und Short-Volumen, Durchschnittspreis und den seit dem Einstieg erreichten Extrempreis. Dies ermöglicht es der Strategie, beide Seiten intern "offen" zu halten, obwohl StockSharp Netting verwendet.
- **Wiederherstellungsaufträge.** Nach jeder Ausführung berechnet der Algorithmus das Volumen der nächsten Stop-Order:
  - Wenn `MultiplyLotCoefficient = false`, entspricht das neue Volumen `LastVolume × LotCoefficient`.
  - Wenn `true`, wird das gesamte absolute Exposure mit `LotCoefficient` multipliziert.
  - Das Volumen wird an Börsenbeschränkungen normalisiert (Schritt, Min- und Max-Volumen) und abgelehnt, wenn es `MaxTotalVolume` überschreiten würde oder die Anzahl aktiver Positionen plus Orders `MaxPositions` überschreiten würde.
- **Gewinnziel und Equity-Wächter.** Aggregierter unrealisierter PnL wird berechnet, indem Preisdifferenzen mithilfe von `PriceStep`/`StepPrice` in Währung übersetzt werden. Wenn er `ProfitTarget` erreicht, werden alle Positionen geschlossen und ausstehende Orders storniert. Der Handel wird auch ausgesetzt, wenn das Eigenkapital unter `MinEquityPercent` des Anfangsguthabens fällt.
- **Trailing-Logik.** Für Long-Positionen wird der höchste seit dem Einstieg gesehene Preis aufgezeichnet. Sobald er den Eintrittspreis um `TrailingStopPips + TrailingStepPips` übersteigt, wird ein Trailing Stop `TrailingStopPips` hinter dem Hoch gesetzt. Short-Positionen wenden die symmetrische Regel mit dem niedrigsten Preis an. Trailing-Updates überschreiben den festen Stop-Loss.

## Risikomanagement-Details

- **Stop / Take-Profit.** Beide sind optional (den Pip-Wert auf null setzen zum Deaktivieren). Sie werden für das aggregierte Long- oder Short-Exposure neu berechnet, wann immer ein neuer Trade Volumen hinzufügt.
- **Max. Positionen.** Zählt die Anzahl offener Seiten (Long + Short) plus die aktive Wiederherstellungs-Stop-Order. Wenn das Limit erreicht ist, verweigert die Strategie die Einreichung neuer Stop-Orders.
- **Maximales Gesamtvolumen.** Begrenzt die Summe des absoluten offenen Volumens und des Volumens der aktiven Wiederherstellungsorder.
- **CloseAllAtStart.** Optionaler Sicherheitsschalter zum Schließen aller Positionen bevor die Strategie mit dem Handel beginnt.

## Parameter

| Name | Beschreibung | Standard |
| --- | --- | --- |
| `CandleType` | Primärer Zeitrahmen für die Signalerkennung. | 1-Minuten-Zeitrahmen |
| `StopLossPips` | Distanz zwischen Eintrittspreis und Stop-Loss (Pips). `0` deaktiviert den Stop. | 50 |
| `TakeProfitPips` | Distanz zwischen Eintrittspreis und Take-Profit (Pips). `0` deaktiviert den Take-Profit. | 50 |
| `TrailingStopPips` | Trailing-Stop-Distanz (Pips). Erfordert `TrailingStepPips > 0`. | 5 |
| `TrailingStepPips` | Zusätzlicher Gewinn vor Vorrücken des Trailing Stops (Pips). | 5 |
| `StepPips` | Distanz zwischen letztem Preis und der entgegengesetzten Wiederherstellungs-Stop (Pips). | 30 |
| `LotCoefficient` | Multiplikator, angewendet auf das vorherige Volumen (oder Gesamtexposure). | 2.0 |
| `MultiplyLotCoefficient` | Wenn `true`, wird das neue Ordervolumen aus dem Gesamtexposure statt dem letzten Trade berechnet. | `false` |
| `MaxPositions` | Maximale Anzahl gleichzeitiger offener Seiten plus aktiver ausstehender Order. | 9 |
| `MaxTotalVolume` | Obergrenze für die Summe des offenen Volumens und des Wiederherstellungsorder-Volumens. | 50 |
| `ProfitTarget` | Unrealisierter Gewinn (in Kontowährung), der einen vollständigen Ausstieg auslöst. `0` deaktiviert das Ziel. | 50 |
| `MinEquityPercent` | Mindest-Eigenkapitalprozentsatz (vs. Anfangsguthaben) zum Weiterhandeln. Unterhalb dieser Schwelle sind nur Ausstiege erlaubt. | 30 |
| `CloseAllAtStart` | Alle Positionen schließen und Orders stornieren, wenn die Strategie startet. | `false` |
| `StartHour` | Handelsfenster-Startstunde (inklusiv, Börsenzeit). | 10 |
| `EndHour` | Handelsfenster-Endstunde (exklusiv, Börsenzeit). | 20 |
| `PositionSizingMode` | `FixedVolume` für statische Lots, `RiskPercent` für prozentuale Eigenkapitalgröße. | `FixedVolume` |
| `VolumeOrRisk` | Feste Losgröße (bei `FixedVolume`) oder Risikoprozentsatz (bei `RiskPercent`). | 1.0 |

## Implementierungshinweise

- Stop-Order-Preise verwenden eine minimale Spread-Annäherung (`PriceStep` wenn verfügbar), weil MetaTrader verlangte, dass die Order außerhalb des Freeze-Levels liegt. `StepPips` anpassen, wenn der tatsächliche Spread breiter ist.
- Die Strategie storniert alle verbleibenden Wiederherstellungsaufträge, wenn ein neuer Trade ausgeführt wird. Dies entspricht dem ursprünglichen EA, der alle ausstehenden Orders nach einer Ausführung löschte.
- Da StockSharp-Portfolios genet sind, wird das Hedging-Exposure intern simuliert. Die tatsächliche Broker-Position wird immer die Nettomenge widerspiegeln.
- Risikobasierte Positionsgrößen erfordern gültige `PriceStep`- und `StepPrice`-Werte aus der Instrumentenbeschreibung.

## Verwendungstipps

1. Einen geeigneten Kerzentyp wählen, der dem ursprünglichen EA-Testzeitrahmen entspricht (typischerweise M5 oder M15) für beste Wiedergabetreue.
2. Börsenlimitvolumen doppelt prüfen: wenn das normalisierte Wiederherstellungsvolumen null wird, wird die Strategie aufhören, neue Legs hinzuzufügen.
3. Wenn `PositionSizingMode = RiskPercent`, sicherstellen, dass das Portfolio aktuelle Eigenkapitalinformationen enthält; andernfalls fällt die Strategie auf die feste Losgröße zurück.
4. Mit dem eingebauten `StrategyProtection` von StockSharp (aktiviert via `StartProtection()`) kombinieren, um bei Bedarf zusätzliche Schutzmaßnahmen auf Kontoebene hinzuzufügen.
