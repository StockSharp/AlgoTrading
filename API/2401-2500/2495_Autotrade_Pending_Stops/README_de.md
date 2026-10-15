# Autotrade-Strategie mit ausstehenden Stops
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie ist eine C#-Konvertierung des MetaTrader Expert Advisors *Autotrade (barabashkakvns Edition)*. Sie pflegt kontinuierlich zwei symmetrische Stop-Einstiegsorders rund um den aktuellen Marktpreis. Wenn der Markt flat bleibt und keine Position offen ist, aktualisiert die Strategie beide ausstehenden Orders. Wenn eine Stop-Order ausgeführt wird, wird die Position aktiv überwacht: Ausstiege werden ausgelöst, sobald sich die Kursbewegung stabilisiert oder ein absoluter Gewinn-/Verlust-Schwellenwert erreicht wird. Die Implementierung verwendet die StockSharp High-Level-API gemäß den Projektrichtlinien.

## Zuordnung der Originalparameter
| StockSharp-Parameter | MQL5-Parameter | Beschreibung |
| --- | --- | --- |
| `IndentTicks` | `InpIndent` | Abstand (in Preisschritten) zwischen dem aktuellen Preis und den Stop-Einstiegsorders. |
| `MinProfit` | `MinProfit` | Minimaler schwebender Gewinn (Kontowährung), der für den Ausstieg in einer ruhigen Marktphase benötigt wird. |
| `ExpirationMinutes` | `ExpirationMinutes` | Lebensdauer der ausstehenden Stop-Orders, bevor sie storniert und neu erstellt werden. |
| `AbsoluteFixation` | `AbsoluteFixation` | Absolutes Gewinn- oder Verlustniveau (Währung), das den Positionsschluss erzwingt. |
| `StabilizationTicks` | `InpStabilization` | Maximale Größe des vorherigen Kerzenkörpers, der als Konsolidierungszone behandelt wird. |
| `OrderVolume` | `Lots` | Volumen, das für die Buy-Stop- und Sell-Stop-Order verwendet wird. |
| `CandleType` | `Period()` | Kerzenserie, die die Logik antreibt (standardmäßig 1-Minuten-Zeitrahmen). |

Alle numerischen Eingaben, die Preisabstände darstellen, werden von "Punkten" in tatsächliche Preisschritte durch den `Security.PriceStep`-Wert konvertiert. Gewinnbasierte Schwellenwerte werden mit `Security.StepPrice` berechnet, was die MQL-Gewinnberechnungen in der Einzahlungswährung widerspiegelt.

## Handelslogik
### Deployment ausstehender Orders
1. Die Strategie reagiert nur auf abgeschlossene Kerzen (`CandleStates.Finished`).
2. Die erste Kerze wird verwendet, um historische Daten (vorheriges Open/Close) zu speichern und sofort ausstehende Orders zu planen.
3. Wenn keine Position offen ist, werden inaktive Referenzen gelöscht und:
   - Eine Buy-Stop wird bei `Close + IndentTicks * PriceStep` platziert.
   - Eine Sell-Stop wird bei `Close - IndentTicks * PriceStep` platziert.
4. Jede ausstehende Order erhält einen Ablaufzeitstempel von `CloseTime + ExpirationMinutes` Minuten. Wenn diese Zeit erreicht ist, wird die Order storniert und bei der nächsten Kerze neu erstellt.

### Positionsverwaltung
1. Sobald eine Stop-Order ausgeführt wird, wird die entgegengesetzte ausstehende Order storniert, um unerwünschtes Hedging im netting-basierten StockSharp-Kontomodell zu vermeiden.
2. Die Strategie speichert den vorherigen Kerzenkörper (`|Open - Close|`), um ruhige Marktbedingungen zu erkennen.
3. Bei jeder Kerze mit offener Position:
   - Der nicht realisierte Gewinn wird in Währung geschätzt, indem die Preisdifferenz gegenüber `PositionAvgPrice` verwendet und mit `Security.PriceStep` und `Security.StepPrice` skaliert wird.
   - Wenn der Gewinn `MinProfit` überschreitet **und** der vorherige Kerzenkörper unter `StabilizationTicks * PriceStep` liegt, wird die Position zum Marktpreis geschlossen.
   - Unabhängig von der Stabilisierung wird die Position auch zum Marktpreis geschlossen, wenn der absolute Gewinn oder Verlust `AbsoluteFixation` überschreitet.
4. Wenn die Position auf flat zurückgeht, werden alle verbleibenden ausstehenden Orders gelöscht.

### Zusätzliche Verhaltensweisen
- Es ist jeweils nur eine Position erlaubt; Order-Volumina werden mit `OrderVolume` verrechnet.
- Da StockSharp während Backtests Bid/Ask nicht auf die gleiche Weise wie MetaTrader exponiert, wird der Schlusskurs der abgeschlossenen Kerze als Referenzniveau für neue Stop-Orders verwendet.
- Die Strategie aktualisiert automatisch den gecachten `Volume`-Wert, wenn `OrderVolume` über Parameter oder Optimierung angepasst wird.

## Implementierungshinweise und Unterschiede
- Gewinnberechnungen hängen von `Security.PriceStep` und `Security.StepPrice` ab. Stellen Sie sicher, dass diese Felder in den Instrumentenmetadaten ausgefüllt sind; andernfalls wird der Wert `1` als Fallback verwendet.
- Die ursprüngliche MQL-Version erlaubte temporäres Hedging (mehrere Orders in entgegengesetzten Richtungen). Der StockSharp-Port storniert den nicht verwendeten Stop sofort nach einer Ausführung, um dem Netting-Modell der Plattform zu entsprechen.
- Die Ablaufzeit ausstehender Orders verwendet den `CloseTime` der Kerze. Wenn historische Daten keine Schlusszeitstempel enthalten, passen Sie den Feed an, um diese bereitzustellen, oder erweitern Sie den Code entsprechend.
- Die Strategie funktioniert mit jedem Kerzendatentyp durch Anpassung von `CandleType`. Standard-Kerzen sind zeitrahmenbasiert (`TimeSpan.FromMinutes(1).TimeFrame()`).

## Verwendungsempfehlungen
1. Konfigurieren Sie die Kerzenserie, die dem in MetaTrader verwendeten Chart-Zeitraum entspricht.
2. Setzen Sie `IndentTicks`, `StabilizationTicks` und Gewinnschwellen in Bezug auf die Tick-Größe und den Tick-Wert des Instruments.
3. Überprüfen Sie, ob das Portfolio Hedging oder Netting nach Wunsch verwendet. Die Strategie setzt Netting voraus und schließt das Buch, bevor Stop-Orders neu aufgesetzt werden.
4. Verwenden Sie die bereitgestellten Parameter für die Optimierung in StockSharp Designer oder Backtester, um das Verhalten an verschiedene Märkte anzupassen.
5. Überwachen Sie die Log-Ausgabe: Der Code hängt von abgeschlossenen Kerzen und Marktverfügbarkeit (`IsFormedAndOnlineAndAllowTrading()`) ab, bevor neue Orders übermittelt werden.

## Risikohinweis
Automatisierter Handel birgt erhebliche Risiken. Testen Sie gründlich, validieren Sie die Parameter auf historischen Daten und bestätigen Sie broker-spezifische Anforderungen (wie Mindestabstände für Stop-Orders), bevor Sie die Strategie auf einem Live-Konto einsetzen.
