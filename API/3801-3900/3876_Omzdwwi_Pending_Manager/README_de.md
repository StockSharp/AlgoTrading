# Omzdwwi Ausstehende Managerstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die **Omzdwwi Pending Manager Strategy** ist eine direkte High-Level-StockSharp-Übersetzung des MetaTrader 4-Experten `omzdwwi7739cyjayvs_1_65.mq4`. Der ursprüngliche Berater konzentriert sich auf die Aufrechterhaltung eines Rings ausstehender Aufträge rund um den aktuellen Marktpreis, die Ausführung von Markteintritten zu einem geplanten Zeitpunkt und die Verwaltung von Trailing Stops sowohl für aktive Positionen als auch für ausstehende ausstehende Aufträge. Diese C#-Version reproduziert die gleiche Logik und nutzt dabei die Feeds `Strategy` API, `SubscribeLevel1` und die Auftragsverwaltungshelfer (`BuyStop`, `SellLimit`, `ReRegisterOrder` usw.) von StockSharp.

Die Strategie kontinuierlich:

- Hält bis zu vier ausstehende Aufträge (Kaufstopp, Verkaufsstopp, Kauflimit, Verkaufslimit) in konfigurierbaren Abständen von Geld-/Briefkursen.
- Löst optional marktübliche Kauf-/Verkaufsaufträge zu einer bestimmten Stunde und Minute aus.
- Wendet mehrere Ausstiegsebenen für Marktpositionen an: fester Take-Profit, fester Stop-Loss, zusätzliches „Pips-Gewinn“-Ziel und Trailing-Stop-Logik, die die `TrailingPositions()`-Routine des Experten nachahmt.
- Verschiebt ausstehende Aufträge gemäß den `TrailingOtlozh()`-Regeln des Experten näher oder weiter vom Preis entfernt, sobald der Markt um die konfigurierte Nachlaufdistanz vorrückt.
- Überwacht Gewinn- und Verlustschwellen auf Kontoebene und gibt Informations-/Warnprotokolle aus, wenn die konfigurierten globalen Take-Profit- oder Stop-Loss-Prozentsätze erreicht werden.

## Signalfluss und Datenabonnements

- `SubscribeLevel1()` liefert Gebots-/Briefaktualisierungen. Bei jeder Angebotsaktualisierung werden Zeitprüfungen, Auftragserteilung, nachlaufende Anpassungen und Beendigungsprüfungen ausgelöst. Es sind keine Kerzendaten oder Indikatoren erforderlich.
- `GetWorkingSecurities()` deklariert das Level-1-Abonnement, sodass die Strategie sowohl in Live- als auch in Backtesting-Umgebungen ausgeführt werden kann.

## Eingabelogik

1. **Geplante Marktaufträge.** Wenn `UseTimeSignals` aktiviert ist und die Serveruhr `SignalHour:SignalMinute` erreicht, löst die Strategie boolesche Latches aus, die von den `Time*Signal`-Parametern abgeleitet werden. Das nächste Level-1-Update ruft `BuyMarket()` oder `SellMarket()` auf, sofern `WaitClose`/`MaxMarketOrders` dies zulassen. Die Latches werden unmittelbar nach dem Handel zurückgesetzt.
2. **Persistente ausstehende Orders.** Für jeden aktivierten Bestelltyp (`EnableBuyStop`, `EnableSellStop`, `EnableBuyLimit`, `EnableSellLimit`) überprüft die Strategie, ob eine aktive Bestellung vorhanden ist. Fehlende Aufträge werden `Distance * PriceStep` Punkte vom besten Geld-/Briefkurs entfernt platziert und reproduzieren so das `UstanOtlozh()`-Verhalten des Experten. Wenn die Bestellung bereits vorhanden ist, passt `ReRegisterOrder` den Preis an die aktuellen Angebote an.

## Exit-Logik für Marktpositionen

- **Fester Stop-Loss/Take-Profit** stammen von `MarketStopLossPoints` und `MarketTakeProfitPoints`. Wenn der beste Geld-/Briefkurs diese Schwellenwerte überschreitet, wird die Position über die Marktorder abgeflacht.
- **Zusätzliches Pips-Ziel** repliziert das `PipsProfit`-Verhalten des Experten. Wenn der Wert ungleich Null ist, wird die Position geschlossen, nachdem der konfigurierte Gewinn erzielt wurde, auch wenn TP deaktiviert ist.
- **Trailing Stop** kopiert `TrailingPositions()`. Sobald die Position ausreichend profitabel ist (oder sofort, wenn `RequireProfitBeforeTrailing=false`), wird der interne Trailing-Preis für Long-Positionen auf `Bid - MarketTrailingOffsetPoints * PriceStep` und für Short-Positionen auf `Ask + MarketTrailingOffsetPoints * PriceStep` aktualisiert, wobei der minimale Trail-Schritt durch `MarketTrailingStepPoints` erzwungen wird.

## Nachgestellte Logik für ausstehende Orders

- Stop-Orders verwenden `StopTrailingOffsetPoints` und `StopTrailingStepPoints`. Wenn der Preis den Schwellenwert MQL überschreitet (`Ask < OrderPrice - (offset + step)` für Kaufstopps, symmetrisch für Verkäufe), wird die Order erneut auf `Ask + offset` oder `Bid - offset` registriert.
- Limit-Orders verwenden `LimitTrailingOffsetPoints` und `LimitTrailingStepPoints` auf die gleiche Weise, wodurch die `TrailingOtlozh()`-Anpassungen neu erstellt werden.

## Risiko- und Kontoüberwachung

- `MaxMarketOrders` begrenzt, wie viele Lose (ausgedrückt in Vielfachen von `OrderVolume`) pro Richtung akkumuliert werden können, wenn `WaitClose=false`.
- `UseGlobalLevels`, `GlobalTakeProfitPercent` und `GlobalStopLossPercent` beobachten Portfolio-Aktien. Wenn Schwellenwerte überschritten werden, schreibt die Strategie ein Informations- oder Warnprotokoll, das die ursprünglichen Warn-Popups widerspiegelt.

## Parameter

| Gruppe | Parameter | Beschreibung |
|-------|-----------|-------------|
| Allgemein | `OrderVolume` | Handelsvolumen (Lots), das bei jeder Bestellung wiederverwendet wird. |
| Ausführung | `WaitClose` | Blockieren Sie neue Einträge, bis die Nettoposition flach ist. |
| Ausführung | `MaxMarketOrders` | Maximale gleichzeitige Menge pro Richtung, wenn Pyramidenbildung zulässig ist. |
| Ausstehende Bestellungen | `EnableBuyStop` / `EnableSellStop` / `EnableBuyLimit` / `EnableSellLimit` | Aktivieren oder deaktivieren Sie jeden ausstehenden Auftragstyp. |
| Ausstehende Bestellungen | `StopStepPoints`, `LimitStepPoints` | Abstand in Punkten, der zum Platzieren von Stop-/Limit-Orders relativ zum aktuellen Geld-/Briefkurs verwendet wird. |
| Ausstehende Bestellungen | `StopTakeProfitPoints`, `StopStopLossPoints`, `LimitTakeProfitPoints`, `LimitStopLossPoints` | Schutzabstände werden angewendet, sobald ausstehende Befehle ausgelöst werden. |
| Ausstehende Bestellungen | `StopTrailingOffsetPoints`, `StopTrailingStepPoints`, `LimitTrailingOffsetPoints`, `LimitTrailingStepPoints` | Nachgestellte Parameter für ausstehende ausstehende Aufträge. |
| Marktrisiko | `MarketTakeProfitPoints`, `MarketStopLossPoints` | Take-Profit und Stop-Loss in Punkten für Marktpositionen. |
| Marktrisiko | `MarketTrailingOffsetPoints`, `MarketTrailingStepPoints`, `RequireProfitBeforeTrailing` | Trailing-Stop-Konfiguration für Marktpositionen. |
| Marktrisiko | `ExitProfitPoints` | Zusätzliches festes Gewinnziel. |
| Zeitmanagement | `UseTimeSignals`, `SignalHour`, `SignalMinute` | Einstellungen für die geplante Ausführung. |
| Zeitmanagement | `TimeBuySignal`, `TimeSellSignal`, `TimeBuyStopSignal`, `TimeSellStopSignal`, `TimeBuyLimitSignal`, `TimeSellLimitSignal` | Welche Befehle werden ausgelöst, wenn der Timer ausgelöst wird? |
| Kontoüberwachung | `UseGlobalLevels`, `GlobalTakeProfitPercent`, `GlobalStopLossPercent` | Alarmschwellenwerte auf Portfolioebene. |
| Sonstiges | `SlippagePoints` | Reservierter Legacy-Parameter, der der Vollständigkeit halber beibehalten wird. |

## Konvertierungshinweise

- Der MQL-Experte hat Take-Profit/Stop-Loss direkt für ausstehende Aufträge festgelegt. StockSharp platziert den ausstehenden Eintrag zuerst und verwaltet dann Exits über die Strategielogik, um die Implementierung innerhalb der API-Einschränkungen auf hoher Ebene zu halten.
- Akustische Warnungen wurden weggelassen, da die StockSharp-Protokollierung bereits strukturierte Benachrichtigungen bereitstellt.
- MetaTraders `MODE_STOPLEVEL`-Einschränkung existiert nicht in StockSharp; Daher setzen die Parameter voraus, dass der Händler die von der Börse auferlegten Mindestabstände einhält.
- Bei der Fehlerbehandlung werden `AddInfoLog`/`AddWarningLog` statt `Alert()` Popups verwendet.

## Nutzung

1. Hängen Sie die Strategie an ein `Security` und `Portfolio` mit einem gültigen Preisschritt an.
2. Konfigurieren Sie Entfernungen in Punkten (sie werden mithilfe des `ShrinkPrice` des Wertpapiers automatisch in Preiseinheiten umgerechnet).
3. Starten Sie die Strategie; Es abonniert Kurse der Stufe 1 und beginnt sofort mit der Auftragsverwaltung.

> **Tipp:** Stellen Sie beim Backtesting sicher, dass der Tester Level-1-Daten einspeist, damit die Schluss- und Timing-Logik bei jedem Angebot Aktualisierungen erhält, genau wie der ursprüngliche MQL-Experte.
