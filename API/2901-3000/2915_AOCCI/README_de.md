# AOCCI Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
- Konvertierung des MetaTrader 5 Expert Advisors `AOCCI` in die StockSharp High-Level-API.
- Kombiniert den Awesome Oscillator und den Commodity Channel Index mit einem einfachen Pivot-Level-Filter.
- Enthält Spread-Schutz durch "Big Jump"- und "Double Jump"-Filter zum Überspringen instabiler Preisbewegungen.
- Reproduziert die ursprüngliche MQL5-Logik, bei der das Short-Setup dieselben Bedingungen wie das Long-Setup verwendet.

## Daten und Indikatoren
- Verwendet den primären Zeitrahmen, der durch `CandleType` definiert wird, für die Signalgenerierung.
- Abonniert einen zusätzlichen höheren Zeitrahmen (`HigherCandleType`, Standard 1 Stunde), um den vorherigen Schlusskurs als Trendfilter zu lesen.
- Indikatoren:
  - `AwesomeOscillator` zur Erkennung der Impulsrichtung.
  - `CommodityChannelIndex` mit konfigurierbarer Periode und optionalem Signalshift.
- Berechnet ein Pivot-Level aus der Kerze bei `SignalCandleShift + 1` im Arbeitszeitrahmen: `(High + Low + Close) / 3`.

## Einstiegslogik
1. Warten, bis beide Indikatoren vollständig gebildet sind und mindestens sechs abgeschlossene Kerzen verfügbar sind.
2. CCI-Werte mit dem konfigurierten Shift sammeln (`SignalCandleShift` für den aktuellen Vergleich und `SignalCandleShift + 1` für den vorherigen Balken).
3. Den Balken ablehnen, wenn ein Sprungfilter ausgelöst wird:
   - `BigJumpPips` vergleicht aufeinanderfolgende Eröffnungspreise der letzten fünf Intervalle.
   - `DoubleJumpPips` vergleicht Eröffnungspreise, die durch einen Balken getrennt sind.
4. Long-Einstieg, wenn alle folgenden Bedingungen erfüllt sind und keine aktive Position vorhanden ist:
   - Awesome Oscillator ist am aktuellen Balken positiv.
   - Verschobener CCI-Wert ist größer oder gleich null.
   - Aktueller Schlusskurs liegt über dem Pivot-Level.
   - Mindestens eine Bestätigung ist bei den vorherigen Daten bärisch: vorheriger AO-Wert unter null, vorheriger verschobener CCI ≤ 0, oder letzter höherer Zeitrahmen-Schlusskurs unter dem Pivot.
5. Short-Einstieg verwendet exakt dasselbe Regelwerk wie der Long-Einstieg (der ursprüngliche Experte enthält identische Bedingungen für beide Richtungen).

## Ausstiegslogik und Risikomanagement
- Wenn ein Trade eröffnet wird, werden optionale Stop-Loss- und Take-Profit-Niveaus unter Verwendung der konfigurierten Pip-Abstände multipliziert mit der erkannten Pip-Größe des Instruments zugewiesen.
- Bei jeder abgeschlossenen Kerze prüft die Strategie auf Take-Profit- oder Stop-Loss-Treffer anhand der Kerzenextrema und schließt die Position zum Markt.
- Trailing Stop aktiviert, wenn sowohl `TrailingStopPips` als auch `TrailingStepPips` positiv sind:
  - Long-Trades verschieben den Stop auf `Close - TrailingStopPips`, sobald der Preis mindestens `TrailingStopPips + TrailingStepPips` vom Einstieg vorrückt.
  - Short-Trades verschieben den Stop auf `Close + TrailingStopPips`, sobald der Preis um dieselbe kombinierte Distanz fällt.
- Wenn eine Position geschlossen wird (durch Stop, Ziel oder Trailing), wartet die Strategie bis zur nächsten Kerze, um neue Einstiege zu bewerten.

## Parameter
| Parameter | Standard | Beschreibung |
|-----------|----------|--------------|
| `TradeVolume` | 1 | Basis-Ordervolumen für Markteinträge. |
| `StopLossPips` | 50 | Abstand in Pips für den Schutz-Stop. Auf 0 setzen zum Deaktivieren. |
| `TakeProfitPips` | 50 | Abstand in Pips für den Take-Profit. Auf 0 setzen zum Deaktivieren. |
| `TrailingStopPips` | 5 | Trailing-Stop-Abstand in Pips. Erfordert `TrailingStepPips` > 0. |
| `TrailingStepPips` | 5 | Zusätzlicher Puffer, bevor der Trailing Stop aktualisiert wird. |
| `CciPeriod` | 55 | Periode des Commodity Channel Index. |
| `SignalCandleShift` | 0 | Shift beim Lesen des CCI-Puffers und der Pivot-Kerze. |
| `BigJumpPips` | 100 | Maximal erlaubte Differenz (in Pips) zwischen aufeinanderfolgenden Eröffnungen der letzten Kerzen. |
| `DoubleJumpPips` | 100 | Maximal erlaubte Differenz (in Pips) zwischen jeder zweiten Kerzeneröffnung. |
| `CandleType` | 15-Minuten-Kerzen | Arbeitszeitrahmen für die primären Signale. |
| `HigherCandleType` | 1-Stunden-Kerzen | Höherer Zeitrahmen zum Abrufen des vorherigen Schlusskurses zur Bestätigung. |

## Hinweise
- Die Pip-Größe wird aus `Security.PriceStep` abgeleitet und für Instrumente mit 3 oder 5 Dezimalstellen angepasst.
- Da der ursprüngliche EA identische Filter für beide Richtungen verwendete, treten Short-Trades nur auf, wenn die Long-Bedingung auch erfüllt ist und die Strategie verkaufen darf. Short-Trades extern deaktivieren, wenn nicht gewünscht.
- Sprungfilter benötigen mindestens sechs abgeschlossene Kerzen, bevor der erste Trade bewertet wird.
