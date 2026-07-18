# MA Crossover Multi-Zeitrahmen-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie reproduziert die Idee des ursprünglichen **MA Crossover** Expert Advisors für MetaTrader 4. Sie vergleicht zwei gleitende Durchschnitte, die aus verschiedenen Zeitrahmen stammen können. Ein bullischer Crossover (schneller MA über langsamem MA) öffnet eine Long-Position, während ein bärischer Crossover eine Short-Position öffnet. Optionale Filter steuern die erlaubte Handelsrichtung, den aktiven Handelszeitplan und einen Equity-Schutz. Interne Stop-Loss-, Take-Profit- und Trailing-Logik emulieren die „versteckten" Ausstiege aus der MQL-Version.

## Handelslogik

1. Abonnieren Sie zwei Kerzenstreams (aktuelle und vorherige Zeitrahmen) und berechnen Sie den ausgewählten Typ gleitender Durchschnitte.
2. Wenden Sie die konfigurierten Balkenverschiebungen auf die gleitenden Durchschnittswerte an, bevor Sie sie vergleichen.
3. Ignorieren Sie unfertige Kerzen und warten Sie, bis beide gleitenden Durchschnitte gebildet sind.
4. Überspringen Sie den Handel außerhalb des konfigurierten Tag/Uhrzeit-Fensters oder wenn der Equity-Schutz ausgelöst wird.
5. Bei einem bullischen Crossover:
   - Optionales Schließen einer Short-Position, wenn `ClosePositionsOnCross = true`.
   - Öffnen einer Long-Position, wenn Long-Handel erlaubt ist.
6. Bei einem bärischen Crossover:
   - Optionales Schließen einer Long-Position, wenn `ClosePositionsOnCross = true`.
   - Öffnen einer Short-Position, wenn Short-Handel erlaubt ist.
7. Verwalten Sie die offene Position mit Stop-Loss-, Take-Profit- und Trailing-Regeln, die als Prozentsätze des Einstiegspreises ausgedrückt werden.

## Parameter

| Parameter | Beschreibung |
|-----------|--------------|
| `AllowedDirection` | Handelsrichtungsfilter (`LongOnly`, `ShortOnly`, `LongAndShort`). |
| `ClosePositionsOnCross` | Schließen der entgegengesetzten Position bei einem Crossover, bevor ein neuer Trade eröffnet wird. |
| `MaType` | Berechnungstyp des gleitenden Durchschnitts (`Simple`, `Exponential`, `Smoothed`, `Weighted`). |
| `CurrentMaPeriod` | Periode für den schnellen gleitenden Durchschnitt. |
| `PreviousPeriodAddition` | Zusätzliche Länge für den langsamen gleitenden Durchschnitt (`PreviousMaPeriod = CurrentMaPeriod + addition`). |
| `CurrentShift` / `PreviousShift` | Anzahl der abgeschlossenen Balken, die zum Rückverschieben der gleitenden Durchschnittswerte verwendet werden. |
| `CurrentCandleType` / `PreviousCandleType` | Kerzendaten für schnelle und langsame gleitende Durchschnitte. |
| `StopLossPercent` | Stop-Loss-Abstand in Prozent des Einstiegspreises (versteckter Ausstieg). |
| `TrailingStopPercent` | Trailing-Stop-Abstand in Prozent basierend auf dem besten erreichten Preis. |
| `TakeProfitPercent` | Take-Profit-Abstand in Prozent des Einstiegspreises (versteckter Ausstieg). |
| `StartDay` / `EndDay` | Wochentag-Filter für Handelsaktivität. |
| `StartTime` / `EndTime` | Intratägiges Zeitfenster für das Öffnen neuer Trades. |
| `ClosePositionsOnMinEquity` | Alle Positionen schließen, wenn der Equity-Schutz ausgelöst wird. |
| `MinimumEquityPercent` | Mindestprozentsatz des anfänglichen Portfoliowerts, der vom Equity-Schutz erlaubt wird. |

## Risikomanagement

- Die Strategie berechnet Stop-Loss-, Take-Profit- und Trailing-Niveaus intern und verlässt über Marktorders, was die versteckte Schutzlogik des MQL-Skripts imitiert.
- `MinimumEquityPercent` speichert den anfänglichen Portfoliowert beim Start und kann eine erzwungene Glättung auslösen, wenn das Equity unter den Schwellenwert fällt.
- Die Positionsgröße wird durch die Basis-`Strategy.Volume`-Eigenschaft gesteuert. Das Standardvolumen ist auf `1` gesetzt.

## Verwendungshinweise

- Die Strategie erfordert Kerzendaten für beide konfigurierten Zeitrahmen. Stellen Sie sicher, dass die zugehörigen Konnektoren die angeforderten Zeitrahmen unterstützen.
- Wenn beide gleitenden Durchschnitte denselben Zeitrahmen verwenden, abonniert die Strategie dennoch zwei Streams, um die Logik symmetrisch zu halten.
- Da Stop- und Take-Profit-Ausstiege über Marktorders ausgeführt werden, verbleiben keine Schutzorders im Orderbuch.
- Die Parameter entsprechen den Haupteingaben des ursprünglichen MQL Expert Advisors, während Risiko/Margin-Management-Funktionen, die von brokerspezifischen Funktionen abhängen (Hedging, Averaging), bewusst weggelassen werden.

## Unterschiede zur MQL-Version

- Averaging-Funktionen (`Average_Up`, `Average_Down`) und Hedging-Einstellungen sind nicht implementiert, um die Logik mit der StockSharp High-Level-API kompatibel zu halten.
- Der Equity-Schutz verwendet den Portfoliowert aus StockSharp anstelle von Free-Margin-spezifischen Berechnungen.
- Risikoausstiege werden durch Marktorders bei Kerzenabschlussereignissen ausgeführt und sind daher immer vor dem Orderbuch verborgen.
