# Big Dog Range-Ausbruch-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **Big Dog**-Strategie sucht in der Londoner Morgensitzung nach einem engen Konsolidierungsfenster und handelt Ausbrüche aus dieser Box. Der ursprüngliche MQL Expert Advisor platzierte Stop-Orders, sobald der Preisbereich zwischen den angegebenen `StartHour` und `StopHour` innerhalb einer konfigurierbaren Punktanzahl blieb. Der StockSharp-Port behält die gleiche Idee bei und verwendet Market Orders beim Ausbruch, begleitet von dynamischen Stop-Loss- und Take-Profit-Levels, die von den Extremen der Konsolidierung abgeleitet werden.

## Handelslogik

1. Abgeschlossene Kerzen zwischen `StartHour` (einschließlich) und `StopHour` (standardmäßig ausschließlich) sammeln, um den täglichen Bereich zu erstellen.
2. Die Sitzung ignorieren, wenn die Differenz zwischen Sitzungshoch und -tief `MaxRangePoints` überschreitet (konvertiert in Preiseinheiten mit der angepassten Punktgröße).
3. Nach dem Schließen der Sitzung den Abstand zwischen dem aktuellen besten Ask/Bid und den Ausbruchslevels prüfen. Ein Setup wird nur aktiviert, wenn der Markt mindestens `DistancePoints` vom Hoch (für Long-Einstiege) oder Tief (für Short-Einstiege) entfernt ist.
4. Wenn der Preis auf einer nachfolgenden Kerze durch das vorbereitete Hoch oder Tief bricht, mit einer Market Order in Größe von `OrderVolume` einsteigen (entgegengesetzte Positionen werden automatisch ausgeglichen).
5. Sofort Ausstiege zuweisen:
   - Long-Trades verwenden einen Stop-Loss beim aufgezeichneten Sitzungstief und einen Take-Profit `TakeProfitPoints` über dem Einstiegslevel.
   - Short-Trades verwenden einen Stop-Loss beim aufgezeichneten Sitzungshoch und einen Take-Profit `TakeProfitPoints` unter dem Einstiegslevel.
6. Bei jeder abgeschlossenen Kerze überwacht die Strategie das Hoch/Tief, um zu entscheiden, ob der Stop-Loss oder Take-Profit erreicht wurde, und schließt die Position entsprechend.
7. Am Beginn eines neuen Handelstages werden alle gecachten Levels zurückgesetzt, um Restorders aus der vorherigen Sitzung zu verhindern.

> **Angepasste Punkte.** Die Strategie konvertiert punktbasierte Eingaben in tatsächliche Preisabstände, indem sie diese mit dem Instrument-`PriceStep` multipliziert. Wenn das Wertpapier 3 oder 5 Dezimalstellen hat, wird der Wert zusätzlich um 10 skaliert, um die im ursprünglichen EA verwendete Pip-Logik nachzuahmen.

## Parameter

| Parameter | Beschreibung | Standard |
|-----------|--------------|---------|
| `StartHour` | Tagesstunde (0-23), zu der das Konsolidierungsfenster beginnt. | `14` |
| `StopHour` | Tagesstunde (0-23), zu der das Konsolidierungsfenster endet. | `16` |
| `MaxRangePoints` | Maximale Höhe der Sitzungsbox in angepassten Punkten. | `50` |
| `TakeProfitPoints` | Take-Profit-Abstand in angepassten Punkten vom Ausbruchspreis. | `50` |
| `DistancePoints` | Mindestabstand zwischen aktuellem Preis und Ausbruchslevel vor der Aktivierung von Orders. | `20` |
| `OrderVolume` | Volumen jedes Ausbruchsgeschäfts (auch auf Strategie-`Volume` angewendet). | `1` |
| `CandleType` | Kerzentyp zum Erstellen der Sitzungsbox. Einstündiger Zeitrahmen standardmäßig. | `1h` |

## Implementierungshinweise

- Die Strategie abonniert sowohl Kerzen als auch das Orderbuch. Die besten Bid/Ask-Werte werden zur Auswertung der Distanzfilter verwendet, mit Fallback auf den letzten Kerzenschluss, wenn keine Tiefe verfügbar ist.
- Einstiege werden mit Market Orders ausgeführt. Dies spiegelt das Verhalten der ursprünglichen ausstehenden Stop-Orders wider, während es innerhalb der High-Level-API bleibt.
- Stop-Loss- und Take-Profit-Entscheidungen werden bei Kerzenschlüssen basierend auf Intrabar-Hochs und -Tiefs getroffen, was die Schutzlevels der MQL-Version emuliert, ohne zusätzliche Child-Orders zu registrieren.
- Das tägliche Zustandsmanagement storniert aktive Orders und setzt gecachte Hochs/Tiefs zurück, wenn sich das Kalenderdatum ändert.
