# FT TIME BIGDOG Breakout-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **FT TIME BIGDOG**-Strategie ist ein London-Session-Breakout-System, das aus dem MetaTrader 4 Expert Advisor `FT_TIME_BIGDOG.mq4` (Verzeichnis `MQL/9259`) konvertiert wurde.
Es misst den Konsolidierungsbereich, der sich zwischen den konfigurierten Start- und Stoppstunden bildet, und platziert dann Stop-Orders oberhalb und unterhalb dieses Bereichs, sobald sich das Fenster schließt.
Die StockSharp-Version behält das ursprüngliche Verhalten bei und stellt gleichzeitig konfigurierbare Parameter für Breakout-Timing, Orderentfernung und Risikomanagement bereit.

## Handelslogik
1. An jedem Handelstag zeichnet die Strategie das höchste Hoch und das niedrigste Tief der fertigen Kerzen auf, deren Öffnungszeit zwischen **StartHour** und **StopHour** (einschließlich) liegt.
2. Wenn die akkumulierte Spanne nach Ablauf der Stop-Hour-Kerze kleiner als **RangeLimitPoints** ist, werden zwei ausstehende Stop-Orders zugelassen:
   - Ein **Kaufstopp** beim Rekordhoch.
   - Ein **Verkaufsstopp** beim aufgezeichneten Tief.
3. Aufträge werden nur erstellt, wenn der Marktpreis mindestens **OrderBufferPoints** vom Einstiegsniveau entfernt ist. Sofern verfügbar, werden die besten Geld-/Briefkurse verwendet, andernfalls wird der letzte Kerzenschluss verwendet.
4. Jede ausstehende Order beinhaltet einen Schutzstopp auf der gegenüberliegenden Seite der Spanne und einen durch **TakeProfitPoints** definierten Take-Profit-Offset.
5. Wenn eine Position eröffnet wird, wird die entgegengesetzte ausstehende Order storniert. Die aktive Position wird anhand abgeschlossener Kerzen überwacht: Wenn der Preis das gespeicherte Stop-Loss- oder Take-Profit-Niveau berührt, wird die Position zum Marktwert geschlossen.
6. Der Zyklus läuft höchstens einmal pro Tag; Der gesamte Status wird zu Beginn des nächsten Handelstages zurückgesetzt.

## Parameter
| Parameter | Standard | Beschreibung |
|-----------|---------|-------------|
| `StartHour` | 14 | Stunde (0–23), die den Beginn des Akkumulationsfensters markiert. |
| `StopHour` | 16 | Stunde, in der ausstehende Orders berechtigt werden. Muss größer oder gleich `StartHour` sein. |
| `RangeLimitPoints` | 50 | Maximale Breite des Sitzungsbereichs in Brokerpunkten (Punkte × `PointMultiplier`). Bei größeren Sortimenten werden keine Bestellungen aufgegeben. |
| `TakeProfitPoints` | 50 | Auf ausgelöste Positionen angewendete Take-Profit-Distanz, ausgedrückt in Broker-Punkten. |
| `OrderBufferPoints` | 20 | Erforderlicher Mindestabstand zwischen dem Marktpreis und einer ausstehenden Order. Verhindert, dass Bestellungen zu nahe am aktuellen Preis platziert werden. |
| `PointMultiplier` | 1 | Auf die Punktgröße des Instruments angewendeter Multiplikator. Für fünfstellige Forex-Symbole auf 10 einstellen. |
| `Volume` | 0,1 | Ordervolumen für beide Stop-Orders. |
| `CandleType` | 1 Stunde | Kerzenreihe zur Reichweitenmessung und Antriebssignalauswertung. |

## Risiko- und Handelsmanagement
- Der Stop-Loss für Long-Trades entspricht dem Sitzungstief; Der Stop-Loss für Short-Trades entspricht dem Sitzungshoch.
- Take-Profit-Level werden aus dem Breakout-Preis unter Verwendung von `TakeProfitPoints` und der Punktgröße des Instruments berechnet.
- Alle Risikokontrollen werden bei Kerzenschlussereignissen durchgeführt; Intrabar-Exkursionen über die Stop-Level hinaus können zu verzögerten Ausstiegen führen.

## Unterschiede zum ursprünglichen Expert Advisor
- Die MetaTrader-Version arbeitet mit Tick-Ereignissen, während dieser Port auf fertigen Kerzen und Level-1-Updates basiert. Das Verhalten innerhalb einer Kerze kann daher geringfügig abweichen.
- Bei der Punktumrechnung wird `Security.PriceStep` multipliziert mit `PointMultiplier` verwendet. Überprüfen Sie diese Kombination, bevor Sie sie live ausführen.
- Bestellungen werden nur aufgegeben, wenn `StartHour <= StopHour`. In diesem Port werden keine Mitternachtsfenster unterstützt.

## Nutzungshinweise
1. Weisen Sie die gewünschte Sicherheit zu und stellen Sie sicher, dass Daten der Ebene 1 für genaue Pufferprüfungen verfügbar sind.
2. Konfigurieren Sie die Handelszeiten entsprechend der Zeitzone des Brokers.
3. Führen Sie zunächst eine Simulation durch, um die Punktkonvertierung und das Timing im Verhältnis zu Ihrem Datenfeed zu validieren.
4. Setzen Sie die Strategie zurück oder stoppen Sie sie, bevor Sie ausstehende Aufträge manuell ändern, um Konflikte im Status zu vermeiden.

## Dateien
- `CS/FtTimeBigdogStrategy.cs` – Kernimplementierung von StockSharp mit detaillierten Inline-Kommentaren.
- `MQL/9259/FT_TIME_BIGDOG.mq4` – ursprüngliche MetaTrader-Quelle, die für die Konvertierung verwendet wurde.
