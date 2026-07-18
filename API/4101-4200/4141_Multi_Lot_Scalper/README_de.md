# Multi-Lot-Scalper-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die **Multi-Lot-Scalper-Strategie** ist ein Mittelungssystem im Martingal-Stil, das vom klassischen MetaTrader-Expertenberater „Multi Lot Scalper“ abgeleitet wurde. Der ursprüngliche Algorithmus wurde für wichtige FX-Paare entwickelt und stützte sich auf die Steigung des MACD-Histogramms, um zu entscheiden, ob der Markt in eine bullische oder bärische Phase eintritt. Sobald eine Richtung identifiziert ist, öffnet die Strategie eine Leiter von Marktaufträgen und erhöht das Volumen nach jeder negativen Bewegung schrittweise. Der StockSharp-Port behält die ursprüngliche Eingabelogik, Geldverwaltungsregeln und Schutzmechanismen bei und nutzt gleichzeitig das High-Level-Candle-Abonnement API.

Die Strategie funktioniert am besten bei liquiden Instrumenten, bei denen die Spreads eng sind und die Pip-Definition stabil ist. Standardmäßig abonniert es 15-Minuten-Kerzen, aber jeder andere mit den Instrumenten kompatible Zeitrahmen kann über den Parameter `CandleType` angegeben werden.

## Handelslogik

1. **Signalerkennung** – Ein MACD-Indikator (`MacdFastLength`, `MacdSlowLength`, `MacdSignalLength`) wird bei jeder fertigen Kerze ausgewertet. Wenn die MACD-Hauptlinie im Verhältnis zum vorherigen Wert steigt, sucht die Strategie nach Long-Gelegenheiten, andernfalls bereitet sie sich auf Short vor. Der Parameter `ReverseSignals` kehrt diese Interpretation für Benutzer um, die konträre Einträge bevorzugen.
2. **Erster Eintrag** – Die erste Position in einer neuen Sequenz wird unmittelbar nach einem gültigen Signal eröffnet, sofern der Datums-/Uhrzeitfilter (`StartYear`, `StartMonth`, `EndYear`, `EndMonth`, `EndHour`, `EndMinute`) den Handel zulässt. Es werden Marktaufträge verwendet, die die MetaTrader-Implementierung widerspiegeln.
3. **Pyramidenbildung** – Nachfolgende Orders werden nur ausgelöst, wenn sich der Preis um mindestens `EntryDistancePips` gegenüber der letzten Ausführung bewegt. Jeder zusätzliche Handel multipliziert das Basisvolumen entweder mit 2 oder mit 1,5 (wenn `MaxTrades` über 12 liegt), um die Martingalgröße von EA zu reproduzieren.
4. **Stopps und Ziele** – `InitialStopPips` und `TakeProfitPips` werden in Preisniveaus für den gesamten Korb umgewandelt. Ein Trailing Stop wird aktiviert, nachdem die positive Bewegung `EntryDistancePips + TrailingStopPips` überschreitet, wodurch der Ausstieg verschärft wird, wenn sich der Markt beschleunigt.
5. **Kontoschutz** – Wenn der Korb fast seine Kapazität erreicht (`MaxTrades - OrdersToProtect`) und der variable Gewinn `SecureProfit` erreicht, schließt die Strategie den letzten Trade und blockiert vorübergehend neue Einträge, wenn `UseAccountProtection` aktiviert ist.

## Money-Management

Der ursprüngliche Fachberater hat optional die Basislosgröße in Abhängigkeit vom Kontostand neu berechnet. Der Port StockSharp behält dieses Verhalten durch die Parameter `UseMoneyManagement`, `RiskPercent` und `IsStandardAccount` bei. Wenn die Funktion aktiv ist, wird das Basislos (`LotSize`) ignoriert und stattdessen aus dem Portfoliowert abgeleitet, skaliert für Mini- oder Standardkonten, genau wie der MQL-Code.

## Parameter

| Parameter | Beschreibung | Standard |
| --- | --- | --- |
| `TakeProfitPips` | Auf jeden Eintrag angewendete Take-Profit-Distanz, ausgedrückt in Pips. | `40` |
| `LotSize` | Basislosgröße, die verwendet wird, wenn die Geldverwaltung deaktiviert ist. | `0.1` |
| `InitialStopPips` | Anfängliche Stop-Loss-Distanz in Pips. | `0` |
| `TrailingStopPips` | Trailing-Stop-Distanz, die nach dem Schwellenwert aktiviert wird. | `20` |
| `MaxTrades` | Maximale Anzahl gleichzeitig zulässiger Martingaleinträge. | `10` |
| `EntryDistancePips` | Minimale Gegenbewegung vor dem Hinzufügen einer neuen Bestellung. | `15` |
| `SecureProfit` | Variabler Gewinn (in Währung) erforderlich, um den Kontoschutz auszulösen. | `10` |
| `UseAccountProtection` | Ermöglicht das Schließen des letzten Handels, wenn die sichere Gewinnschwelle erreicht ist. | `true` |
| `OrdersToProtect` | Anzahl der endgültigen Trades, die von der Secure-Profit-Regel betroffen sind. | `3` |
| `ReverseSignals` | Kehrt die MACD-Interpretation um (aus bullisch wird short, aus bärisch wird long). | `false` |
| `UseMoneyManagement` | Ermöglicht die auf dem Kontostand basierende Lotberechnung. | `false` |
| `RiskPercent` | Risikoprozentsatz, der verwendet wird, wenn das Geldmanagement aktiv ist. | `12` |
| `IsStandardAccount` | Verwendet die Standard-Lot-Skalierung anstelle der Mini-Lot-Skalierung. | `false` |
| `EurUsdPipValue` | Pip-Wertüberschreibung für EURUSD. | `10` |
| `GbpUsdPipValue` | Pip-Wertüberschreibung für GBPUSD. | `10` |
| `UsdChfPipValue` | Pip-Wertüberschreibung für USDCHF. | `10` |
| `UsdJpyPipValue` | Pip-Wertüberschreibung für USDJPY. | `9.715` |
| `DefaultPipValue` | Für andere Instrumente verwendeter Fallback-Pip-Wert. | `5` |
| `StartYear` | Erstes Kalenderjahr, in dem neue Stellen eröffnet werden können. | `2005` |
| `StartMonth` | Der erste Monat ist für neue Einträge zugelassen. | `1` |
| `EndYear` | Letztes Kalenderjahr für die Initiierung von Trades. | `2006` |
| `EndMonth` | Letzter Kalendermonat für die Initiierung von Trades. | `12` |
| `EndHour` | Stunde (24h), nach der neue Einträge gesperrt werden. | `22` |
| `EndMinute` | Minutenanteil der täglichen Cut-off-Zeit. | `30` |
| `CandleType` | Kerzentyp, der zur Signalgenerierung verwendet wird (Standard ist 15 Minuten). | `15-minute time frame` |
| `MacdFastLength` | Schnelle EMA-Länge des MACD-Indikators. | `14` |
| `MacdSlowLength` | Langsame EMA-Länge des MACD-Indikators. | `26` |
| `MacdSignalLength` | Signallänge EMA des Indikators MACD. | `9` |

## Nutzungsrichtlinien

- Stellen Sie sicher, dass der Pip-Schritt des Instruments mit der Pip-Wertkonfiguration übereinstimmt. Aktualisieren Sie die Pip-Wertparameter, wenn Sie die Strategie auf CFDs, Metalle oder Krypto-Assets anwenden.
- Die Martingal-Skalierung kann die Exposition schnell steigern. Beginnen Sie mit konservativen Werten für `MaxTrades`, `EntryDistancePips` und `TrailingStopPips`, bevor Sie mit größeren Körben experimentieren.
- Optimieren Sie die MACD-Einstellungen und das Kerzenintervall für das gehandelte Instrument. Langsamere Diagramme reduzieren normalerweise die Anzahl der Mittelungsschritte, während schnellere Diagramme die Aktivität erhöhen.
- Die Kontoschutzregel ist besonders wichtig auf Märkten, die zu plötzlichen Umkehrungen neigen. Wenn der gesicherte Gewinn häufig beeinträchtigt wird, sollten Sie eine Reduzierung von `SecureProfit` oder eine Verschärfung von `TrailingStopPips` in Betracht ziehen.
- Mit dem Handelsfensterfilter kann die Strategie nach einer ausgewählten Intraday-Zeit deaktiviert werden. Dies ist nützlich, um Pressemitteilungen oder Volatilität zu später Stunde zu vermeiden.

## Konvertierungshinweise

- Die StockSharp-Version verwendet das High-Level-Kerzenabonnement API (`SubscribeCandles().BindEx(...)`) anstelle der manuellen Tick-Verarbeitung, wodurch die Indikatorverwaltung transparent bleibt.
- Trailing-Stops werden intern gehandhabt, indem das Gesamt-Stop-Level für den Korb verwaltet wird, anstatt jede untergeordnete Order einzeln zu ändern, was das beabsichtigte Verhalten in einer Portfolio-bewussten Umgebung widerspiegelt.
- Die Verwendung von `AccountBalance` durch EA zur Positionsgrößenbestimmung wird der Eigenschaft `Portfolio.CurrentValue` zugeordnet, wodurch die Parität zwischen den Implementierungen MetaTrader und StockSharp gewahrt bleibt.
