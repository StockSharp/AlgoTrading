# MACD Beispiel für eine klassische Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie reproduziert den Expert Advisor „MetaTrader 4 „MACD Sample““ unter Verwendung des übergeordneten API von StockSharp. Es handelt mit einem einzigen Instrument in beide Richtungen und spiegelt die ursprüngliche Logik wider: Trades werden ausgeführt, wenn die MACD-Linie ihre Signallinie auf der richtigen Seite von Null kreuzt, während ein Trend EMA die Richtung bestätigt. Schutzaufträge werden mit optionalen Trailing Stops in den integrierten Risikomanager von StockSharp umgewandelt.

## Handelslogik

1. Warten Sie, bis mindestens 100 fertige Kerzen vorhanden sind, damit MACD und EMA genügend Verlauf enthalten.
2. Berechnen Sie einen Standard MACD (12, 26, 9) zusammen mit seiner Signallinie und einem exponentiellen gleitenden Durchschnitt mit 26 Perioden, der als Richtungsfilter fungiert.
3. **Lange Eingabe** – nur zulässig, wenn keine Position vorhanden ist. Der MACD muss unter Null liegen, aber die Signallinie überschreiten, der vorherige MACD-Wert lag unter seinem Signal, der absolute MACD-Wert überschreitet den konfigurierbaren `MacdOpenLevel`-Schwellenwert (in Preispunkten) und der Trend EMA ist steigend.
4. **Kurzer Einstieg** – der symmetrische Aufbau: MACD über dem Nulldurchgang unter seinem Signal, der vorherige MACD lag über dem Signal, der aktuelle Wert überschreitet den Schwellenwert `MacdOpenLevel` und der Trend EMA ist fallend.
5. **Langer Ausstieg** – wenn MACD das Signal auf der positiven Seite von Null wieder unterschreitet und der Wert über `MacdCloseLevel` liegt. Die Position kann auch früher durch den von `StartProtection` verwalteten Trailing Stop oder Take-Profit geschlossen werden.
6. **Kurzer Ausgang** – wenn MACD das Signal auf der negativen Seite wieder kreuzt und der absolute MACD-Wert `MacdCloseLevel` überschreitet, oder durch die Schutzmodule.

Die Strategie hält nie mehr als eine Position gleichzeitig. Jeder Eintrag verwendet Marktaufträge, deren Größe durch die Eigenschaft `Volume` bestimmt wird. Die Schutzlogik basiert auf dem Risikocontroller von StockSharp, sodass Take-Profit-Abstände und Trailing-Stops mit der Tick-Größe des Instruments synchronisiert bleiben.

## Parameter

| Name | Beschreibung | Standard | Notizen |
| --- | --- | --- | --- |
| `FastEmaPeriod` | Schneller Zeitraum von EMA, der von MACD verwendet wird. | 12 | Optimierbarer Bereich 6…18.
| `SlowEmaPeriod` | Langsamer EMA Zeitraum, der von MACD verwendet wird. | 26 | Optimierbarer Bereich 20…32.
| `SignalPeriod` | Signalisieren Sie einen Zeitraum von EMA innerhalb von MACD. | 9 | Optimierbarer Bereich 5…13.
| `TrendMaPeriod` | EMA Länge für den Richtungsfilter. | 26 | Optimierbarer Bereich 20…40.
| `MacdOpenLevel` | Eintrittsschwelle ausgedrückt in MACD Punkten (Preisschritten). | 3 | Entspricht `MACDOpenLevel` im MT4-Code.
| `MacdCloseLevel` | Ausstiegsschwelle, ausgedrückt in MACD Punkten. | 2 | Entspricht `MACDCloseLevel`.
| `TakeProfitPoints` | Nehmen Sie Gewinne in Preispunkten mit (multipliziert mit der Tick-Größe des Instruments). | 50 | Auf 0 setzen, um Take-Profit zu deaktivieren.
| `TrailingStopPoints` | Trailing Stop in Preispunkten. | 30 | Auf 0 setzen, um den Trailing Stop zu deaktivieren.
| `CandleType` | Kerzenserie, die für Indikatoraktualisierungen verwendet wird. | Zeitrahmen von 5 Minuten | Unterstützt jeden Kerzentyp StockSharp.

## Implementierungshinweise

- Die Indikatoren MACD und EMA sind über `BindEx`/`Bind` an das Kerzenabonnement gebunden, sodass StockSharp gebrauchsfertige Werte ohne manuelles Zwischenspeichern einspeisen kann.
- Positionen werden nur geöffnet, wenn die Plattform `IsFormedAndOnlineAndAllowTrading()` meldet, wodurch Trades verhindert werden, während historische Daten noch geladen werden oder die Verbindung offline ist.
- Alle Schwellenwerte, die sich auf „Punkte“ beziehen, werden automatisch anhand der Preisstufe des Instruments skaliert und ahmen die `Point`-Konstante von MetaTrader nach.
- `StartProtection` wandelt den festen Take-Profit und Trailing Stop von MetaTrader in börsenseitige Schutzaufträge um. Aktivieren oder deaktivieren Sie jedes Modul, indem Sie den entsprechenden Parameter ändern.
- Eine umfassende Protokollierung (`LogInfo`) dokumentiert jede Handelsentscheidung und vereinfacht so den Vergleich mit dem ursprünglichen Expertenberater während der Migrationsvalidierung.

## Nutzungstipps

- Das Original EA zielt auf Forex-Majors in Intraday-Zeiträumen ab. Beginnen Sie mit ähnlichen Symbolen und passen Sie die Parameter an, wenn das Instrument eine andere Tick-Größe verwendet.
- Stellen Sie beim Testen von Symbolen mit exotischen Tick-Werten sicher, dass `Security.PriceStep` konfiguriert ist. andernfalls wird der Standardwert 1.0 verwendet.
- Kombinieren Sie es mit den Portfolioschutzfunktionen von StockSharp, wenn Sie eine Geldverwaltung auf Kontoebene über die Stopps pro Position hinaus benötigen.

## Schlagworte

- Trendfolge
- Schwung
- MACD Crossover
- Intraday (Standard 5 Minuten)
- Trailing Stop + Take Profit
