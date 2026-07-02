# AOCCI-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die AOCCI-Strategie ist eine direkte Umsetzung des Expertenberaters MetaTrader 4 „AOCCI“. Es kombiniert Momentum- und Mean-Reversion-Filter unter Verwendung des Awesome Oscillator (AO) und des Commodity Channel Index (CCI) zusammen mit einem täglichen Floor-Pivot. Die konvertierte Version funktioniert auf der hohen Ebene API von StockSharp und behält die gleiche Schutzlogik wie das Originalskript bei.

## Logik
1. **Datenvorbereitung**
   - Verwendet Intraday-Kerzen (Standard 1 Stunde) zur Signalgenerierung.
   - Verwendet Tageskerzen, um den Pivot des vorherigen abgeschlossenen Tages zu berechnen (Hoch + Tief + Schlusskurs dividiert durch 3).
   - Verfolgt die letzten sechs Intraday-Eröffnungskurse, um große Lücken zu erkennen.
2. **Lückenfilter**
   - Jede einzelne Schrittdifferenz, die den Schwellenwert des *Big Jump Filter* überschreitet, löscht das aktuelle Signal.
   - Jede zweistufige kombinierte Differenz, die den *Double Jump Filter*-Schwellenwert überschreitet, löscht das Signal ebenfalls aus.
3. **Indikatorprüfungen**
   - AO muss größer als Null sein und CCI darf auf dem aktuellen Balken nicht negativ sein.
   - Mindestens eine der folgenden Aussagen muss auf dem vorherigen Balken zutreffen: AO unter Null, CCI bei oder unter Null oder Preis unter dem Pivot.
4. **Richtungsfilter**
   - Der Schlusskurs muss über dem Pivot-Level bleiben.
5. **Bestellungen**
   - Der ursprüngliche Expertenberater eröffnet nur Long-Trades, da die Short-Bedingung die Long-Logik dupliziert. Die Konvertierung behält dieses Verhalten bei.
   - Market Orders verwenden das konfigurierte *Order Volume*.
6. **Schutz**
   - Anfänglicher Stop-Loss und Take-Profit werden in Preisschritten ausgedrückt.
   - Der optionale Trailing-Stop verschärft den Stop, sobald sich der Preis mindestens um die Trailing-Distanz zugunsten der Position bewegt.

## Parameter
| Name | Beschreibung |
| --- | --- |
| `CciPeriod` | Zeitraum für den Commodity Channel Index (Standard 55). |
| `SignalCandleOffset` | Zusätzlicher Offset wird angewendet, wenn auf historische Tageskerzen verwiesen wird (Standard 0). |
| `StopLossPoints` | Stop-Loss-Distanz in Preisschritten. |
| `TakeProfitPoints` | Take-Profit-Distanz in Preisschritten. |
| `TrailingStopPoints` | Trailing-Stop-Distanz in Preisschritten (0 deaktiviert Trailing). |
| `BigJumpPoints` | Maximal zulässige Eröffnungslücke eines einzelnen Balkens, ausgedrückt in Preisschritten. |
| `DoubleJumpPoints` | Maximal zulässige kombinierte Zwei-Balken-Gap, ausgedrückt in Preisschritten. |
| `OrderVolume` | Bei der Übermittlung von Marktaufträgen verwendetes Volumen. |
| `CandleType` | Intraday-Kerzentyp (Standard-1-Stunden-Balken). |
| `DailyCandleType` | Täglicher Kerzentyp, der für die Pivot-Berechnung verwendet wird. |

## Nutzungshinweise
- Die Strategie erfordert sowohl Intraday- als auch tägliche Datenabonnements.
- Der Preisschritt (Tickgröße) vom ausgewählten Wertpapier wird verwendet, um punktbasierte Risikoparameter in tatsächliche Preise umzuwandeln.
- Die Trailing-Stop-Verwaltung wird auf abgeschlossene Kerzen angewendet und spiegelt das Verhalten des ursprünglichen EA wider.
- Da die ursprüngliche MQL4-Version niemals Short-Trades auslöst, behält die Konvertierung absichtlich den gleichen Regelsatz bei.
