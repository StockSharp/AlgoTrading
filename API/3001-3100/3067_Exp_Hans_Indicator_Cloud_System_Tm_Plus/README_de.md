# Exp Hans Indicator Wolkensystem Tm Plus Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Exp Hans Indicator Wolkensystem Tm Plus ist eine sessionbasierte Ausbruchsstrategie, die das Verhalten des ursprünglichen MQL5 Expert Advisors reproduziert. Der Algorithmus überwacht die Farbzustände des Hans-Indikators auf einem konfigurierbaren Zeitrahmen. Er eröffnet eine neue Position, nachdem ein bullischer (Farben 0/1) oder bärischer (Farben 3/4) Ausbruch endet und der Preis in den Kanal zurückkehrt. Die Implementierung hält alle Handelsentscheidungen bei geschlossenen Kerzen, verwendet pip-basierte Risikolimits und spiegelt die zeitbasierte Liquidationsregel der MQL-Version wider.

Die Strategie operiert auf einem einzigen Instrument/Kerzen-Feed-Paar, das von `GetWorkingSecurities()` bezogen wird. Alle Ordergrößen werden aus der `Volume`-Eigenschaft der Strategie und dem von den Parametern bereitgestellten Geldmanagement-Anteil abgeleitet.

## Indikatorlogik
1. Kerzenzeitstempel werden von der Broker-Zeit (`LocalTimeZone`) in die Zielzeitzone (`DestinationTimeZone`) konvertiert. Standardmäßig arbeitet das Skript mit GMT+4, was der Referenzimplementierung entspricht.
2. Täglich werden zwei Londoner-Session-Bereiche gesammelt:
   - **Bereich 1**: 04:00–08:00 Zielzeit. Das Hoch/Tief dieses Zeitraums wird zum initialen Ausbruchskanal.
   - **Bereich 2**: 08:00–12:00 Zielzeit. Einmal abgeschlossen, ersetzt er den ersten Bereich für den Rest des Tages.
3. Jeder Bereich wird um `PipsForEntry` Pips auf beiden Seiten erweitert. Ein Pip entspricht dem Instrumenten-`PriceStep`, multipliziert mit 10, wenn das Wertpapier 3 oder 5 Dezimalstellen hat (MetaTrader-artige Dezimalpips).
4. Kerzenfarben werden genau wie im Indikator abgeleitet:
   - Schluss über dem oberen Band → Farbe `0` (bullischer Schluss) oder `1` (bärischer Schluss).
   - Schluss unter dem unteren Band → Farbe `4` (bärischer Schluss) oder `3` (bullischer Schluss).
   - Schluss innerhalb des Kanals → neutrale Farbe `2`.

## Handelsregeln
- **Einstieg**: Wenn die vorherige geschlossene Kerze eine bullische Farbe (0/1) hatte und die neueste nicht bullisch ist, eröffnet die Strategie eine Long-Position (wenn aktiviert). Symmetrisch dazu löst eine vorherige bärische Farbe (3/4), gefolgt von einer neutralen/gegenteiligen Farbe, einen Short-Einstieg aus.
- **Ausstieg**:
  - Direktionaler Ausstieg wenn die vorherige Farbe sich gegen die aktuelle Position wendet (0/1 für Shorts, 3/4 für Longs).
  - Optionaler zeitbasierter Ausstieg sobald die Haltezeit `HoldingMinutes` überschreitet.
  - Optionale Stop-Loss/Take-Profit-Niveaus in Punkten (`StopLossPoints`, `TakeProfitPoints`). Niveaus werden übersprungen, wenn das Wertpapier kein positives `PriceStep` offenlegt.
- Ausstiege werden vor neuen Einstiegen verarbeitet, sodass eine Position geflacht wird, bevor eine Umkehrorder gesendet wird.

## Parameter
| Parameter | Beschreibung | Standard |
|-----------|--------------|---------|
| `MoneyManagement` | Anteil des Strategie-`Volume` pro Trade. Werte ≤ 0 verwenden das volle Volumen. | `0.1` |
| `MoneyMode` | Platzhalter für die originalen Geldmanagement-Modi. Derzeit wird nur `Lot` angewendet. | `Lot` |
| `StopLossPoints` / `TakeProfitPoints` | Schutzstop und Gewinnziel in Punkten (Pips). Auf `0` setzen zum Deaktivieren. | `1000` / `2000` |
| `DeviationPoints` | Maximale akzeptable Ausführungsabweichung in Punkten. Zur Kompatibilität vorhanden; von der StockSharp-Orderschicht nicht durchgesetzt. | `10` |
| `AllowBuyEntries` / `AllowSellEntries` | Aktiviert Long-/Short-Einstiege. | `true` |
| `AllowBuyExits` / `AllowSellExits` | Aktiviert automatische Ausstiege für Long-/Short-Positionen. | `true` |
| `UseTimeExit` | Schaltet den zeitbasierten Liquidationsfilter ein. | `true` |
| `HoldingMinutes` | Maximale Haltezeit für eine Position in Minuten. | `1500` |
| `PipsForEntry` | Pip-Offset, der über/unter den Ausbruchsbereichen hinzugefügt wird. | `100` |
| `SignalBar` | Offset für geschlossene Kerzen für Signale. Verwenden Sie Werte ≥ 1 zur Übereinstimmung mit MT5-Logik. | `1` |
| `LocalTimeZone` | Broker/Server-Zeitzone (Stunden von UTC). | `0` |
| `DestinationTimeZone` | Zielzeitzone für Session-Grenzen. | `4` |
| `CandleType` | Zeitrahmen für Hans-Berechnungen. | `30m`-Kerzen |

## Geldmanagement und Ausführung
- Ordergröße = `Volume * MoneyManagement`, normalisiert auf den Instrumenten-`VolumeStep`. Wenn der berechnete Wert nicht-positiv ist, fällt die Logik auf einen Volumenschritt zurück.
- Wenn ein Umkehrsignal erscheint, sendet die Strategie eine einzige Marktorder gleich dem neuen Volumen plus jede offene entgegengesetzte Menge. Dies reproduziert das Verhalten von `BuyPositionOpen`/`SellPositionOpen` aus dem MQL-Helper.
- Stop-Loss- und Take-Profit-Niveaus werden bei jedem Einstieg neu berechnet und gelöscht, wenn eine Position geschlossen oder umgekehrt wird.

## Verwendungsrichtlinien
1. Hängen Sie die Strategie an ein Wertpapier an, das gültige `PriceStep`-, `Decimals`- und `VolumeStep`-Metadaten veröffentlicht.
2. Setzen Sie das gewünschte `Volume` auf der Strategie, bevor Sie sie starten. Der Geldmanagement-Anteil wird dann angewendet.
3. Wählen Sie einen Kerzentyp gleich dem in MetaTrader verwendeten (M30 Standard). Alle Berechnungen basieren auf abgeschlossenen Kerzen.
4. Passen Sie die Zeitzonen an, wenn Ihre Marktdatenquelle von der Standard-GMT+4-Zielzeit des Hans-Indikators abweicht.
5. Überwachen Sie die Protokolle auf Meldungen über fehlende Pip-Größe; Risikoniveaus werden übersprungen, wenn kein `PriceStep` verfügbar ist.

## Implementierungshinweise
- Farberkennung wird ausschließlich bei abgeschlossenen Kerzen über die High-Level-`SubscribeCandles`-API durchgeführt, was manuelle Indikatorbuffer vermeidet.
- Ausbruchsniveaus werden einmal pro Kerze neu berechnet und im Speicher gecacht; keine historischen Sammlungen werden erstellt.
- `DeviationPoints` wird für Konfigurationsvollständigkeit beibehalten, kann aber nicht mit einfachen Marktorders in StockSharp durchgesetzt werden.
- Die Strategie setzt ihren internen Zustand in `OnReseted()` zurück, um wiederholte Backtests ohne veraltete Session-Daten zu unterstützen.

## Einschränkungen
- Die aktuelle Implementierung unterstützt nur `SignalBar ≥ 1`, passend zum ursprünglichen EA-Verhalten bei neuen Balken-Ereignissen. Die Verwendung von `0` würde Tick-Level-Zugang erfordern, der im High-Level-Port nicht vorhanden ist.
- Geldmanagement-Modi außer `Lot` sind nicht implementiert. Erweitern Sie `GetOrderVolume()`, wenn Ihr Workflow von bilanbasierter Größenbestimmung abhängt.
- Ohne einen gültigen `PriceStep`-Wert können pip-basierte Abstände (Stop, Take-Profit, Hans-Offsets) nicht berechnet werden und werden ignoriert.
