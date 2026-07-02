# LCS MACD Händlerstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist eine StockSharp-Portierung des Expertenberaters „LCS-MACD-Trader“ MetaTrader 4. Es handelt mit MACD-Überkreuzungen, die unterhalb/über der Nulllinie auftreten, und erfordert optional eine Bestätigung vom Stochastic-Oszillator. Die Logik spiegelt auch die ursprünglichen Tageszeitfilter und die Trailing-Stop/Break-Even-Verwaltung im MetaTrader-Stil wider.

## Wie es funktioniert

- Long-Einträge werden ausgelöst, wenn die MACD-Linie ihre Signallinie überschreitet, während beide unter Null bleiben. Wenn der stochastische Filter aktiviert ist, muss die %D-Linie innerhalb des angegebenen Lookbacks über %K gelegen haben und die aktuelle Kerze muss anzeigen, dass %D wieder unter %K fällt.
- Short-Einträge werden ausgelöst, wenn die MACD-Linie ihre Signallinie unterschreitet, während beide über Null bleiben. Bei aktiviertem stochastischen Filter muss die %D-Linie kürzlich unter %K gelegen haben und steigt nun wieder darüber an.
- Der Handel ist nur innerhalb von drei konfigurierbaren Intraday-Fenstern zulässig, die die EA-Einstellungen replizieren.
- Take-Profit-, Stop-Loss-, Break-Even- und Trailing-Stop-Abstände werden in Pips ausgedrückt und anhand der Punktgröße des Instruments umgerechnet.
- Es wird nur eine Nettoposition pro Richtung gepflegt (StockSharp Netting). Positionsstapelung ist bis zu `MaxOrders` Losen zulässig; Gegensignale warten, bis die aktuelle Nettoposition durch das Risikomanagement geschlossen wird.

## Parameter

| Name | Beschreibung | Standard |
| --- | --- | --- |
| `CandleType` | Für Indikatorberechnungen verwendete Kerzenserien. | 15-minütiger Zeitrahmen |
| `FastEmaPeriod` | Schnelle EMA-Periode im MACD. | 12 |
| `SlowEmaPeriod` | Langsamer EMA Zeitraum im MACD. | 26 |
| `SignalPeriod` | Signalleitungsperiode im MACD. | 9 |
| `UseStochasticFilter` | Erfordern eine stochastische Bestätigung vor der Eingabe. | wahr |
| `BarsToCheckStochastic` | Maximal geschlossene Balken seit der entgegengesetzten stochastischen Beziehung. | 5 |
| `StochasticKPeriod` | Lookback-Länge von %K. | 5 |
| `StochasticDPeriod` | Glättungslänge von %D. | 3 |
| `StochasticSlowing` | Zusätzliche Glättung auf %K angewendet. | 3 |
| `TradeVolume` | Pro Eintrag verwendete Losgröße. | 0,1 |
| `TakeProfitPips` | Take-Profit-Distanz in Pips. | 100 |
| `StopLossPips` | Stop-Loss-Distanz in Pips. | 100 |
| `MaxOrders` | Maximale gestapelte Einträge pro Richtung. | 5 |
| `EnableTrailing` | Aktivieren Sie die Trailing-Stop-Logik im MetaTrader-Stil. | falsch |
| `TrailingActivationPips` | Vor Beginn des Trailings ist ein Gewinn erforderlich. | 50 |
| `TrailingDistancePips` | Vom Trailing Stop aufrechterhaltener Abstand. | 25 |
| `BreakEvenActivationPips` | Erforderlicher Gewinn, um den Stop auf die Gewinnschwelle zu bringen. | 25 |
| `BreakEvenOffsetPips` | Beim Platzieren des Break-Even-Stopps werden zusätzliche Pips hinzugefügt. | 1 |
| `Session1Start/End`, `Session2Start/End`, `Session3Start/End` | Intraday-Handelsfenster. | 08:15-08:35, 13:45-14:42, 22:15-22:45 |

## Notizen

- Die Strategie geht von einem Netting-Konto aus. Es schließt bestehende Positionen über die konfigurierten Risikoregeln, anstatt gegenläufige Aufträge wie die ursprüngliche MT4-Version abzusichern.
- Bei der Pip-Konvertierung wird die Punktgröße des Instruments verwendet. Bei 5-stelligen FX-Symbolen skaliert die Logik die Pip-Werte automatisch um 10, um der Multiplikatoreinstellung EA zu entsprechen.
- Die Trailing-Stop- und Break-Even-Logik wird an fertigen Kerzen ausgewertet und verwendet das Hoch/Tief jedes Balkens, um tickbasiertes MetaTrader-Verhalten zu emulieren.
