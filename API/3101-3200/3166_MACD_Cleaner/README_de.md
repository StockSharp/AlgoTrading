# MACD Cleaner-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **MACD Cleaner**-Strategie ist eine Konvertierung des MetaTrader-5-Expert-Advisors „MACD Cleaner". Sie analysiert abgeschlossene Kerzen eines einzelnen Zeitrahmens und platziert Trades, wenn die MACD-Hauptlinie über drei aufeinanderfolgende geschlossene Bars monoton steigt oder fällt. Das System hält immer höchstens eine direktionale Position und dreht um, wenn das Momentum umkehrt.

## Handelslogik
- Bei jeder abgeschlossenen Kerze liest die Strategie die mit den konfigurierten Schnell-, Langsam- und Signalperioden berechnete MACD-Linie.
- Wenn die letzten drei MACD-Werte nicht-abnehmend sind, bereitet die Strategie einen Long-Einstieg vor. Falls eine Short-Position vorhanden ist, wird diese zuerst geschlossen, dann eine neue Long-Position eröffnet.
- Wenn die letzten drei MACD-Werte nicht-zunehmend sind, bereitet die Strategie einen Short-Einstieg vor. Vorhandene Long-Positionen werden geglättet, bevor der Short eröffnet wird.
- Schützende Stop-Loss- und Take-Profit-Niveaus werden auf Kerzenhochs und -tiefs mit pip-basierten Offsets ausgewertet.
- Wenn Trailing-Parameter aktiviert sind, wird der Stop in Handelsrichtung gezogen, sobald der Preis um mindestens den konfigurierten Trailing-Schritt voranschreitet.
- Alle Ausstiegsorders werden als Marktorders unter Verwendung des aggregierten Positionsvolumens ausgegeben, um sicherzustellen, dass die gesamte Position geschlossen wird.

## Parameter
| Name | Standard | Beschreibung |
|------|---------|-------------|
| `CandleType` | 1-Stunden-Kerzen | Zeitrahmen für MACD-Berechnung und Orderauswertung. |
| `TradeVolume` | 1 | Basisvolumen für eine neue Position. Wenn die Gegenseite offen ist, wird das absolute Positionsvolumen zum Schließen hinzugefügt, bevor umgekehrt wird. |
| `StopLossPips` | 35 | Stop-Loss-Abstand in Pips vom Einstiegspreis. Auf null setzen zum Deaktivieren. |
| `TakeProfitPips` | 30 | Take-Profit-Abstand in Pips vom Einstiegspreis. Auf null setzen zum Deaktivieren. |
| `TrailingStopPips` | 0 | Trailing-Stop-Abstand. Bei null ist die Trailing-Logik deaktiviert. |
| `TrailingStepPips` | 5 | Mindest-Favoritenverschiebung (in Pips) bevor der Trailing-Stop angepasst wird. Ignoriert, wenn der Trailing-Stop deaktiviert ist. |
| `MacdFastPeriod` | 15 | Schnelle EMA-Länge für den MACD-Indikator. |
| `MacdSlowPeriod` | 33 | Langsame EMA-Länge für den MACD-Indikator. |
| `MacdSignalPeriod` | 11 | Signal-EMA-Länge für den MACD-Indikator. |

## Orderverwaltung
- Long-Ausstiege: die Strategie gibt eine Marktverkaufsorder aus, wenn Stop-Loss, Take-Profit oder Trailing-Niveau getroffen wird.
- Short-Ausstiege: eine Marktkauforder schließt die Position unter denselben Bedingungen, gespiegelt für Short-Trades.
- Nachdem die Position vollständig geschlossen ist, wird der Trailing-Status zurückgesetzt, damit der nächste Trade mit frischen Niveaus beginnt.

## Hinweise
- Die Pip-Größe wird automatisch vom Instrument abgeleitet. Für Symbole mit 3 oder 5 Dezimalstellen entspricht der Pip zehn minimalen Preisschritten, entsprechend der ursprünglichen MetaTrader-Implementierung.
- Die Logik wertet nur abgeschlossene Kerzen aus und reagiert nicht auf Intrabar-Änderungen.
- Zum Deaktivieren des Risikomanagements die entsprechenden Pip-Abstände auf null setzen. Trailing erfordert sowohl `TrailingStopPips` als auch ein positives `TrailingStepPips`.
