# Doji-Pfeile-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Konzept
Die Doji-Pfeile-Strategie konvertiert den ursprünglichen MetaTrader "Doji Arrows" Expert Advisor in die StockSharp-High-Level-API. Die Idee ist, auf eine echte Doji-Kerze zu warten und dann einen Ausbruch aus ihrem Bereich zu handeln. Eine Doji-Kerze repräsentiert Unentschlossenheit, daher deutet ein Schluss über dem Doji-Hoch auf bullische Stärke hin, während ein Schluss unter dem Doji-Tief bärische Kontrolle anzeigt.

1. Die Strategie verarbeitet nur abgeschlossene Kerzen aus dem konfigurierten `CandleType`-Abonnement.
2. Die vorherige Kerze wird analysiert, um zu bestimmen, ob es sich um ein Doji handelt. Die Kerze wird als Doji klassifiziert, wenn die absolute Differenz zwischen Eröffnung und Schluss kleiner oder gleich `DojiBodyPoints` multipliziert mit dem Sicherheitspreisschritt ist. Wenn der Parameter auf `0` gesetzt ist, wird ein einzelner Preisschritt als Toleranz verwendet, was der strikten Gleichheitsprüfung in der MQL5-Version entspricht.
3. Wenn die nächste Kerze über dem Doji-Hoch schließt, sendet die Strategie eine Market-Kauforder. Wenn die nächste Kerze unter dem Doji-Tief schließt, wird eine Market-Verkauforder ausgegeben. Bestehende entgegengesetzte Positionen werden automatisch durch das Market-Order-Volumen ausgeglichen.

Diese Sequenz spiegelt den ursprünglichen Expert Advisor wider, der einmal bei der Eröffnung jedes neuen Balkens reagierte.

## Risikomanagement
Die konvertierte Implementierung behält das Schutzverhalten des MQL-Skripts:

- **Stop Loss**: `StopLossPoints` kontrolliert, wie weit in Preisschritten der anfängliche Stop Loss vom Einstiegspreis entfernt platziert wird. Auf null setzen, um den festen Stop zu deaktivieren.
- **Take Profit**: `TakeProfitPoints` definiert die Distanz zum Gewinnziel in Preisschritten. Auf null setzen, um das Ziel zu überspringen.
- **Trailing Stop**: `TrailingStopPoints` und `TrailingStepPoints` reproduzieren den Trailing-Mechanismus. Sobald der Trade mehr als `TrailingStopPoints + TrailingStepPoints` gewinnt, wird das Stop-Niveau auf `TrailingStopPoints` vom letzten Schluss (höchster Schluss für Long, niedrigster Schluss für Short) gezogen. Trailing ist optional und aktiviert sich nur, wenn `TrailingStopPoints` größer als null ist.

Stops und Ziele werden bei jeder abgeschlossenen Kerze bewertet. Wenn ein Niveau verletzt wird (unter Verwendung von Kerzenhoch/-tief), verlässt die Strategie die Position mit einer Market Order und setzt den Schutzstatus zurück.

## Parameter
| Parameter | Standard | Beschreibung |
|-----------|---------|-------------|
| `StopLossPoints` | `30` | Distanz des anfänglichen Stop Loss in Preisschritten. |
| `TakeProfitPoints` | `90` | Distanz des Take Profit in Preisschritten. |
| `TrailingStopPoints` | `15` | Vom Trailing Stop verwendete Distanz in Preisschritten. |
| `TrailingStepPoints` | `5` | Zusätzlicher Gewinn erforderlich, bevor der Trailing Stop angepasst wird, in Preisschritten. |
| `DojiBodyPoints` | `1` | Maximal erlaubte Körpergröße der vorherigen Kerze in Preisschritten, um sie als Doji zu behandeln. `0` verwendet einen Preisschritt als Toleranz. |
| `CandleType` | `1 Stunde` | Kerzentyp für die Signalgenerierung abonniert. |

## Implementierungshinweise
- Die Strategie abonniert Kerzen durch `SubscribeCandles(CandleType).Bind(ProcessCandle)` und behält nur die letzte abgeschlossene Kerze im Speicher.
- Der Sicherheitspreisschritt wird über `Security?.PriceStep` abgerufen. Wenn er nicht verfügbar ist, wird ein Fallback-Wert von `1` verwendet, damit die Strategie weiterhin mit synthetischen oder historischen Daten arbeiten kann.
- Schutzlevels werden nach jedem Einstieg neu berechnet, und die Trailing-Logik kann einen Stop erstellen, auch wenn der feste Stop Loss deaktiviert ist (was dem MQL-Verhalten entspricht, bei dem der Trailing Stop von null aus starten konnte).
- Alle Aktionen werden mit Market Orders ausgeführt, um mit dem ursprünglichen Advisor, der auf sofortige Marktausführung angewiesen war, konform zu bleiben.

## Nutzungstipps
1. Konfigurieren Sie die Eigenschaften `Security`, `Portfolio` und `Volume`, bevor Sie die Strategie starten.
2. Passen Sie die punktbasierten Parameter entsprechend dem gehandelten Instrument an. Für mit fraktionalen Pips quotierte Instrumente erhöhen Sie die Werte, um zur Tick-Größe des Brokers zu passen.
3. Kombinieren Sie die Strategie mit StockSharp-Risikokontrollen oder Analysemodule, wenn eine fortgeschrittenere Positionsgrößenbestimmung erforderlich ist, da die Konvertierung die Fixed-Volume-Logik des ursprünglichen Codes beibehält.
