# News-Release-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie reproduziert das Kernverhalten des ursprünglichen **NewsReleaseEA**-Expert-Advisors, indem sie ein Bracket aus Pending Orders um eine geplante Nachrichtenveröffentlichung vorbereitet und die resultierende Position aktiv verwaltet.

## Schlüsselideen

- Fünf Eingaben (Nachrichtenzeit, Vor-/Nachlauf-Fenster, Orderdistanzen und Abstand) definieren, wann und wo die Stop-Orders platziert werden.
- Ein symmetrischer Satz aus Buy-Stop- und Sell-Stop-Orders wird kurz vor der konfigurierten Nachrichtenzeit gesendet. Das erste Paar wird `DistancePips` vom aktuellen Ask/Bid entfernt platziert, zusätzliche Paare werden um `StepPips` versetzt.
- Pending Orders bleiben bis `PostNewsMinutes` Minuten nach dem Ereignis aktiv. Am Ende des Fensters storniert die Strategie jede aktive Order und schließt auf Wunsch jede offene Position.
- Wird eine Order gefüllt, werden die entgegengesetzten Pending Orders automatisch storniert und die offene Position über Stop-Loss-, Take-Profit-, Break-even- und Trailing-Regeln in Pips verwaltet.
- Break-even-Schutz wird scharf, nachdem sich der Preis `BreakEvenTriggerPips` zugunsten der Position bewegt hat, und erzwingt einen Ausstieg, wenn der Preis zum Einstiegspreis plus `BreakEvenOffsetPips` (Longs) oder minus diesem Offset (Shorts) zurückkehrt.
- Trailing-Verwaltung verfolgt den besten nach Einstieg erreichten Preis. Sobald die Distanz zwischen aktuellem Preis und Extrem `TrailingPips` überschreitet, wird die Position geschlossen, um aufgelaufenen Gewinn zu schützen.
- Das Flag `TradeOnce` spiegelt das "trade one time per news"-Verhalten des MQL-Programms, indem es eine zweite Aktivierung nach Abschluss des ersten Trades verhindert.

## Parameter

- `NewsTime`: geplante Zeit der Nachrichtenveröffentlichung.
- `PreNewsMinutes`: wie viele Minuten vor der Veröffentlichung Pending Orders platziert werden.
- `PostNewsMinutes`: wie viele Minuten nach der Veröffentlichung Pending Orders vor Stornierung aktiv bleiben.
- `OrderPairs`: Anzahl der Buy-Stop-/Sell-Stop-Paare, die das Bracket bilden.
- `DistancePips`: Distanz in Pips des ersten Paars vom aktuellen besten Ask/Bid im Platzierungszeitpunkt.
- `StepPips`: zusätzlicher Abstand in Pips zwischen aufeinanderfolgenden Paaren.
- `OrderVolume`: Volumen jeder Pending Order.
- `TradeOnce`: wenn aktiviert, kann die Strategie nur einmal pro Ereignisfenster handeln.
- `UseStopLoss` / `StopLossPips`: aktiviert und konfiguriert Stop-Loss-Distanz in Pips.
- `UseTakeProfit` / `TakeProfitPips`: aktiviert und konfiguriert Take-Profit-Distanz in Pips.
- `UseBreakEven`, `BreakEvenTriggerPips`, `BreakEvenOffsetPips`: konfiguriert das Break-even-Modul.
- `UseTrailing` / `TrailingPips`: aktiviert Trailing-Ausstiegslogik und definiert die Trailing-Distanz in Pips.
- `CloseAfterEvent`: schließt jede offene Position, wenn das Nach-Nachrichten-Fenster endet.

## Hinweise

- Die Strategie arbeitet ausschließlich mit Level1-Daten (`SubscribeLevel1`), damit sie auf die neuesten Bid/Ask-Preise reagieren kann, ohne auf Kerzen zu warten.
- In Pips angegebene Preisdistanzen werden über `PriceStep` des Instruments in absolute Preise konvertiert. Ist `PriceStep` nicht verfügbar, wird 1 als sicherer Fallback verwendet.
- Stop-Loss-, Take-Profit-, Break-even- und Trailing-Bedingungen schließen die Position zum Markt über `ClosePosition()`. Dies spiegelt die reaktive Verwaltung des ursprünglichen Experten wider und hält die Implementierung kompakt.
- Wie gewünscht wird keine Python-Version bereitgestellt.
