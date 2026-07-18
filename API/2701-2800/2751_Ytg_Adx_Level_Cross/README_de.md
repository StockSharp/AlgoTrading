# Ytg ADX Niveau-Kreuzung Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie portiert Yuriy Tokmans `_ADX.mq5`-Expert-Advisor in die StockSharp-High-Level-API. Sie überwacht den Average Directional Index und reagiert, wenn die +DI- oder -DI-Komponenten konfigurierbare Schwellenwerte überschreiten. Orders werden jeweils nur einmal eröffnet, was der ursprünglichen MQL-Logik entspricht, und schützende Stop-Loss- und Take-Profit-Level in Preispunkten werden automatisch angewendet.

## Übersicht

- **Marktregime**: Funktioniert bei Trend- oder stark direktionalen Bewegungen, bei denen DI-Spitzen Ausbrüche begleiten.
- **Richtung**: Öffnet entweder Long- oder Short-Positionen, aber nie beide gleichzeitig.
- **Zeitrahmen**: Gesteuert durch den `CandleType`-Parameter (Standard 1-Stunden-Kerzen).
- **Daten**: Verwendet fertige Kerzen zur Berechnung von ADX/DI-Werten aus dem `AverageDirectionalIndex`-Indikator.

## Handelslogik

1. Die ausgewählte Kerzenserie abonnieren und den ADX-Indikator mit dem konfigurierten `AdxPeriod` erstellen.
2. Für jede fertige Kerze die +DI- und -DI-Werte sammeln und nur die vom `Shift`-Parameter benötigte Historienmenge behalten. Ein `Shift` von 1, identisch mit dem MQL-Standard, wertet die vorherige geschlossene Kerze aus.
3. **Long-Einstieg**: Ausgelöst, wenn der verschobene +DI-Wert über `LevelPlus` steigt, während sein vorheriger Wert unter dem gleichen Schwellenwert lag. Die Strategie prüft, dass aktuell keine Position offen ist, bevor sie zum Markt kauft.
4. **Short-Einstieg**: Ausgelöst, wenn der verschobene -DI-Wert über `LevelMinus` steigt, während sein vorheriger Wert unter diesem Niveau lag. Ein Marktverkauf wird nur gesendet, wenn keine aktive Position vorhanden ist.
5. Ausstiege werden ausschließlich durch schützende Orders gehandhabt, die über `StartProtection` gestartet werden: ein fester Take-Profit und Stop-Loss gemessen in Preispunkten, entsprechend `TP` und `SL` aus dem Originalcode.

Diese Implementierung vermeidet bewusst das Averaging in Positionen, Wiedereinstiege bei aktiven Trades oder zusätzliche Filter, was dem leichtgewichtigen Verhalten des Quell-EAs entspricht.

## Parameter

| Parameter | Standard | Beschreibung |
|-----------|----------|--------------|
| `CandleType` | 1-Stunden-Zeitrahmen | Zeitrahmen des Kerzen-Abonnements für die ADX-Berechnung. |
| `AdxPeriod` | 28 | Länge des Average Directional Index und seiner DI-Berechnungen. |
| `LevelPlus` | 5 | Schwellenwert, den die +DI-Serie überschreiten muss, um eine Long-Position zu eröffnen. |
| `LevelMinus` | 5 | Schwellenwert, den die -DI-Serie überschreiten muss, um eine Short-Position zu eröffnen. |
| `Shift` | 1 | Anzahl der geschlossenen Kerzen, die bei der DI-Kreuzungsbewertung zurückgeschaut werden (1 = vorherige Kerze). |
| `TakeProfitPoints` | 500 | Abstand in Preispunkten für die Take-Profit-Order. Intern mit der Tick-Größe des Instruments multipliziert. |
| `StopLossPoints` | 500 | Abstand in Preispunkten für die schützende Stop-Loss-Order. |
| `TradeVolume` | 0.1 | Basisvolumen für neue Marktorders, entspricht der `Lots`-Einstellung im MQL-Experten. |

## Risikomanagement

- `StartProtection` konvertiert die punktbasierten Take-Profit- und Stop-Loss-Werte in absolute Preisdistanzen unter Verwendung des Instrument-`PriceStep`.
- Kein Trailing Stop oder Breakeven-Logik wird angewendet; Ausstiege erfolgen ausschließlich über die konfigurierten Schutz-Orders.

## Hinweise und Tipps

- Extrem niedrige DI-Schwellenwerte können zu häufigen Sägezahn-Trades führen, während höhere Level auf stärkere Richtungsausbrüche warten.
- Der `Shift`-Parameter kann erhöht werden, wenn Bestätigung von früheren Kerzen benötigt wird, zum Beispiel auf höheren Zeitrahmen zur Filterung von Intrabar-Rauschen.
- Da die Strategie jeweils nur eine Position handelt, sollten manuelle Eingriffe oder externe Trades auf demselben Konto vermieden werden, um Konflikte mit dem internen Positions-Tracking zu verhindern.
