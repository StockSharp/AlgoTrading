# Händler handeln mit der Strategie MACD MQL4
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Dealers Trade MACD MQL4-Strategie ist eine direkte Konvertierung des Expertenberaters „Dealers Trade v7.74“ für MetaTrader 4. Sie behält das Pyramiden-Geldmanagement und die MACD-Steigungslogik des ursprünglichen Systems bei und passt gleichzeitig die Positionsverwaltung an die Nettokonten von StockSharp an. Die Strategie ist für Swing-Trading auf H4/D1-Charts konzipiert und trägt kontinuierlich zum Trend bei, solange die Dynamik mit der MACD-Hauptlinie übereinstimmt.

## Wie die Strategie funktioniert

- **Signalerkennung** – die Strategie abonniert Kerzen des konfigurierten Zeitrahmens und berechnet einen klassischen MACD-Indikator (schneller EMA, langsamer EMA und Signal EMA). Ein steigender MACD-Hauptwert im Vergleich zum vorherigen Balken signalisiert ein zinsbullisches Momentum, während ein fallender Wert ein bärisches Momentum signalisiert. Der Parameter `ReverseCondition` kann verwendet werden, um die Richtung umzukehren, wenn ein konträrer Ansatz bevorzugt wird.
- **Auftragsabstände und -skalierung** – es ist jeweils nur ein Richtungskorb aktiv. Wenn MACD einen langen Trend anzeigt, eröffnet die Strategie eine erste Marktkauforder. Zusätzliche Käufe werden nur gesendet, wenn der Preis um mindestens `SpacingPips * PriceStep` gegenüber dem letzten Einstiegspreis gesunken ist, was dem „Durchschnittsverhalten“ des MQL-Skripts entspricht. Kurze Körbe verhalten sich symmetrisch, wenn die MACD-Steigung negativ wird.
- **Lotgröße** – die Basislosgröße ist entweder der feste `FixedVolume` oder, wenn `UseRiskSizing` aktiviert ist, ein Wert, der aus dem Portfolioeigenkapital und `RiskPercent` abgeleitet wird. Minikonten werden durch das Flag `IsStandardAccount` unterstützt, das die ursprüngliche Option „Konto ist normal“ emuliert. Jede zusätzliche Bestellung im selben Warenkorb wird mit `LotMultiplier` multipliziert und mit `MaxVolume` begrenzt.
- **Risikokontrollen** – harte Stop-Loss- und Take-Profit-Level werden jeder Position mithilfe der Distanzen `StopLossPips` und `TakeProfitPips` zugeordnet. Sobald sich ein Trade um `TrailingStopPips + SpacingPips` im Gewinn bewegt hat, wird das Stop-Level verschärft, um mindestens `TrailingStopPips` des Gewinns beizubehalten, wodurch die Trailing-Regel aus der MetaTrader-Implementierung reproduziert wird.
- **Kontoschutz** – wenn die Anzahl der offenen Trades `MaxTrades - OrdersToProtect` erreicht und der gesamte nicht realisierte Gewinn `SecureProfit` übersteigt, wird der letzte Trade geschlossen, um Gewinne zu sichern, bevor neue Aufträge berücksichtigt werden. Dies entspricht dem „AccountProtection“-Block in der Quelle EA.

## Parameter

| Name | Standard | Beschreibung |
| --- | --- | --- |
| `CandleType` | H4 | Zeitrahmen, der für MACD-Berechnungen und Signalauswertung verwendet wird. |
| `FixedVolume` | 0,1 | Basislosgröße, wenn `UseRiskSizing` deaktiviert ist. |
| `UseRiskSizing` | wahr | Ermöglicht die ausgleichsbasierte Positionsgrößenbestimmung. |
| `RiskPercent` | 2 | Prozentsatz des Eigenkapitals, der zur Größe von Positionen verwendet wird, wenn `UseRiskSizing` wahr ist. |
| `IsStandardAccount` | wahr | Für Minikonten (Lots geteilt durch 10) auf „false“ setzen. |
| `MaxVolume` | 5 | Maximal zulässiges Volumen für eine einzelne Bestellung. |
| `LotMultiplier` | 1.5 | Für jeden weiteren Eintrag im Warenkorb wird ein Multiplikator auf das Basislos angewendet. |
| `MaxTrades` | 5 | Maximale Anzahl gleichzeitig offener Trades. |
| `SpacingPips` | 4 | Mindestpunktabstand zwischen aufeinanderfolgenden Einträgen. |
| `OrdersToProtect` | 3 | Anzahl der gehaltenen Aufträge, bevor die Schutzsperre neue Geschäfte eröffnen kann. |
| `AccountProtection` | wahr | Aktiviert die sichere Gewinnschutzlogik. |
| `SecureProfit` | 50 | Nicht realisierter Gewinn (in Kontowährung), der zum Auslösen des Schutzes erforderlich ist. |
| `TakeProfitPips` | 30 | Take-Profit-Distanz pro Trade, ausgedrückt in Pips. |
| `StopLossPips` | 90 | Stop-Loss-Distanz pro Trade, ausgedrückt in Pips. |
| `TrailingStopPips` | 15 | Nach der Aktivierung angewendeter Trailing-Stop-Abstand. |
| `ReverseCondition` | falsch | Kehrt die MACD-Steigungsinterpretation um. |
| `MacdFast` | 14 | Schnelle EMA-Länge für den MACD-Indikator. |
| `MacdSlow` | 26 | Langsame EMA-Länge für den MACD-Indikator. |
| `MacdSignal` | 1 | Signallänge von EMA für den Indikator MACD. |

## Hinweise und Einschränkungen

- StockSharp-Strategien verwalten eine Nettoposition pro Wertpapier, daher können abgesicherte Long- und Short-Körbe nicht gleichzeitig existieren. Das ursprüngliche EA erlaubte eine Absicherung, aber die Konvertierung schließt die Gegenseite ab, bevor die Richtung geändert wird.
- Die sichere Gewinnlogik berechnet den nicht realisierten Gewinn anhand der Metadaten des Instruments `PriceStep` und `StepPrice`. Instrumente ohne diese Informationen greifen auf einen nominalen Pip-Wert von 0,0001 mit einem Einheitswährungsschritt zurück. Passen Sie daher die Schwellenwerte entsprechend an.
- Für die risikobasierte Größenbestimmung ist ein positiver `StopLossPips`-Wert erforderlich. Wenn die Stop-Distanz Null ist, wird der berechnete Risikobetrag undefiniert und die Strategie überspringt den Handel.
- Die Strategie funktioniert nur bei geschlossenen Kerzen. Signale, die auf Intrabar-MACD-Bewegungen in MetaTrader beruhten, erscheinen in dieser Implementierung möglicherweise einen Balken später, aber das Verhalten ist beim Backtesting deutlich stabiler.
