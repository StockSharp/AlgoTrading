# MACD Multi-Zeitrahmen Experte Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie repliziert den ursprünglichen MetaTrader-Roboter "MACD Expert" im StockSharp-Framework. Sie synchronisiert MACD-Trends über vier Zeitrahmen—5 Minuten, 15 Minuten, 1 Stunde und 4 Stunden—und erlaubt nur dann eine neue Position, wenn jeder Zeitrahmen in dieselbe Richtung zeigt. Das Ziel ist es, Multi-Zeitrahmen-Momentum-Ausrichtung zu erfassen und Perioden hoher Spreads herauszufiltern.

## Daten und Indikatoren
- **Kerzen**: 5m (Ausführung), 15m, 1h und 4h Bestätigungen. Alle Kerzen verwenden Schlusskurse und nur abgeschlossene Bars.
- **Indikator**: `MovingAverageConvergenceDivergenceSignal` mit Standardwerten 12/26/9. Jeder Zeitrahmen hat seine eigene MACD-Instanz, damit sich Signale nicht gegenseitig beeinflussen.
- **Level-1-Kurse**: Beste Geld-/Briefkurse werden verbraucht, um den Live-Spread vor dem Öffnen von Trades zu überwachen.

## Handelslogik
1. Warten, bis alle vier MACD-Instanzen einen abgeschlossenen Wert emittieren.
2. Die Beziehung zwischen der MACD-Linie und der Signallinie in jedem Zeitrahmen berechnen.
3. Einen maximalen Spread-Filter, gemessen in Preispunkten (Preisschritten), durchsetzen.
4. Maximal eine Position gleichzeitig öffnen; bestehende Positionen müssen durch Stop-Loss oder Take-Profit beendet werden, bevor eine neue Order erlaubt wird.

### Long-Setup
- Die MACD-Signallinie liegt auf **allen** überwachten Zeitrahmen über der MACD-Linie.
- Spread übersteigt `MaxSpreadPoints` nicht.
- Eine Long-Position wird mit `OrderVolume` Lots beim Schlusskurs der neuesten 5-Minuten-Kerze eröffnet.

### Short-Setup
- Die MACD-Signallinie liegt auf **allen** überwachten Zeitrahmen unter der MACD-Linie.
- Spread übersteigt `MaxSpreadPoints` nicht.
- Eine Short-Position wird mit `OrderVolume` Lots beim Schlusskurs der neuesten 5-Minuten-Kerze eröffnet.

### Positionsmanagement
- Long-Trades platzieren logische Ziele `TakeProfitPoints` über dem Einstieg und Stops `StopLossPoints` darunter.
- Short-Trades platzieren logische Ziele `TakeProfitPoints` unter dem Einstieg und Stops `StopLossPoints` darüber.
- Ausstiege werden ausgelöst, wenn das Intrabar-Hoch/-Tief einer abgeschlossenen 5-Minuten-Kerze das jeweilige Ziel oder Stop-Niveau berührt.
- Während einer Position ignoriert die Strategie entgegengesetzte Signale; sie wartet, bis der Trade durch Stop oder Take-Profit geschlossen wird, bevor sie wieder reagiert, was der ursprünglichen MQL-Logik entspricht.

## Parameter
| Name | Standard | Beschreibung |
| --- | --- | --- |
| `OrderVolume` | 0.1 | Positionsgröße in Lots (spiegelt den `Lots`-Input der MQL-Version wider). |
| `StopLossPoints` | 200 | Abstand zum Schutz-Stop in Preispunkten. |
| `TakeProfitPoints` | 400 | Abstand zum Gewinnziel in Preispunkten. |
| `MaxSpreadPoints` | 20 | Maximaler erlaubter Spread in Preispunkten, bevor Einstiege übersprungen werden. |
| `FastPeriod` | 12 | Schnelle EMA-Länge innerhalb jeder MACD-Instanz. |
| `SlowPeriod` | 26 | Langsame EMA-Länge innerhalb jeder MACD-Instanz. |
| `SignalPeriod` | 9 | Signal-EMA-Länge innerhalb jeder MACD-Instanz. |
| `FiveMinuteCandleType` | 5-Minuten-Kerzen | Primärer Ausführungszeitrahmen. |
| `FifteenMinuteCandleType` | 15-Minuten-Kerzen | Erster Bestätigungszeitrahmen. |
| `HourCandleType` | 1-Stunden-Kerzen | Zweiter Bestätigungszeitrahmen. |
| `FourHourCandleType` | 4-Stunden-Kerzen | Dritter Bestätigungszeitrahmen. |

## Implementierungshinweise
- Verwendet `BindEx`, um stark typisierte MACD-Werte ohne Aufruf von `GetValue` zu lesen, gemäß den Projektrichtlinien.
- Ein gemeinsamer Helfer konvertiert die MACD-/Signalbeziehung in `{-1, 0, 1}`-Flags, um Bestätigungsprüfungen zu vereinfachen.
- Die Spread-Validierung teilt das beste Briefangebot minus bestes Geldangebot durch `Security.PriceStep`, damit der Schwellenwert dem MetaTrader-"Punkte"-Verhalten entspricht.
- Trade-Ereignisse werden mit `LogInfo` protokolliert, um das Debugging beim Testen in Designer oder Runner zu unterstützen.
- Keine Python-Übersetzung ist gemäß den Aufgabenanforderungen vorgesehen; nur die C#-Version ist enthalten.
