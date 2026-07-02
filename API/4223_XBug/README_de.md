# X-Bug-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **X-Bug-Strategie** ist ein gleitendes Durchschnitts-Crossover-System, das vom gleichnamigen Expert Advisor MQL4 übernommen wurde. Es vergleicht zwei einfache gleitende Durchschnitte, die auf der Grundlage des mittleren Kerzenpreises berechnet wurden. Wenn der schnelle Durchschnitt den langsamen Durchschnitt über- oder unterschreitet, eröffnet die Strategie eine Position in Richtung des Crossovers. Die Implementierung reproduziert die ursprünglichen Expert Advisor-Funktionen, einschließlich optionaler Signalumkehr, automatischer Positionsschließung bei entgegengesetzten Signalen und Pip-basierten Schutzaufträgen.

## Handelslogik
1. Abonnieren Sie den konfigurierten Kerzentyp (standardmäßig Ein-Minuten-Kerzen) und berechnen Sie zwei einfache gleitende Durchschnitte: eine schnelle Linie und eine langsame Linie. Die Durchschnittswerte verwenden den Medianpreis und respektieren die konfigurierten Indikatorverschiebungen.
2. Erkennen Sie einen bullischen Crossover, wenn der aktuelle schnelle Wert über dem langsamen Wert liegt, während der schnelle Wert zwei Balken zuvor unter dem langsamen Wert lag. Erkennen Sie einen bearischen Crossover anhand der gegenteiligen Bedingung.
3. Invertieren Sie optional das Crossover-Signal, wenn **ReverseSignals** aktiviert ist, um in die entgegengesetzte Richtung zu handeln.
4. Wenn **CloseOnSignal** aktiviert ist, schließen Sie sofort jede gegnerische Position, bevor Sie bei dem neuen Signal eine neue eingeben.
5. Gehen Sie Long-Positionen bei bullischen Signalen und Short-Positionen bei bärischen Signalen ein. Die Strategie vermeidet das Stapeln von Positionen in die gleiche Richtung; Es wird nur gehandelt, wenn die aktuelle Position flach ist oder mit dem Signal übereinstimmt.

## Risikomanagement
- **StopLossPips** – legt einen absoluten Schutzstopp in Pips fest. Der Stop wird in ganzen Pips ausgedrückt; Bruchpreise (5- oder 3-stellige Kurse) werden automatisch durch Umrechnung des Pip-Werts mithilfe der Wertpapierpreisstufe gehandhabt.
- **TakeProfitPips** – konfiguriert die Gewinnzieldistanz in Pips.
- **TrailingStopPips** – wenn **UseTrailingStop** aktiviert ist, wird ein Trailing Stop aktiviert, der bei der konfigurierten Pip-Distanz beginnt, sobald die Position in den Gewinnbereich übergeht. Der nachgestellte Schritt entspricht der nachgestellten Distanz und repliziert die ursprüngliche MetaTrader-Logik.
- Alle Schutzanordnungen werden über `StartProtection` mit Marktaustritten verwaltet, um die Parität mit dem MQL4-Experten aufrechtzuerhalten.

## Parameter
| Parameter | Beschreibung | Standard |
|-----------|-------------|---------|
| `OrderVolume` | Basishandelsvolumen, das für Markteintritte verwendet wird. | `0.1` |
| `StopLossPips` | Stop-Loss-Distanz, gemessen in Pips; zum Deaktivieren auf `0` setzen. | `70` |
| `TakeProfitPips` | Take-Profit-Distanz, gemessen in Pips; zum Deaktivieren auf `0` setzen. | `5000` |
| `UseTrailingStop` | Aktiviert oder deaktiviert die Trailing-Stop-Verwaltung. | `true` |
| `TrailingStopPips` | Nachlaufdistanz in Pips. | `90` |
| `FastPeriod` | Periode des schnellen gleitenden Durchschnitts. | `1` |
| `FastShift` | Balken zur Verschiebung des sich schnell bewegenden Durchschnitts vor der Auswertung von Signalen. | `0` |
| `SlowPeriod` | Periode des langsamen gleitenden Durchschnitts. | `14` |
| `SlowShift` | Balken zur Verschiebung des langsam gleitenden Durchschnitts vor der Auswertung von Signalen. | `10` |
| `CloseOnSignal` | Schließen Sie eine gegnerische Position sofort, wenn ein neues Signal erscheint. | `true` |
| `ReverseSignals` | Kehren Sie die Signalrichtung um, um entgegen der Kreuzung zu handeln. | `false` |
| `AppliedPrice` | Den gleitenden Durchschnitten zugeführte Kerzenpreisquelle. | `Median` |
| `CandleType` | Kerzendatentyp zur Signalgenerierung. | `1 minute` Zeitrahmen |

## Notizen
- Die Pip-Umrechnung multipliziert den Preisschritt mit 10 für Symbole, die mit 5 oder 3 Dezimalstellen angegeben werden, was dem ursprünglichen Verhalten des Expert Advisors entspricht.
- Es wird kein Python-Port bereitgestellt. In diesem Verzeichnis ist nur die C#-Strategie enthalten.
- Trailing Stops, Stopps und Ziele sind optional. Setzen Sie die entsprechenden Pip-Werte auf Null, um sie zu deaktivieren.
