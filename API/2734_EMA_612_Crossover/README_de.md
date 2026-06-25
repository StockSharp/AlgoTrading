# Ema612CrossoverStrategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Zusammenfassung
- Port des MetaTrader 5-Experten **"EMA 6.12 (barabashkakvns Ausgabe)"** in die StockSharp High-Level-API.
- Handelt den Crossover zwischen einem schnellen und einem langsamen einfachen gleitenden Durchschnitt (das ursprüngliche Skript verwendete trotz seines EMA-Namens ebenfalls MODE_SMA).
- Fügt optionales Take-Profit- und Trailing-Stop-Management in absoluten Preiseinheiten hinzu, damit das Verhalten pro Instrument angepasst werden kann.

## Handelslogik
### Datenvorbereitung
- Die Strategie abonniert Kerzen des durch `CandleType` definierten Typs (15-Minuten-Zeitrahmen standardmäßig).
- Zwei einfache gleitende Durchschnitte werden berechnet: Länge `FastPeriod` für die schnelle Kurve und Länge `SlowPeriod` für die langsame Kurve. Die langsame Periode muss größer als die schnelle Periode sein.

### Einstiegsregeln
- Signale werden beim Abschluss jeder fertigen Kerze ausgewertet.
- Ein **bullischer Crossover** tritt auf, wenn der langsame SMA bei der vorherigen Kerze über dem schnellen SMA lag und bei der aktuellen Kerze darunter fällt. Jede offene Short-Position wird geschlossen und eine Long-Position mit dem konfigurierten `Volume` eröffnet.
- Ein **bärischer Crossover** tritt auf, wenn der langsame SMA bei der vorherigen Kerze unter dem schnellen SMA lag und bei der aktuellen Kerze darüber steigt. Jede offene Long-Position wird geschlossen und eine Short-Position mit dem konfigurierten `Volume` eröffnet.

### Ausstiegsregeln
- Offene Positionen werden auf dem entgegengesetzten Crossover wie oben beschrieben geschlossen.
- Optionaler Take-Profit: wenn `TakeProfitOffset` größer als null ist, berechnet die Strategie ein festes Preisziel vom Einstiegspreis. Long-Trades steigen aus, wenn der Preis `Einstieg + TakeProfitOffset` erreicht; Short-Trades steigen aus, wenn der Preis `Einstieg - TakeProfitOffset` erreicht.
- Optionaler Trailing Stop: wenn `TrailingStopOffset` größer als null ist, wartet die Strategie, bis der unrealisierte Gewinn `TrailingStopOffset + TrailingStepOffset` überschreitet. Sobald dieser Schwellenwert überschritten wird, wird der Stop-Preis auf `TrailingStopOffset` Abstand vom letzten Schluss enger gezogen, aber nur wenn das neue Niveau mindestens `TrailingStepOffset` näher am Preis liegt als der vorherige Stop. Long-Trades verwenden Tiefs, um den Stop auszulösen, Shorts verwenden Hochs.

## Parameter
| Parameter | Standard | Beschreibung |
|-----------|----------|--------------|
| `CandleType` | 15-Minuten-Zeitrahmen | Kerzenauflösung für SMA-Berechnungen und Signalauswertung. |
| `FastPeriod` | 6 | Periode für den schnellen einfachen gleitenden Durchschnitt. Muss > 0 und kleiner als `SlowPeriod` sein. |
| `SlowPeriod` | 54 | Periode für den langsamen einfachen gleitenden Durchschnitt. Muss > 0 und größer als `FastPeriod` sein. |
| `Volume` | 1 | Ordervolumen für neue Einstiege. |
| `TakeProfitOffset` | 0.001 | Optionale absolute Preisdistanz für das Take-Profit-Ziel. Auf 0 setzen, um zu deaktivieren. |
| `TrailingStopOffset` | 0.005 | Absoluter Abstand zwischen Preis und Trailing Stop. Auf 0 setzen, um Trailing zu deaktivieren. |
| `TrailingStepOffset` | 0.0005 | Zusätzliche günstige Bewegung, bevor der Trailing Stop verschoben wird. |

> **Wichtig:** Die Offsets werden in absoluten Preiseinheiten angegeben. Passen Sie sie an die Tick-Größe des Instruments an (zum Beispiel auf EURUSD mit einem 0.0001-Schritt entsprechen die Standardwerte jeweils 10, 50 und 5 Pips).

## Implementierungshinweise
- Verwendet den High-Level-Workflow `SubscribeCandles().Bind()` entsprechend den Projektrichtlinien.
- Die Chart-Ausgabe zeichnet beide SMAs und Trade-Markierungen, wenn Charting in der Umgebung verfügbar ist.
- Zustandsvariablen verfolgen den Einstiegspreis, das Trailing-Stop-Niveau und das Take-Profit-Ziel genau wie die MQL-Version.
- Die C#-Implementierung erzwingt `SlowPeriod > FastPeriod` beim Start, um eine ungültige Indikatorkonfiguration zu vermeiden.

## Verwendungstipps
- Optimieren Sie den Kerzen-Zeitrahmen und SMA-Perioden entsprechend dem gehandelten Markt (z.B. kürzere Perioden für Intraday-Futures, längere für Swing-Trading).
- Konvertieren Sie die Offsets von Pips oder Ticks in absolute Preiseinheiten, bevor Sie die Strategie ausführen.
- Trailing kann durch Setzen von `TrailingStopOffset` auf null deaktiviert werden; die Strategie wird dann ausschließlich auf den entgegengesetzten Crossover oder den optionalen Take-Profit für Ausstiege angewiesen sein.
