# Sail-System-EA-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Sail System EA ist ein Hedging-Scalper, der symmetrische Long-/Short-Exposure hält und dabei ständig Brokeranforderungen wie maximalen Spread, minimale Stop-Ebene und Handelssitzungsgrenzen prüft. Der StockSharp-Port stellt das ursprüngliche Verhalten mit der High-Level-`Strategy`-API nach: Die Engine abonniert Level-1-Quotes, öffnet oder rearmt beide Seiten des Hedges und verwaltet virtuelle Stop-Loss-/Take-Profit-Niveaus ohne Low-Level-Connector-Aufrufe.

Die Implementierung hält zwei interne `PositionState`-Objekte (Long und Short). Für jede Seite verfolgt die Strategie Einstiegspreis, Restvolumen, virtuelle Schutzlevel und Pending Orders. Dies spiegelt den MQL-Experten wider, der separate Ticketzähler für Markt- und Pending-Orders führte.

## Handelslogik
1. **Sitzungsfilter.** Handel kann auf ein konfigurierbares Zeitfenster beschränkt werden. Liegt die aktuelle Zeit außerhalb der Sitzung, hält, storniert oder schließt die Strategie bestehende Exposure abhängig von `ManageExistingOrders`.
2. **Spread-Wächter.** Bid/Ask-Updates werden über `SubscribeLevel1()` gesammelt. Die Strategie prüft entweder den momentanen Spread oder einen rollierenden Durchschnitt (bis zu 100 Samples) und vergleicht den Wert mit `MaxSpread` plus konfigurierter Kommission. Ist der Spread zu weit, kann das System offene Positionen schließen, und die Einstiegsdistanz kann mit `MultiplierIncrease` multipliziert werden, um ruhigere Bedingungen abzuwarten.
3. **Einstiegsengine.** Wenn Handel erlaubt ist, öffnet die Strategie beide Seiten mit Marktorders oder hält gepaarte Limit-Orders, abhängig von `UsePendingOrders`. Der Limitpreis neuer Orders wird aus aktuellem bestem Bid/Ask plus `DistancePending` (in Pips) und optionalem Sicherheitsmultiplikator abgeleitet.
4. **Virtueller Schutz.** Jede Füllung setzt virtuelle Stop-Loss- und optionale Take-Profit-Niveaus mit `OrdersStopLoss` / `OrdersTakeProfit`. Virtuelle Levels werden nach `DelayModifyOrders` Quote-Updates neu berechnet, aber nur wenn die Verbesserung größer als `StepModifyOrders` ist. Der Mechanismus reproduziert schrittweise Stop-Anpassungen aus der MQL-Version ohne `OrderModify`.
5. **Ausstiegsbehandlung.** Wenn der Bid (für Longs) oder Ask (für Shorts) den virtuellen Stop oder das Ziel erreicht, sendet die Strategie die entgegengesetzte Marktorder zum Schließen der Position. Ausstiege werden nach Grund gekennzeichnet (Stop Loss, Take-Profit, Sitzungsende oder Spread-Verstoß), damit das Trade-Log der Expert-Advisor-Ausgabe entspricht.
6. **Wiedereinstiegsverwaltung.** Wenn Pending Orders um mehr als `PipsReplaceOrders` multipliziert mit `SafeMultiplier` vom Markt wegdriften, werden sie storniert und zu frischen Preisen neu erstellt. Dies ersetzt die timerbasierte Relocation-Logik des MQL-Skripts.
7. **Lotgröße.** Entweder wird ein festes `ManualLotSize` genutzt oder das Volumen aus Portfolio-Equity und `RiskFactor` abgeleitet, was die Auto-Lot-Berechnung des ursprünglichen Codes imitiert.

## Parameter
| Parameter | Beschreibung |
|-----------|-------------|
| `OrderVolume` / `ManualLotSize` | Basisvolumen pro Order, wenn automatisches Sizing deaktiviert ist. |
| `AutoLotSize`, `RiskFactor` | Aktiviert equitybasiertes Lot-Sizing. |
| `UseVirtualLevels` | Hält Stop-Loss-/Take-Profit-Logik auf Strategieseite. |
| `OrdersStopLoss`, `OrdersTakeProfit`, `PutTakeProfit` | Schutzdistanzen in Pips. |
| `DelayModifyOrders`, `StepModifyOrders` | Steuern, wie schnell virtuelle Levels aktualisiert werden. |
| `PipsReplaceOrders`, `SafeMultiplier` | Erzwingen Wiedereinstieg, wenn Pending Orders zu weit vom Markt entfernt sind. |
| `UsePendingOrders`, `DistancePending` | Wechsel zwischen Limit- und Markteinstiegen. |
| `UseTimeFilter`, `TimeStartTrade`, `TimeStopTrade`, `ManageExistingOrders` | Konfiguration des Handelsfensters. |
| `MaxSpread`, `TypeOfSpreadUse`, `HighSpreadAction`, `MultiplierIncrease`, `CloseOnHighSpread` | Spreadfilter und Reaktion. |
| `CommissionInPip`, `CountAvgSpread`, `TimesForAverage` | Steuerelemente der Spread-Mittelung. |
| `AcceptStopLevel`, `Slippage`, `OrdersId` | Broker-Stoplevel, Ausführungsslippage und Magic-Number-Äquivalent. |

Alle Parameter werden über `StrategyParam<T>` bereitgestellt, sodass sie in der Designer-UI verfügbar und mit Optimierungsläufen kompatibel sind.

## Unterschiede zu MQL
- StockSharp verwendet ein Netting-Positionsmodell; daher storniert die Strategie die entgegengesetzte Pending Order, sobald eine Seite gefüllt ist, um ein Glattstellen der Nettoposition zu vermeiden. Das bewahrt dennoch das alternierende Hedge-Verhalten des ursprünglichen EA.
- Das Flag `UseVirtualLevels` hält Stop-Loss-/Zielverwaltung innerhalb der Strategie. Der MQL-Experte nutzte Chartobjekte zur Visualisierung; dieser Port protokolliert jede Aktualisierung statt Linien zu zeichnen.
- Spread-Mittelung ist als inkrementeller laufender Mittelwert implementiert und ersetzt den MQL-arraybasierten Akkumulator, während dieselbe Periodenbegrenzung eingehalten wird.

## Nutzung der High-Level-API
- `SubscribeLevel1().Bind(ProcessLevel1)` treibt die gesamte Entscheidungsengine anhand von Best-Bid/Ask-Updates.
- Ein- und Ausstiegsorders werden über Helfer im Stil `RegisterOrder`, `BuyMarket`, `SellMarket` erstellt, genau wie in den Konvertierungsrichtlinien empfohlen.
- `StartProtection()` wird einmal während `OnStarted` aufgerufen, entsprechend der Framework-Best-Practice zur Aktivierung von Schutzorder-Support.
