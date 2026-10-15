# Lbs V12-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die Lbs V12-Strategie ist eine Konvertierung des MetaTrader Expert Advisors **LBS_V12.mq4**. Es öffnet ein Paar Breakout-Stop-Orders rund um die vorherige 15-Minuten-Kerze, wenn die konfigurierte Triggerstunde beginnt. Beide Orders werden durch den aktuellen Average True Range (ATR)-Wert ausgeglichen, um kurzfristige Volatilität zu berücksichtigen. Die Strategie versucht, den ersten Impuls der Handelssitzung zu erfassen und verwaltet Ausstiege durch virtuelle Stop-Loss-, Take-Profit- und Trailing-Regeln, die für jede abgeschlossene Kerze ausgewertet werden.

## Handelslogik
1. Die Strategie überwacht fertige Kerzen des ausgewählten Zeitrahmens (standardmäßig 15 Minuten).
2. Wenn eine neue Kerze mit der Minute `00` am konfigurierten `TriggerHour` erscheint, wird die vorherige Kerze zum Referenzbereich.
3. Wenn für den aktuellen Tag keine offenen Positionen und keine funktionierenden Orders vorhanden sind, werden zwei Stop-Orders gesendet:
   - **Kaufstopp** über dem Referenzhoch plus dem Instrumenten-Spread, einem Preisschritt und dem letzten ATR-Wert.
   - **Verkaufsstopp** unterhalb des Referenztiefs abzüglich der gleichen Puffer.
4. Schutzpreisniveaus für jede Seite werden intern gespeichert:
   - Der Stop-Loss wird jenseits des entgegengesetzten Extrems der Referenzkerze platziert.
   - Der Take-Profit wird anhand der Punktentfernung im MetaTrader-Stil berechnet.
   - Ein Trailing Stop wird aktiviert, sobald sich der Handel weiter als die konfigurierte Distanz bewegt.
5. Wenn eine Long- oder Short-Position eröffnet wird, wird die entgegengesetzte Stop-Order aufgehoben. Der gesamte Schutz wird virtuell angewendet: Kerzenhochs und -tiefs werden mit den gespeicherten Stop/Take-Werten verglichen und die Position wird mit Marktaufträgen geschlossen, wenn die Limits erreicht sind.
6. Die Strategie wird nur einmal pro Tag ausgeführt. Alle ausstehenden Aufträge und der interne Status werden zu Beginn eines neuen Handelsdatums gelöscht.

## Parameter
| Name | Beschreibung | Standard |
|------|-------------|---------|
| `Volume` | Handelsvolumen in Lots. | `1` |
| `TriggerHour` | Stunde des Tages (Terminalzeitzone), zu der die Breakout-Orders gesendet werden sollen. | `9` |
| `TakeProfitPoints` | MetaTrader-ähnliche Punkte zwischen dem Einstiegspreis und dem Take-Profit-Ziel. | `100` |
| `TrailingStopPoints` | Punkte im MetaTrader-Stil, die für den Trailing Stop verwendet werden, nachdem der Trade in die Gewinnzone übergeht. | `20` |
| `AtrPeriod` | Zeitraum des Indikators ATR, der die ausstehenden Aufträge ausgleicht. | `3` |
| `CandleType` | Kerzentyp, der für Signalberechnungen verwendet wird. Der Standardwert sind Kerzen mit einem Zeitrahmen von 15 Minuten. | `15m timeframe` |

## Risikomanagement
- Ausstiege werden durch Marktaufträge ausgeführt, wenn die Kerzenextreme die virtuellen Stop-Loss- oder Take-Profit-Niveaus berühren.
- Der Trailing Stop erhöht (für Long-Positionen) oder verringert (für Short-Positionen) das Schutzniveau, wenn der Trade mehr als die konfigurierte Distanz gewinnt.
- Durch das tägliche Zurücksetzen wird sichergestellt, dass die Strategie nicht mehrere Positionen oder veraltete ausstehende Aufträge ansammelt.

## Notizen
- Genaue Bid/Ask-Aktualisierungen verbessern die Spread-Kompensation, die zu den Breakout-Preisen hinzugefügt wird. Wenn keine Spread-Daten verfügbar sind, fällt die Strategie auf einen Preisschritt zurück.
- Die Konvertierung behält die ursprünglichen MetaTrader-Standardwerte bei, passt jedoch die Take-Profit-Behandlung für Short-Positionen an, sodass das Ziel immer in die profitable Richtung gelegt wird.
