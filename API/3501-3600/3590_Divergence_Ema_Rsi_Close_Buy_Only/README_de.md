# Divergenz + EMA + RSI Nur Kauf schließen
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Diese Strategie portiert den Expertenberater „Divergence + ema + rsi close buy only“ von MetaTrader auf den hochrangigen API von StockSharp. Es reagiert auf **5-Minuten-Kerzen** und zieht gleichzeitig **stündliche** und **tägliche** Daten heran, um die Trendausrichtung und überverkaufte Bedingungen zu bestätigen. Bestellungen sind nur auf lange Sicht möglich. Einträge erfordern eine bullische MACD-Histogrammdivergenz, die durch einen stündlichen stochastischen Crossover innerhalb eines engen überverkauften Bandes und durch eine steigende tägliche EMA-Struktur bestätigt wird. Exits basieren auf einer festen RSI-Überschreitung in Kombination mit einem optionalen Stop-Loss- und Take-Profit-Schutz, der vom Framework verwaltet wird.

## Handelslogik

1. **Trendfilter für längere Zeiträume**
   - Täglich muss EMA(9) über EMA(20) liegen, um einen vorherrschenden Aufwärtstrend sicherzustellen.
   - Der letzte 5-Minuten-Schlusskurs muss unter dem täglichen EMA(9) bleiben, damit bei Pullbacks Long-Einstiege versucht werden.

2. **Stündliche stochastische Bestätigung**
   - Der letzte abgeschlossene stündliche stochastische %K-Wert muss zwischen `StochasticLowerBound` (Standard 0) und `StochasticUpperBound` (Standard 40) liegen.
   - %K muss auf dem letzten Stundenbalken %D überschritten haben (aktueller %K > %D, während der vorherige %K ≤ vorheriger %D).

3. **MACD Divergenzauslöser (5 Minuten)**
   - Das MACD-Histogramm (MACD-Linie minus Signallinie) muss sich um mindestens `MacdThreshold` verbessern, während der 5-Minuten-Schluss ein niedrigeres Tief im Vergleich zur vorherigen Kerze setzt. Dies entspricht in etwa der bullischen Divergenz, die vom ursprünglichen EA verwendet wurde.

4. **Eintrittsausführung**
   - Wenn alle Filter übereinstimmen und keine Long-Position offen ist, sendet die Strategie einen Marktkauf. Wenn eine unerwartete Short-Position besteht, wird das angeforderte Volumen erhöht, um die Position zu neutralisieren, bevor eine Long-Position eingegangen wird.

5. **Ausgangsregeln**
   - Ein schützender RSI-Ausgang schließt den Long, wenn der 5-Minuten-RSI `RsiExitLevel` überschreitet (Standard 77).
   - `StartProtection` aktiviert sowohl Stop-Loss- als auch Take-Profit-Level, die von Pips in Preisabstände umgewandelt werden, wenn die entsprechenden Parameter positiv sind.

6. **Auftragsverwaltung**
   - Alle aktiven Aufträge werden storniert, bevor ein neuer Marktkaufauftrag gesendet wird, um doppelte Ausführungen zu vermeiden.
   - Die Lautstärke ist standardmäßig auf den Parameter `TradeVolume` eingestellt und kann zur Optimierung angepasst werden.

## Parameter

| Name | Beschreibung | Standard |
|------|-------------|---------|
| `CandleType` | Primärer Zeitrahmen für MACD, RSI und Ausführung. | 5-Minuten-Kerzen |
| `HourTimeFrame` | Stündlicher Zeitrahmen, der vom stochastischen Filter verwendet wird. | 1 Stunde |
| `DayTimeFrame` | Täglicher Zeitrahmen für EMA Trendbestätigung. | 1 Tag |
| `MacdFastPeriod` / `MacdSlowPeriod` / `MacdSignalPeriod` | MACD-Struktur im primären Zeitrahmen. | 6 / 13 / 5 |
| `MacdThreshold` | Mindestens MACD Histogrammerhöhung, um eine Divergenz zu akzeptieren. | 0,0003 |
| `DailyFastPeriod` / `DailySlowPeriod` | Täglich EMA Zeiträume. | 9 / 20 |
| `StochasticKPeriod` / `StochasticDPeriod` / `StochasticSlowing` | Stündliche stochastische Konfiguration. | 30.05.09 |
| `StochasticUpperBound` / `StochasticLowerBound` | Akzeptierter stündlicher %K-Bereich. | 40 / 0 |
| `RsiPeriod` | RSI Länge im primären Zeitrahmen. | 7 |
| `RsiExitLevel` | RSI-Wert, der lange Exits erzwingt. | 77 |
| `TradeVolume` | Basisbestellgröße für Käufe. | 0,01 |
| `StopLossPips` | Stop-Loss-Distanz in Pips (0 deaktiviert). | 100 |
| `TakeProfitPips` | Take-Profit-Distanz in Pips (0 deaktiviert). | 200 |

## Notizen

- Die Strategie abonniert drei Datenströme: den konfigurierten primären Zeitrahmen, eine stündliche Serie und eine tägliche Serie. Jeder Stream steuert seinen eigenen Indikatorsatz über `Bind`/`BindEx`, um die Implementierung prägnant und ereignisgesteuert zu halten.
- Indikatorwerte werden nur bei fertigen Kerzen verarbeitet, um die ursprünglichen Verschiebungsparameter von EA widerzuspiegeln.
- Die MACD-Divergenzerkennung verwendet den Schluss- und Histogrammwert des vorherigen Balkens als einfache, aber robuste Annäherung an die vom Builder generierte Logik aus der Quelldatei MQL.
- Stop-Loss und Take-Profit werden von `StartProtection` verwaltet, um mit der Ausführung durch den Broker synchronisiert zu bleiben und Backtesting oder Live-Handel ohne manuelle Auftragsreplikation zu unterstützen.
