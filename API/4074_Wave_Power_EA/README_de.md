# Wave Power EA-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **Wave Power EA-Strategie** ist eine C#-Portierung des MQL4-Expertenberaters „Wave Power EA1“. Der Originalroboter baut eine Position ein
Richtung eines stochastischen oder MACD-Signals und fügt dann bei jeder festgelegten Anzahl von Pips zusätzliche Marktaufträge hinzu, während die angepasst wird
gemeinsame Take-Profit-Ebene. Die StockSharp-Version reproduziert dieses Verhalten mithilfe der High-Level-Strategie API, Indikatorbindung
und integrierte Ordnungshelfer. Alle Kommentare bleiben nach Bedarf auf Englisch.

## Wie die Strategie funktioniert

1. **Signalauswahl** – der erste Trade wird nur eröffnet, wenn einer der Indikatorfilter eine Richtung generiert:
   - `Stochastic` – %K überschreitet %D innerhalb überverkaufter/überkaufter Regionen.
   - `MacdSlope` – Die Linie MACD steigt über oder unter ihren vorherigen Wert.
   - `CciLevels` – CCI fällt unter –120 oder steigt über +120.
   - `AwesomeBreakout` – Fantastischer Oszillator, der das adaptive historische Tief/Hoch durchbricht, das während der Initialisierung erfasst wurde.
   - `RsiMa` – schnelles SMA kreuzt langsames SMA, während RSI den Schwung bestätigt (über/unter 50).
   - `SmaTrend` – ein 15/20/25/50 SMA-Fächer, der in die gleiche Richtung zeigt, mit einem minimalen Neigungsunterschied.

2. **Netzerweiterung** – nachdem die erste Marktorder ausgeführt wurde, merkt sich die Strategie den Ausführungspreis. Immer wenn sich der Markt bewegt
um `GridStepPips` gegenüber der aktuellen Position und die maximale Orderanzahl wird nicht überschritten, übermittelt die Strategie einen neuen Markt
Reihenfolge in der *gleichen* Richtung. Jede neue Ebene multipliziert das Volumen mit dem Parameter `Multiplier`.

3. **Gemeinsame Ziele** – bei jeder neuen Order wird ein gemeinsamer Take-Profit- und (optional) Stop-Loss-Preis neu berechnet. Wenn die Anzahl der
Wenn sich aktive Bestellungen dem Schwellenwert von `OrdersToProtect` nähern, wird die Take-Profit-Distanz durch `ReboundProfitPrimary` ersetzt.
Nachdem der Schwellenwert überschritten wurde, wechselt die Distanz auf `ReboundProfitSecondary`, um eine schnellere Wiederherstellung zu fördern.

4. **Korbüberwachung** – bei jedem Kerzenschluss wandelt die Strategie den offenen Gewinn und Verlust in Pips pro Lot um. Wenn der Rebound-Gewinn bzw
Bei Erreichen der Verlustschutzschwellen wird der gesamte Warenkorb mittels Marktaufträgen liquidiert. Das gleiche passiert, wenn der Älteste
Handel ist älter als `OrdersTimeAliveSeconds` oder wenn der Handel am Freitag deaktiviert ist.

5. **Lebenszyklus** – sobald der Korb flach ist, werden alle internen Zähler zurückgesetzt, sodass das nächste Signal eine neue Mittelwertbildung starten kann
Zyklus.

Im Vergleich zum ursprünglichen EA vermeidet dieser Port absichtlich die Eröffnung entgegengesetzter (Absicherungs-)Positionen nach einer bestimmten Anzahl von Rastern
Schichten. Alle weiteren Einträge folgen der ursprünglichen Richtung. Der Rest der Geldverwaltungsregeln, Schutzlogik und
Indikatorfilter bleiben mit der Referenzimplementierung MQL4 kompatibel.

## Parameter

| Parameter | Beschreibung |
|-----------|-------------|
| `EntryLogic` | Indikatormodus, der für die allererste Bestellung verwendet wird. |
| `CandleType` | Zeitrahmen, der alle Indikatoren speist (Standard: 1 Stunde). |
| `InitialVolume` | Volumen der ersten Bestellung in Losen/Kontrakten. |
| `GridStepPips` | Minimaler Abstand in Pips zwischen Gitterschichten. |
| `MaxOrders` | Maximale Anzahl gleichzeitiger Bestellungen im Warenkorb. |
| `TakeProfitPips` | Gemeinsame Take-Profit-Distanz in Pips (0 deaktiviert das Ziel). |
| `StopLossPips` | Gemeinsamer Stop-Loss-Abstand in Pips (0 deaktiviert den Stop). |
| `Multiplier` | Auf jede weitere Bestellung wird ein Volumenmultiplikator angewendet. |
| `SecureProfitProtection` | Aktiviert die Rebound-Gewinnlogik. |
| `OrdersToProtect` | Anzahl der erforderlichen Bestellungen, bevor der Rebound-Schutz beginnt. |
| `ReboundProfitPrimary` | Gewinn pro Los (in Pips) für die erste Schutzstufe. |
| `ReboundProfitSecondary` | Gewinn pro Lot (in Pips), sobald die geschützte Orderanzahl überschritten wird. |
| `LossProtection` | Aktiviert den Floating-Loss-Schutz. |
| `LossThreshold` | Verlust pro Los (in Pips), der die Wache auslöst, wenn der Korb voll ist. |
| `ReverseCondition` | Kehrt Kauf-/Verkaufssignale um. |
| `TradeOnFriday` | Ermöglicht die Eröffnung neuer Bestellungen freitags. |
| `OrdersTimeAliveSeconds` | Maximale Lebensdauer der neuesten Bestellung in Sekunden (0 deaktiviert den Timer). |
| `TrendSlopeThreshold` | Minimale Steigungsdifferenz von SMA, die von der `SmaTrend`-Logik verwendet wird. |

## Anwendungstipps

1. Hängen Sie die Strategie an ein Wertpapier mit einem konfigurierten Preisschritt an, damit die Pip-Umrechnung korrekt funktioniert.
2. Passen Sie `GridStepPips`, `Multiplier` und `MaxOrders` entsprechend der Instrumentenvolatilität und der Broker-Margin-Richtlinie an.
3. Aktivieren Sie die Schutzblöcke, wenn Sie auf einem Live-Konto laufen, um unkontrollierte Verluste bei längeren Trends zu verhindern.
4. Die Strategie basiert auf geschlossenen Kerzen; Wählen Sie einen Zeitrahmen, der den gewünschten Handelsrhythmus widerspiegelt (das ursprüngliche EA verwendet M30
und H1-Kombinationen, aber die Standard-H1-Kerzen funktionieren gut).
5. Da eine Absicherung nach der fünften Ebene nicht implementiert ist, sollten Sie eine Senkung von `MaxOrders` in Betracht ziehen, wenn Sie das exakte Original benötigen
Verhalten.

## Dateien

- `CS/WavePowerEAStrategy.cs` – StockSharp-Implementierung der Wave Power EA-Gitterlogik.
- `README.md` / `README_ru.md` / `README_zh.md` – Dokumentation in Englisch, Russisch und Chinesisch.

Die Python-Version wird gemäß den Aufgabenanforderungen absichtlich weggelassen.
