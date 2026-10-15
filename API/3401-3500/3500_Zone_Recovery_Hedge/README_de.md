# Zone Recovery Hedge-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **Zone Recovery Hedge Strategy** ist eine StockSharp-Portierung des MetaTrader-Expertenberaters *Zone Recovery Hedge V1*. Der Algorithmus wechselt Kauf- und Verkaufspositionen um einen Ankerpreis herum, sodass immer dann eine neue Order platziert wird, wenn der Preis die konfigurierte Erholungszone überschreitet. Die Sequenz erweitert das Positionsvolumen nach einem Martingal-Plan, bis entweder das variable Gewinnziel oder der optionale Verlustschutz erreicht ist.

## Strategielogik

1. **Einstiegsfilter** – Wenn der Modus *RSI Multi-Timeframe* ausgewählt ist, prüft die Strategie eine konfigurierbare Liste von RSI-Messwerten (von M1 bis MN1) und erfordert, dass jeder aktivierte Zeitrahmen gleichzeitig einen überkauften/überverkauften Bereich verlässt. Der Übergang von überverkauft zu einem Kaufzyklus, während der Übergang von überkauft zu einem Verkaufszyklus führt. Im *Manuellen* Modus können die Hilfsmethoden `StartManualMarketCycle` und `StartManualPendingCycle` aufgerufen werden, um eine neue Sequenz ohne automatische Signale zu beginnen.
2. **Erster Handel** – Der erste Handel verwendet entweder die feste Lotgröße oder eine risikobasierte Größe, die aus dem Portfolioeigenkapital und der geplanten Stop-Distanz abgeleitet wird. Wenn die ATR-Dimensionierung aktiv ist, werden die Stoppdistanz und die Zonenbreite aus dem täglichen ATR abgeleitet; andernfalls werden Brokerpunkte verwendet.
3. **Erholungsraster** – Wenn sich der Preis um die Entfernung der Erholungszone entgegen der aktiven Richtung bewegt, öffnet die Strategie die entgegengesetzte Seite mit einem erhöhten Volumen (benutzerdefinierte Lot-Leiter, Multiplikator oder additive Stufe). Der Zyklus wechselt ständig die Richtung um den ursprünglichen Ankerpreis herum und baut Volumen auf, bis das Gewinnziel erreicht oder die maximale Anzahl an Trades erreicht ist.
4. **Gewinnkontrolle** – Das Ziel wird in der Kontowährung bewertet, wobei entweder die Basis-Take-Profit-Distanz oder die dedizierte Recovery-Take-Profit-Distanz (mit optionalen ATR-Bruchteilen) verwendet wird. Provisionen können über den Parameter *Test Commission* simuliert werden. Wenn der variable Gewinn das Ziel plus Kosten übersteigt, ist der gesamte Zyklus geschlossen.
5. **Risikoschutz** – Wenn `MaxTrades` ungleich Null ist und `SetMaxLoss` aktiviert ist, werden alle Positionen geschlossen und der Zyklus zurückgesetzt, wenn die maximale Handelsanzahl erreicht wird, während der variable PnL das `MaxLoss`-Limit überschreitet.

> **Hinweis:** StockSharp-Strategien werden standardmäßig verrechnet. Der Port reproduziert die Wiederherstellungslogik, indem er die Nettoposition umkehrt, anstatt gleichzeitig abgesicherte Positionen zu halten. Dadurch bleibt die Gewinnberechnung mit StockSharp kompatibel, während die abwechselnden Wiederherstellungsschritte des ursprünglichen Beraters erhalten bleiben.

## Parameter

| Gruppe | Parameter | Beschreibung |
| --- | --- | --- |
| Allgemein | `CandleType` | Primärer Zeitrahmen, der die Eingabelogik steuert. |
| Allgemein | `Mode` | `Manual` deaktiviert Signale, `RsiMultiTimeframe` aktiviert den RSI-Filter. |
| Signale | `RsiPeriod`, `OverboughtLevel`, `OversoldLevel` | RSI Berechnungszeitraum und Schwellenwerte. |
| Signale | `UseM1Timeframe` … `UseMonthlyTimeframe` | Aktivieren/deaktivieren Sie die RSI Bestätigungen für den entsprechenden Zeitraum. |
| Signale | `TradeOnBarOpen` | Verwenden Sie die vorherige Leiste als Bestätigungsleiste (ursprüngliches EA-Verhalten). |
| Erholung | `RecoveryZoneSize`, `TakeProfitPoints` | Zonenbreite und Basis-Take-Profit, wenn ATR deaktiviert ist. |
| Erholung | `UseAtr`, `AtrPeriod`, `AtrZoneFraction`, `AtrTakeProfitFraction`, `AtrRecoveryFraction`, `AtrCandleType` | ATR-basierte Größeneinstellungen. |
| Erholung | `UseRecoveryTakeProfit`, `RecoveryTakeProfitPoints` | Spezielle Take-Profit-Distanz, wenn sich der Zyklus bereits in der Erholungsphase befindet. |
| Risiko | `MaxTrades`, `SetMaxLoss`, `MaxLoss` | Begrenzen Sie die Anzahl der Trades und definieren Sie einen geldbasierten Verlustschutz. |
| Risiko | `TestCommission` | Geschätzte Provision (in Geld), die pro Handelsvolumen bei der Bewertung des Gewinnziels angewendet wird. |
| Geldmanagement | `RiskPercent`, `InitialLotSize`, `LotMultiplier`, `LotAddition`, `CustomLotSize1` … `CustomLotSize10` | Steuert, wie Volumen für jeden Schritt im Zyklus generiert werden. |
| Timer | `UseTimer`, `StartHour`, `StartMinute`, `EndHour`, `EndMinute`, `UseLocalTime` | Beschränken Sie den Handel auf ein tägliches Zeitfenster. |
| Handbuch | `PendingPrice` | Von `StartManualPendingCycle` verwendeter Referenzpreis. |

## Anwendungstipps

- Hängen Sie die Strategie an eine Datenquelle an, die den höchsten Zeitrahmen bietet, den Sie für RSI Bestätigungen verwenden möchten. Aus dem Basiszeitrahmen können durch den internen Aggregator höhere Zeitrahmen erstellt werden.
- Wenn der Modus *Manuell* aktiv ist, rufen Sie `StartManualMarketCycle(true)` oder `StartManualMarketCycle(false)` auf, um einen Kauf- oder Verkaufszyklus zum aktuellen Preis zu öffnen, oder `StartManualPendingCycle`, um den Zyklus auf einem benutzerdefinierten Preisniveau zu verankern.
- Die saldobasierte Positionsgrößenbestimmung begrenzt den Risikoprozentsatz auf 10 %, genau wie beim Original EA.
- Die Wiederherstellungslogik geht davon aus, dass `Security.PriceStep` und `Security.StepPrice` vom Connector gefüllt werden. Ohne sie kann das Gewinnziel nicht berechnet werden.

## Unterschiede zur MetaTrader-Version

- Der Port StockSharp funktioniert mit Nettopositionen statt mit abgesicherten Long/Short-Körben. Die Erholungssequenz wechselt immer noch die Handelsrichtungen, aber die Positionen werden beim Richtungswechsel umgekehrt.
- Grafische Elemente (Schaltflächen, Linien, Kommentare) aus dem MT4-Panel werden nicht wiedergegeben. Timer- und manuelle Befehle werden durch Strategieparameter und Hilfsmethoden verfügbar gemacht.
- Auf eine Spread-basierte Kostenmodellierung wird verzichtet; nur der Wert `TestCommission` wird vom Gewinnziel abgezogen.
