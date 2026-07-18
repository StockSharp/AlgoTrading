# Lavika100-Strategie (StockSharp)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Lavika100-Strategie** ist eine getreue Portierung des MetaTrader-5 Expert Advisors "Lavika  cent". Das System kombiniert einen einstündigen (H1) und einen vierstündigen (H4) RAVI-Momentum-Filter, um zu entscheiden, wann Trades eröffnet werden. Es behält die ursprünglichen Money-Management-Optionen (fester Lot oder Risikoprozentsatz), Ein-Positions-Disziplin, optionale Signalumkehr und automatische Stop-Verwaltung bei. Die StockSharp-Version hält sich an die High-Level-API-Richtlinien: Kerzenabonnements treiben den Ablauf, Indikatoren werden über Binder angesprochen und Schutzorders werden mit `StartProtection` konfiguriert.

## Ablauf
1. **Datenabonnements** - die Strategie abonniert H1-Kerzen für den Ausführungszeitrahmen und H4-Kerzen für den Trendfilter. Der `SimpleMovingAverage`-Indikator wird auf die Eröffnungspreise angewendet, um die MT5-Aufrufe `iMA(..., PRICE_OPEN)` nachzubilden.
2. **RAVI-Momentum** - zwei gleitende Durchschnitte auf jedem Zeitrahmen (schnell/langsam) erzeugen einen "RAVI"-Prozentsatz: `(fast - slow) / slow * 100`. Der H1-Wert muss positiv sein, bevor ein Trade in Betracht gezogen wird.
3. **Erkennung des Trendmusters** - die vier jüngsten RAVI-Werte auf H4 werden geprüft:
   - Eine steigende Sequenz (`r0 > r1`, `r1 < r2`, `r2 < r3`) löst ein Long-Signal aus.
   - Eine fallende Sequenz (`r0 < r1`, `r1 > r2`, `r2 > r3`) löst ein Short-Signal aus. Dies spiegelt das Verhalten des ursprünglichen Codes, obwohl der Expert Advisor die Richtung nur über das `Reverse`-Flag wechselte.
4. **Signalumkehr und Glattstellung** - je nach den Parametern `ReverseSignals` und `CloseOpposite` eröffnet der Algorithmus in der erkannten Richtung oder kehrt sie um und schließt zuvor jede Gegenposition.
5. **Money Management** - das Volumen stammt aus `FixedVolume` oder wird über die Methode `RiskPercent` nach Risiko skaliert (Portfoliowert * Prozentsatz / Stop-Distanz).
6. **Schutz** - Stop-Loss, Take-Profit, Trailing Stop und Trailing-Schritt werden über `StartProtection` aktiviert, sobald die Strategie startet und die Parameter ungleich null sind.

## Handelsregeln
- **Long-Einstieg** - H1 RAVI ist positiv und die H4-Reihe zeigt ein steigendes Muster. Die Strategie schließt vor dem Kauf eine bestehende Short-Position, wenn `CloseOpposite=true` ist.
- **Short-Einstieg** - H1 RAVI ist positiv und die H4-Reihe zeigt ein fallendes Muster. Wenn `ReverseSignals=true` ist, werden die Richtungen getauscht, passend zum MT5-Schalter "Reverse".
- **Einzelposition** - mit `OnlyOnePosition=true` blockiert jede nicht flache Exposure zusätzliche Einstiege, bis die Position geschlossen ist.
- **Volumenbestimmung** - der Risikoprozentsatzmodus verwendet das `PriceStep`/`StepPrice`-Paar des Instruments, um die Preisdistanz in einen Geldwert umzuwandeln, und berücksichtigt `VolumeStep`, `VolumeMin` und `VolumeMax`.

## Parameter
| Name | Beschreibung |
| --- | --- |
| `H1CandleType` | Zeitrahmen für die Ausführungslogik (Standard 1 Stunde). |
| `H4CandleType` | Höherer Zeitrahmen für den Trendfilter (Standard 4 Stunden). |
| `H1FastPeriod` / `H1SlowPeriod` | Längen der gleitenden Durchschnitte für H1 RAVI. |
| `H4FastPeriod` / `H4SlowPeriod` | Längen der gleitenden Durchschnitte für H4 RAVI. |
| `StopLossPoints` | Stop-Loss-Distanz in pipbasierten Punkten. |
| `TakeProfitPoints` | Take-Profit-Distanz in pipbasierten Punkten. |
| `TrailingStopPoints` | Trailing-Stop-Distanz. Auf null setzen, um Trailing zu deaktivieren. |
| `TrailingStepPoints` | Mindestschritt für Trailing-Aktualisierungen. Muss positiv sein, wenn Trailing aktiviert ist. |
| `FixedVolume` | Lotgröße im festen Modus. |
| `RiskPercent` | Prozentsatz des Portfoliowerts, der riskiert wird, wenn `MoneyMode` gleich `RiskPercent` ist. |
| `MoneyMode` | Schaltet zwischen `FixedLot` und `RiskPercent` um. |
| `OnlyOnePosition` | Erlaubt nur eine einzelne offene Position. |
| `ReverseSignals` | Kehrt Long-/Short-Aktionen um (Standard true, um zur EA-Einstellung zu passen). |
| `CloseOpposite` | Schließt eine Gegenposition, bevor eine neue Order platziert wird. |

## Hinweise zur Umrechnung
- Die Pip-Umrechnung imitiert den MT5-Expert: Drei- und fünfstellige Kurse multiplizieren `PriceStep` mit zehn, um eine pipgroße Schrittweite zu erhalten.
- Die RAVI-Historie wird ohne benutzerdefinierte Collections gespeichert - nur vier nullable Felder - und respektiert damit die Repository-Beschränkungen gegen manuelle Buffer.
- Money Management vermeidet Indikator-`GetValue`-Aufrufe und nutzt StockSharp-Marktdaten, um prozentuales Risiko auf Volumen abzubilden.
- `StartProtection` wird nur aufgerufen, wenn mindestens eine der Schutzdistanzen positiv ist, was eine sichere Ausführung in Backtests und im Live-Handel gewährleistet.

## Nutzungstipps
- Stellen Sie ein Forex-ähnliches Instrument mit korrekt konfiguriertem `PriceStep`, `StepPrice`, `VolumeStep`, `VolumeMin` und `VolumeMax` bereit.
- Definieren Sie bei risikobasierter Größenbestimmung einen von null verschiedenen `StopLossPoints`; andernfalls ist das berechnete Volumen null.
- Da der ursprüngliche EA eine logische Eigenheit enthielt, bei der beide Muster das Kauf-Flag setzten, behalten Sie `ReverseSignals=true` bei, wenn Sie seine exakten Trades reproduzieren müssen.
