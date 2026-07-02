# Farhad-Krabbenstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die **Farhad Crab Strategy** ist eine StockSharp High-Level-Portierung des MetaTrader Expertenberaters `FarhadCrab1.mq4`. Das Original EA ist ein schnelles Scalping-System, das für den M1-Zeitrahmen für GBP/JPY, GBP/USD und EUR/USD entwickelt wurde. Bei dieser Konvertierung wird die Handelslogik in C# neu erstellt, indem Filter für den gleitenden Intraday-Durchschnitt mit einem täglichen Trendsicherheitsnetz und automatisiertem Exit-Management kombiniert werden.

Die Strategie analysiert den aktuellen Zeitrahmen anhand eines 9-Perioden-EMA, der auf dem typischen Preis berechnet wird, und eines 9-Perioden-SMA, der auf der Öffnung der Kerze berechnet wird. Gleichzeitig verfolgt es einen geglätteten gleitenden Durchschnitt (SMMA) über 55 Perioden, der aus täglichen Kerzen gebildet wird. Immer wenn die kurzfristigen Filter genügend Aufwärtsdynamik zeigen und keine Position offen ist, wird ein Long-Trade ausgelöst. Wenn umgekehrt das Intraday-Hoch unter SMA der Eröffnungen bleibt, wird ein Short-Trade eröffnet. Der tägliche SMMA fungiert als Schutzschicht: Ein Überschreiten des Preises von unten zwingt alle Long-Trades zum Ausstieg, und ein Überschreiten von oben schließt Short-Positionen.

Das Exit-Management reproduziert das ursprüngliche Verhalten von EA mit konfigurierbaren Take-Profit-Levels in Pips und unabhängigen Trailing Stops für Long- und Short-Positionen. Die Trailing-Logik folgt der MetaTrader-Implementierung, indem sie den Stop erst verschiebt, nachdem der Markt um die konfigurierte Distanz vorgerückt ist. Die Strategie schließt Positionen über Market-Orders und nicht über ausstehende Stop-Orders, wodurch sie mit dem High-Level-Ereignisablauf API kompatibel ist.

## Hauptmerkmale

- **Indikatorsatz identisch mit EA** – 9-Perioden-EMA beim typischen Preis, 9-Perioden-SMA bei Eröffnungen und ein täglicher 55-Perioden-SMMA für die Trendrichtung.
- **Datenverarbeitung in mehreren Zeitrahmen** – abonniert gleichzeitig den Handelszeitrahmen und die täglichen Kerzen, sodass StockSharp die erforderlichen Indikatoren ohne manuelle Pufferung berechnen kann.
- **Konfigurierbare Exits** – symmetrische Take-Profit-Abstände (Long/Short) und Trailing-Stops, ausgedrückt in Pips, genau wie die ursprünglichen externen Eingaben.
- **Täglicher Sicherheitsschalter** – repliziert die Regel von EA, die Long-Positionen schließt, wenn der tägliche SMMA über den Tagesschlusskurs steigt, und Short-Positionen, wenn er darunter fällt.
- **Integrierter Schutz** – ruft `StartProtection()` einmal beim Start auf, um Positionen gemäß den Best Practices des Frameworks zu schützen.

## Parameter

| Parameter | Beschreibung | Standard |
|-----------|-------------|---------|
| `OrderVolume` | Auf neue Marktaufträge angewendetes Handelsvolumen. | `0.1` |
| `LongTakeProfitPips` | Take-Profit-Distanz für Long-Positionen, gemessen in Pips. | `10` |
| `ShortTakeProfitPips` | Take-Profit-Distanz für Short-Positionen, gemessen in Pips. | `10` |
| `LongTrailingStopPips` | Trailing-Stop-Distanz für Long-Trades. Das Nachlaufen ist deaktiviert, wenn es auf Null gesetzt ist. | `8` |
| `ShortTrailingStopPips` | Trailing-Stop-Distanz für Short-Trades. Das Nachlaufen ist deaktiviert, wenn es auf Null gesetzt ist. | `8` |
| `DailyMaPeriod` | Länge des täglichen geglätteten gleitenden Durchschnitts, der für Schutzausstiege verwendet wird. | `55` |
| `CandleType` | Primärer Zeitrahmen, der die Strategieberechnungen steuert. Standardmäßig werden 1-Minuten-Kerzen verwendet. | `1m` |

Alle Parameter werden über `StrategyParam<T>` verfügbar gemacht und dort, wo es sinnvoll ist, als optimierbar markiert, sodass sie über den StockSharp-Optimierer optimiert werden können.

## Handelsregeln

1. **Long-Einträge**: Wenn das aktuelle Kerzentief über dem 9-Perioden-EMA des typischen Preises bleibt und keine Position aktiv ist, eröffnen Sie einen Long-Trade.
2. **Short-Einträge**: Wenn das aktuelle Kerzenhoch unter dem 9-Perioden-SMA des Eröffnungspreises bleibt und keine Position aktiv ist, eröffnen Sie einen Short-Trade.
3. **Täglicher schützender Ausstieg (Long)**: Schließen Sie jede Long-Position, wenn der tägliche SMMA über den Tagesschlusskurs steigt, während er zuvor unter dem vorherigen Schlusskurs lag.
4. **Täglicher schützender Ausstieg (Short)**: Schließen Sie jede Short-Position, wenn der tägliche SMMA unter den Tagesschluss fällt, während er zuvor über dem vorherigen Schlusskurs lag.
5. **Take-Profit**: Schließen Sie die Position, sobald das konfigurierte Pip-Ziel erreicht ist.
6. **Trailing Stop**: Nachdem eine Position die Trailing-Distanz erreicht hat, können Sie Gewinne sichern, indem Sie die Retracement-Distanz überwachen und aussteigen, wenn der Preis um diesen Betrag zurückgeht.

## Implementierungshinweise

- Der Code basiert ausschließlich auf `SubscribeCandles().Bind(...)`-Aufrufen auf hoher Ebene, wodurch manuelle Indikatorpuffer entfallen und die Richtlinien des Projekts eingehalten werden.
- Pips werden aus dem `PriceStep` des Instruments mit der üblichen Anpassung im MetaTrader-Stil für 3- und 5-stellige Notierungen berechnet. Dadurch bleibt das Verhalten mit den punktbasierten Parametern von EA konsistent.
- Das Stop-Loss- und Take-Profit-Management wird intern durchgeführt, indem Positionen geschlossen werden, wenn die Bedingungen erfüllt sind, und nicht durch die Registrierung von Limit-/Stop-Orders. Dieser Ansatz entspricht den im Originalskript gefundenen sofortigen Exits und bleibt gleichzeitig mit der asynchronen Auftragsausführung in StockSharp kompatibel.
- Die Strategie setzt ihren Status innerhalb von `OnReseted` zurück und stellt so sicher, dass Optimierungsläufe und wiederholte Starts von vorne beginnen.

## Nutzungstipps

- Der ursprüngliche EA war auf hochvolatile GBP- und EUR-Paare im M1-Zeitrahmen zugeschnitten. Ähnliche Ergebnisse sind zu erwarten, wenn der gleiche Zeitrahmen und die gleichen Instrumente angewendet werden, die Parameter jedoch so ausgelegt sind, dass sie unterschiedlichen Volatilitätsprofilen Rechnung tragen.
- Da das System jeweils nur eine Position behält, eignet es sich für einfaches Backtesting und Live-Ausführung ohne komplexe Positionspyramide.
- Trailing-Stops werden bei Instrumenten mit glatten Trends effektiver. Erwägen Sie bei schwankenden Märkten eine Reduzierung der Nachlaufdistanz oder die ausschließliche Nutzung von Take-Profit-Exits.
- Als primäre Risikokontrolle dient der tägliche SMMA-Exit. Für Swing-orientierte Setups können Sie `DailyMaPeriod` erhöhen, um den Langzeitfilter weniger reaktiv zu machen.
