# DeMarker gewinnt an Position Band 2 Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie reproduziert den MetaTrader 5 Expertenberater **„DeMarker gewinnt Positionsvolumen 2“** unter Verwendung des High-Levels API von StockSharp. Es analysiert eine konfigurierbare Kerzenreihe mit dem DeMarker-Oszillator und reagiert, wenn der Wert extreme Zonen erreicht. Die Implementierung behält den ursprünglichen Stil des Geldmanagements mit fester Lotgröße, optionaler Signalumkehr, integriertem Stop-Loss/Take-Profit-Handling und einem optionalen Handelssitzungsfilter bei.

## Originelles Expertenverhalten

* **Plattform**: MetaTrader 5.
* **Indikator**: klassischer DeMarker-Oszillator (`DEM`), Standardperiode 14.
* **Einträge**: Long-Positionen eröffnen, wenn DeMarker unter einen unteren Schwellenwert fällt, Short-Positionen öffnen, wenn er über einen oberen Schwellenwert steigt.
* **Risikokontrollen**: fester Stop-Loss/Take-Profit, ausgedrückt in Punkten, optionaler Trailing-Stop mit Schritt, optionales Zeitfenster.
* **Positionsmanagement**: Stellen Sie sicher, dass nur ein Trade pro Balken erfolgt und schließen Sie die Gegenseite, bevor Sie die Richtung ändern.

Die StockSharp-Konvertierung folgt den gleichen Prinzipien. Schutzaufträge werden mit `StartProtection` implementiert, sodass Stop-Loss, Take-Profit und Trailing automatisch verwaltet werden, sobald eine Position eröffnet wird.

## Handelslogik

1. Abonnieren Sie den konfigurierten Kerzentyp (`CandleType`, standardmäßig 5-Minuten-Kerzen) und berechnen Sie den DeMarker-Wert mit dem ausgewählten Zeitraum (`DeMarkerPeriod`).
2. Wenn eine Kerze schließt, bewerten Sie den Oszillator:
   * Wenn `ReverseSignals` **false** ist (Standard):
     * **Lange Einrichtung** – `DeMarker <= LowerLevel`.
     * **Kurze Einrichtung** – `DeMarker >= UpperLevel`.
   * Wenn `ReverseSignals` **wahr** ist, werden die Long-/Short-Regeln vertauscht.
3. Handeln Sie nur innerhalb des optionalen Sitzungsfensters, das durch `SessionStart`/`SessionEnd` definiert ist, wenn `UseTimeFilter` aktiviert ist. Übernachtungssitzungen werden unterstützt.
4. Führen Sie höchstens einen neuen Eintrag pro Kerze aus. Bevor eine neue Position eröffnet wird, schließt die Strategie alle entgegengesetzten Bestände, um die MT5-Logik widerzuspiegeln.
5. Die Lautstärke wird durch den Parameter `TradeVolume` festgelegt. Wenn die Strategie bereits teilweise in die gewünschte Richtung geht, wird auf das gewünschte Volumen aufgefüllt.

## Risikomanagement

* `StopLossPoints` und `TakeProfitPoints` (in Preisschritten) werden den punktbasierten Stop- und Take-Profit-Distanzen des Experten zugeordnet.
* Durch die Aktivierung von `EnableTrailing` wird der Stoppabstand auf `TrailingStopPoints` umgeschaltet und die integrierte Nachlauf-Engine mit `TrailingStepPoints` als Anpassungsschritt aktiviert.
* `StartProtection` ist mit `useMarketOrders = true` konfiguriert, sodass Schutzaufträge sofort ausgeführt werden, ähnlich dem MT5-Handelsschließungsverhalten.

## Parameter

| Parameter | Beschreibung |
|-----------|-------------|
| `DeMarkerPeriod` | Mittelungszeitraum des DeMarker-Indikators. |
| `UpperLevel` / `LowerLevel` | Überkaufte/überverkaufte Schwellenwerte lösen Short-/Long-Positionen aus. |
| `ReverseSignals` | Tauschen Sie Long- und Short-Konditionen aus. |
| `StopLossPoints` | Anfänglicher Schutzstoppabstand, gemessen in Preisschritten. |
| `TakeProfitPoints` | Take-Profit-Distanz gemessen in Preisschritten. |
| `EnableTrailing` | Aktiviert den Trailing-Stop-Block. |
| `TrailingStopPoints` | Abstand des Trailing Stops, sobald das Trailing aktiv ist. |
| `TrailingStepPoints` | Minimale günstige Bewegung, bevor der Trailing Stop vorgeschoben wird. |
| `UseTimeFilter` | Beschränkt den Handel auf das Fenster `SessionStart`–`SessionEnd`. |
| `SessionStart` / `SessionEnd` | Inklusive/exklusive Sitzungsgrenzen (unterstützt Wrap-Around). |
| `TradeVolume` | Menge, die mit jeder Marktorder gesendet werden soll. |
| `CandleType` | Zu analysierende Kerzenserie (Standard 5 Minuten). |

## Hinweise zur Implementierung

* Der MT5-Experte hat einen Schwellenwert für die „nachlaufende Aktivierung“ einbezogen. Der Standard-Trailing-Schutz von StockSharp stellt nicht denselben Parameter bereit, daher wird das Trailing sofort aktiviert, wenn `EnableTrailing` wahr ist.
* Die Fehlerbehandlung für ungültige Losgrößen, Einfrierniveaus und Bid/Ask-Aktualisierungslogik wird von der Infrastruktur von StockSharp übernommen und daher bei der Konvertierung nicht berücksichtigt.
* Die Protokollierung erfolgt über die Basisklasse `Strategy` (rufen Sie `LogInfo/LogError` auf, wenn zusätzliche Ablaufverfolgung erforderlich ist).
