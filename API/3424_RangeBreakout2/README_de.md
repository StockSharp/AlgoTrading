# RangeBreakout2-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die **RangeBreakout2-Strategie** ist eine StockSharp-Portierung des MetaTrader-Expertenberaters „RangeBreakout2“. Der Algorithmus erstellt zu konfigurierbaren Zeiten (wöchentlich, täglich oder kontinuierlich) eine Preisspanne und öffnet eine einzelne Marktorder, sobald die Geld-/Briefkurse diese Spanne verlassen. Nach jedem Trade beginnt der Bereichsvorbereitungszyklus erneut. Die Implementierung reproduziert die ursprünglichen Money-Management-Regeln (konstante, lineare, Martingal- und Fibonacci-Skalierung) und die optionale Erweiterung der Take-Profit-Distanz nach einem Verlusthandel.

Die Strategie funktioniert mit einem einzigen Wertpapier und basiert auf den besten Geld-/Briefkursen. Stellen Sie sicher, dass der Adapter aktuelle Orderbuchdaten bereitstellt, damit die Breakout-Erkennung weiterhin reagiert.

## Handelslogik

1. **Planung** – Zum konfigurierten Zeitpunkt zeichnet die Strategie den aktuellen Briefkurs als Mittelpunkt des Setups auf und leitet die oberen/unteren Ausbruchsniveaus aus der Rohspanne ab.
2. **Reichweitenberechnung** – Die Rohreichweite wird in einem von drei Modi ermittelt:
   - **ATR** – Multipliziert den neuesten Average True Range-Wert mit `AtrPercentage`.
   - **Prozent** – Verwendet `PricePercentage` Prozent des aktuellen Briefkurses.
   - **Behoben** – Konvertiert `FixedRangePoints` Preisschritte in eine absolute Distanz.
3. **Breakout-Erkennung** – Während der `Setup`-Phase überwacht die Strategie den besten Bid/Ask. Wenn der Briefkurs über das obere Niveau steigt oder der Geldkurs unter das untere Niveau fällt, wird eine Marktorder übermittelt.
4. **Eintragstyp** – `TradeMode` wählt zwischen Ausbruch (`Stop`), Fade (`Limit`) oder zufälligem Verhalten. Der Zufallsmodus wählt bei jedem Eintrag entweder Ausbruch oder Fade.
5. **Schutz** – Stop-Loss- und Take-Profit-Offsets werden aus der Rohspanne abgeleitet. Wenn der vorherige Handel mit einem Verlust endete und `RangeMultiplier` größer als 1 ist, wird die Take-Profit-Distanz um diesen Multiplikator erweitert.
6. **Geldmanagement** – Das Ordervolumen wird aus dem freien Portfoliokapital (`CurrentValue - BlockedValue`) und dem ausgewählten Lot-Modus berechnet:
   - **Konstant** – Verwendet immer das Basisvolumen.
   - **Linear** – Steigt nach jedem Verlust linear an.
   - **Martingale** – Multipliziert das vorherige Volumen mit `LotMultiplier` nach einem Verlust.
   - **Fibonacci** – Wächst nach Verlusten entsprechend der Fibonacci-Sequenz.

Sobald die Position geschlossen ist, wird die Strategie in die Standby-Phase zurückgesetzt und wartet auf den nächsten Zeitplanauslöser.

## Parameter

| Gruppe | Name | Beschreibung | Standard |
|-------|------|-------------|---------|
| Zeitplan | `Periodicity` | Häufigkeit der Bereichsvorbereitung: Wöchentlich, täglich oder NonStop. | `Weekly` |
| Zeitplan | `Day` | Handelstag, der verwendet wird, wenn `Periodicity` = Wöchentlich. | `Monday` |
| Zeitplan | `Hour` | Tagesstunde, zu der das Setup erstellt wird (Anpassung im MetaTrader-Stil: gespeicherter Wert + 1, begrenzt auf 0, wenn ≥ 23). | `0` |
| Reichweite | `RangeMode` | Berechnungsmethode für den Rohbereich (ATR / Prozent / Fest). | `Atr` |
| Reichweite | `AtrPercentage` | Prozentualer Multiplikator, der auf den Wert ATR angewendet wird. | `50` |
| Reichweite | `AtrLength` | Anzahl der Kerzen, die im Indikator ATR verwendet werden. | `20` |
| Reichweite | `PricePercentage` | Prozentsatz des aktuellen Briefkurses, der verwendet wird, wenn `RangeMode = Percent`. | `1` |
| Reichweite | `FixedRangePoints` | Fester Bereich, ausgedrückt in Preisschritten bei `RangeMode = Fixed`. | `1000` |
| Handel | `RangePercentage` | Prozentsatz des Rohbereichs, der auf Ausbruchsniveaus angewendet wird. | `100` |
| Handel | `TradeMode` | Eingabestil: Stop (Breakout), Limit (Fade) oder Random. | `Stop` |
| Handel | `TakeProfitPercentage` | Take-Profit-Distanz in Prozent der (optional erweiterten) Range. | `100` |
| Handel | `StopLossPercentage` | Stop-Loss-Distanz als Prozentsatz der Basisspanne. | `100` |
| Risiko | `LotMode` | Grundstücksverwaltungsschema (Konstant / Linear / Martingale / Fibonacci). | `Martingale` |
| Risiko | `MarginPercentage` | Anteil des freien Kapitals, der für das Basisauftragsvolumen reserviert ist. | `10` |
| Risiko | `LotMultiplier` | Multiplikator, der in Martingal-ähnlichen Skalierungsmodi angewendet wird. | `2` |
| Risiko | `RangeMultiplier` | Nach einem Verlustgeschäft wird ein Take-Profit-Multiplikator angewendet. | `1` |
| Daten | `SignalCandleType` | Kerzentyp, der zur Überprüfung der Planungsbedingungen verwendet wird. | `1m time-frame` |
| Daten | `AtrCandleType` | Kerzentyp, der für die ATR-Berechnung verwendet wird. Nur angefordert, wenn `RangeMode = Atr`. | `1d time-frame` |

## Implementierungshinweise

- Die Strategie erfordert Live-Aktualisierungen von Geboten und Briefen; Ohne sie wird die Ausbruchserkennung nicht ausgelöst.
- Die Berechnung des Basisvolumens basiert auf dem Portfolioeigenkapital (`CurrentValue - BlockedValue`). Wenn der Konnektor diese Felder nicht bereitstellt, fällt das Volumen auf das Austauschminimum zurück.
- Schutzanordnungen werden über `SetStopLoss` und `SetTakeProfit` erteilt. Die resultierende Position (nach dem neuen Handel) wird übergeben, damit die Basisklasse den kombinierten Schutz für Skalierungsszenarien verwalten kann.
- Der ATR-Fallback ahmt den ursprünglichen Expert Advisor nach: Wenn der Indikator nicht bereit ist, beträgt der Bereich standardmäßig 1 % des aktuellen Briefkurses.
- Der Zufallshandelsmodus verwendet die .NET-Klasse `Random`, die auf der Strategiekonstruktion basiert. Zwei aufeinanderfolgende Ausbrüche können daher zu unterschiedlichen Einstiegstypen führen.

## Nutzungstipps

1. Konfigurieren Sie `SignalCandleType` so, dass es der gewünschten Auflösung von Zeitplanprüfungen entspricht. Ein einminütiger Kerzenstrom reproduziert genau das tickgesteuerte Verhalten der MQL-Version.
2. Stellen Sie bei wöchentlichen Zeitplänen sicher, dass die Serverzeitzone mit der Erwartung aus dem Original EA übereinstimmt.
3. Beobachten Sie den Effekt von `RangeMultiplier` bei der Verwendung von Martingal-ähnlichen Lot-Modi: Eine Vergrößerung der Take-Profit-Distanz zusammen mit steigenden Volumina erhöht die Präsenz nach Verluststrähnen.
4. Da Stop-Loss- und Take-Profit-Abstände aus der Rohspanne abgeleitet werden, führen große `RangePercentage`-Werte zu ebenso großen Schutzversätzen.
