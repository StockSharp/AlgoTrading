# MacdPatternTrader Fortgeschrittene MultiMuster-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Die MacdPatternTrader-Strategie ist eine High-Level-StockSharp-Konvertierung des ursprünglichen MQL-Expertenberaters *MacdPatternTraderAll*. Das System lauscht auf abgeschlossene Kerzen und bewertet sechs unabhängige MACD-basierte Einstiegsmuster. Jedes Muster verwendet seine eigenen schnellen und langsamen exponentiellen gleitenden Durchschnitte sowie dedizierte Schwellenniveaus, um Umkehr- und Fortsetzungsstrukturen auf der MACD-Hauptlinie zu erkennen. Signale können gleichzeitig eintreffen und jedes sendet eine Marktorder, die durch das aktuelle Martingale-Volumen dimensioniert wird.

Die Strategie ergänzt die Einstiegslogik durch adaptives Risikomanagement. Stop-Loss-Preise werden aus jüngsten Hochs oder Tiefs mit einem Offset berechnet, während Take-Profit-Ziele durch aufeinanderfolgende Historiensegmente genauso wie in der MQL-Implementierung erweitert werden. Offene Positionen werden aktiv durch Teilausstiege basierend auf EMA/SMA-Filtern und einem Schwellenwert für unrealisierte Gewinne verwaltet. Nach jedem Flat-Close wird der Martingale-Multiplikator je nach realisiertem Ergebnis zurückgesetzt oder verdoppelt.

## Handelsregeln
1. **Muster 1 – Schwellen-Umkehr**
   * Verfolgt, wenn die MACD-Hauptlinie über eine obere Schwelle steigt, dann zurückfällt und dabei positiv bleibt.
   * Spiegelt das Verhalten für die untere Schwelle wider, wenn der MACD sich aus negativem Gebiet erholt.
2. **Muster 2 – Null-Niveau-Bounce**
   * Erfordert eine positive MACD-Phase, dann einen bärischen Hook unter die Nulllinie vor dem Verkauf.
   * Verwendet die symmetrische Logik für bullische Hooks über null zum Kauf.
3. **Muster 3 – Mehrstufige Sequenz**
   * Reproduziert die dreistufige Hoch- und Tieferkennung aus der MQL-Quelle mit verschachtelten Flags und Schwellenpaaren.
   * Setzt die Hilfszähler (`bars_bup`) nach jedem ausgeführten Trade zurück.
4. **Muster 4 – Lokales Hoch/Tief**
   * Wartet auf lokale MACD-Hochs oder -Tiefs in Bezug auf die vorherigen zwei Balken, um Short- bzw. Long-Signale aufzubauen.
5. **Muster 5 – Neutralband-Ausbruch**
   * Sucht Short-Einstiege, nachdem der Preis unter ein neutrales Band fällt und sofort unter einem bärischen Limit zurückkehrt.
   * Sucht Long-Einstiege, nachdem er über das neutrale Band gestiegen ist und über ein bullisches Limit gesprungen ist.
6. **Muster 6 – Aufeinanderfolgender Balkenzähler**
   * Zählt die Anzahl der Balken über oder unter den konfigurierten Schwellen und wird nur ausgelöst, wenn der Zähler den `TriggerBars`-Wert überschreitet, während er unter dem `MaxBars`-Limit bleibt.

## Risikomanagement und Handelsmanagement
* **Stop-Loss** – Bestimmt durch den höchsten (bei Short-Trades) oder niedrigsten (bei Long-Trades) Preis über die letzten `StopLossBars` Kerzen plus dem konfigurierten Offset in Preisschritteinheiten.
* **Take-Profit** – Durchsucht aufeinanderfolgende Historiensegmente von `TakeProfitBars` Kerzen, genau wie die verschachtelten `iLowest`/`iHighest`-Schleifen in der MQL-Version. Das Ziel verlängert sich, solange das nächste Segment einen extremeren Wert liefert.
* **Teilausstiege** – Sobald der unrealisierte Gewinn fünf Währungseinheiten übersteigt (approximiert durch Preisdifferenz × Positionsvolumen) und die EMA/SMA-Filter zustimmen, schließt die Strategie ein Drittel des offenen Volumens, dann die Hälfte des Restes.
* **Martingale-Lot-Kontrolle** – Nach einem Flat-Exit setzt die Strategie das Lot auf `InitialVolume` zurück, wenn der geschlossene Trade Geld gewonnen hat; andernfalls verdoppelt sich das Volumen (wenn `UseMartingale` aktiviert ist).
* **Zeitfilter** – Wenn `UseTimeFilter` aktiviert ist, bewertet die Strategie Einstiege nur innerhalb des `(StartTime, StopTime)`-Fensters. Stops werden noch bei jeder abgeschlossenen Kerze überprüft.

## Parameter
| Gruppe | Name | Beschreibung |
| --- | --- | --- |
| Muster 1 | `Pattern1Enabled` | Aktiviert das erste MACD-Muster. |
| Muster 1 | `Pattern1StopLossBars`, `Pattern1TakeProfitBars`, `Pattern1Offset` | Stop-Loss/Take-Profit-Lookback- und Offset-Einstellungen. |
| Muster 1 | `Pattern1Slow`, `Pattern1Fast` | Langsame und schnelle EMA-Längen für die MACD-Berechnung. |
| Muster 1 | `Pattern1MaxThreshold`, `Pattern1MinThreshold` | Obere und untere MACD-Schwellen. |
| Muster 2 | Gleiche Struktur wie Muster 1 mit eigenen Werten. |
| Muster 3 | Fügt zusätzliche Schwellen `Pattern3MaxLowThreshold` und `Pattern3MinHighThreshold` hinzu, um die gestufte Hoch-/Tief-Erkennung zu reproduzieren. |
| Muster 4 | Enthält `Pattern4AdditionalBars` (für Kompatibilität mit dem ursprünglichen Code beibehalten). |
| Muster 5 | Verwendet neutrale Schwellen für die Neutralband-Ausbrucherkennung. |
| Muster 6 | Fügt `Pattern6MaxBars`, `Pattern6MinBars`, `Pattern6TriggerBars` hinzu, um die Balkenzähler-Logik zu verwalten. |
| Verwaltung | `EmaPeriod1`, `EmaPeriod2`, `SmaPeriod3`, `EmaPeriod4` | Gleitende Durchschnitte für Teilausstiegsfilter. |
| Allgemein | `InitialVolume`, `UseTimeFilter`, `StartTime`, `StopTime`, `UseMartingale`, `CandleType` | Globale Verhaltenssteuerungen. |

## Hinweise
* Die Konvertierung behält die ursprüngliche Logikstruktur bei, einschließlich der segmentierten Take-Profit-Suche und Martingale-Reset-Regeln.
* Gewinnbasierte Teilausstiege verwenden eine Annäherung, da die StockSharp High-Level-API keine rohen Terminal-Gewinnwerte pro Position freigibt; stattdessen wird Preisdifferenz × Volumen verwendet.
* `Pattern4AdditionalBars` wird für Kompatibilität beibehalten, obwohl der ursprüngliche MQL-Code ihn nie direkt referenziert hat.
* Stops und Take-Profits werden auf geschlossenen Kerzen ausgewertet, da StockSharp in der High-Level-API keine Schutzorders automatisch anfügt.
