# ATR Step-Trader-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die ATR Step Trader-Strategie ist eine direkte Portierung des MetaTrader5-Expertenberaters `atrTrader.mq5`. Es kombiniert einen Filter für den schnellen/langsamen gleitenden Durchschnitt mit Breakout- und Pyramiding-Regeln, die auf dem Average True Range (ATR) basieren. Der Port behält den balkengesteuerten Workflow des ursprünglichen EA bei: Es werden nur abgeschlossene Kerzen verarbeitet, der schnelle SMA muss für eine feste Anzahl von Balken über oder unter dem langsamen SMA liegen und jede Entscheidung ist an ATR-Vielfache verankert, um die Abstände zwischen den Märkten zu normalisieren.

## Indikatoren und Daten
- **Einfache gleitende Durchschnitte (SMA).** Zwei gleitende Durchschnitte (`FastPeriod` und `SlowPeriod`) definieren den primären Trendfilter. Beides wird auf die Abo-Kerzenserie angewendet.
- **Average True Range (ATR).** Ein `AverageTrueRange`-Indikator (`AtrPeriod`) wandelt die Volatilität in Preisabstände um. Jede Breakout-, Add-on- und Stop-Berechnung verwendet ATR Vielfache.
- **Höchst-/Tiefstpreiskanäle.** Die Indikatoren `Highest` und `Lowest` verfolgen das extreme Hoch und Tief der letzten `MomentumPeriod`-Kerzen. Sie ersetzen die `iHighest`/`iLowest`-Aufrufe aus dem MQL-Code.
- **Zeitrahmen.** Der Standardkerzentyp ist eine Stunde (`TimeSpan.FromHours(1)`) und spiegelt das `PERIOD_CURRENT`-Verhalten des ursprünglichen Skripts wider. Sie können zu einem anderen Zeitrahmen wechseln, indem Sie den Parameter `CandleType` bearbeiten.

## Eingabelogik
1. Warten Sie, bis die Kerze fertig ist. Unvollendete Kerzen werden ignoriert, um mit dem MT5 OnTick + iTime Guard synchron zu bleiben.
2. Aktualisieren Sie die Streak-Zähler für den gleitenden Durchschnitt. Ein Aufwärtstrend nimmt zu, wenn der schnelle SMA über dem langsamen SMA liegt; Ein Abwärtstrend nimmt zu, wenn er unten angezeigt wird. Gemischte Messwerte setzen den gegenteiligen Trend zurück.
3. Sobald der Aufwärtstrend `MomentumPeriod` erreicht, prüfen Sie, ob der Schlusskurs immer noch um mindestens `StepMultiplier * ATR` unter dem jüngsten Hoch liegt. Wenn ja, kaufen Sie auf dem Markt.
4. Sobald der Abwärtstrend `MomentumPeriod` erreicht, prüfen Sie, ob der Schlusskurs immer noch mindestens `StepMultiplier * ATR` über dem jüngsten Tief liegt. Wenn ja, verkaufen Sie es auf dem Markt.
5. Jede neue Position initialisiert den Richtungszustand: Die Strategie merkt sich die höchsten und niedrigsten gefüllten Preise pro Seite, sodass spätere Pyramiden Referenzanker haben. Mit der ersten Order ist auch ein Stop in Volatilitätsgröße (`StepMultiplier * StopMultiplier * ATR`) verbunden.

## Positionsmanagement
- **Pyramidenbildung:** Während die Anzahl der aktiven Einträge unter `PyramidLimit` liegt, fügt die Strategie immer dann eine weitere Einheit hinzu, wenn sich der Preis um entweder `+/- StepsMultiplier * ATR` von der aktuellen Extremreferenz entfernt. Dies spiegelt das „Schritte“-Skalierungsraster von EA wider und funktioniert sowohl in günstige als auch in ungünstige Richtungen.
- **Schutzstopps:** Der anfängliche Stopp für eine neue Order liegt `StepMultiplier * StopMultiplier * ATR` vom Ausführungspreis entfernt. Wenn die Pyramide voll ist, werden die Stopps auf `StepMultiplier * ATR` hinter (für Long-Positionen) oder vor (für Short-Positionen) des letzten Schlusskurses verschärft, wodurch die nachlaufende Aktualisierung von EA nachgeahmt wird, wenn drei Positionen offen sind.
- **Ungünstige Ausstiege:** Wenn der Preis um `StepsMultiplier * ATR` über das verfolgte Extrem hinaus zurückgeht, verlässt die Strategie sofort alle Positionen auf dieser Seite mit einer Marktorder. Dies erfasst die EA-Logik, die den gesamten Stack verwirft, wenn der Preis die letzte Leiterkante durchbricht.
- **Zustandsrücksetzung:** Nach einem vollständigen Ausstieg werden die Streak-Zähler und die Stoppreferenzen ATR zurückgesetzt, sodass sich vor dem Wiedereintritt eine neue Trendsequenz entwickeln muss.

## Parameter
| Gruppe | Name | Beschreibung | Standard |
| --- | --- | --- | --- |
| Trendfilter | `FastPeriod` | Schnelle SMA-Länge, die die kurzfristige Richtung misst. | `70` |
| Trendfilter | `SlowPeriod` | Langsame SMA-Länge, die die langfristige Richtung misst. | `180` |
| Trendfilter | `MomentumPeriod` | Anzahl aufeinanderfolgender fertiger Kerzen, die den Trend bestätigen müssen. | `50` |
| Volatilität | `AtrPeriod` | ATR Fenster, das für alle Entfernungsberechnungen verwendet wird. | `100` |
| Eingabelogik | `StepMultiplier` | ATR Vielfaches, das erste Ausbrüche verhindert. | `4` |
| Eingabelogik | `StepsMultiplier` | ATR Vielfaches, das Pyramidenschichten trennt. | `2` |
| Risikomanagement | `StopMultiplier` | Zusätzlicher Multiplikator, der auf den anfänglichen Stopp jenseits der Basisschrittentfernung angewendet wird. | `3` |
| Positionsgrößenbestimmung | `PyramidLimit` | Maximale Anzahl Einträge pro Richtung. | `3` |
| Handel | `TradeVolume` | Mit jeder Marktorder übermitteltes Strategievolumen. | `1` |
| Allgemein | `CandleType` | Kerzentyp (Zeitrahmen), der für das Abonnement verwendet wird. | `TimeFrame(1h)` |

## Praktische Hinweise
- Die StockSharp-Version verwendet die Strategieeigenschaft `Volume` für die Größenanpassung. Passen Sie `TradeVolume` an die Vertragsgröße Ihres Instruments an, bevor Sie es in Betrieb nehmen.
- Es wird davon ausgegangen, dass Marktaufträge sofort ausgeführt werden, genau wie bei der Verwendung von `CTrade.Buy`/`Sell` durch MT5. In dünnen Märkten möchten Sie möglicherweise die Marktaufträge durch Limit- oder Stop-Aufträge ersetzen.
- Die Hoch-/Tief-Referenzen replizieren die `h_price`- und `l_price`-Variablen von EA und werden jedes Mal aktualisiert, wenn eine neue Ebene hinzugefügt oder entfernt wird. Sie sind wichtig, um zu bestimmen, wann die Leiter hinzugefügt oder gespült werden muss.
- Da die EA Stop-Verluste pro Position speichern, während StockSharp sie auf Strategieebene verwaltet, wendet der Port die strengste Stop-Logik auf den gesamten Stapel an. Dies führt zu demselben Verhalten (alle Positionen werden gemeinsam geschlossen), wobei weniger Aufträge auf Brokerseite verwaltet werden müssen.
- Testen Sie die Strategie immer in der Simulation. ATR-Distanzen passen sich der Volatilität an, aber in Märkten mit Lücken oder hohem Slippage kann das realisierte Risiko immer noch die prognostizierte Stop-Distanz überschreiten.
