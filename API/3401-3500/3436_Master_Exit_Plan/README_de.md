# Master-Exit-Plan-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

`MasterExitPlanStrategy` reproduziert die Risikomanagementlogik des Expertenberaters „Master Exit Plan“ von MetaTrader unter Verwendung des übergeordneten API von StockSharp. Die Strategie eröffnet keine neuen Trades. Stattdessen überwacht es das bestehende Risiko, wendet eine Kombination aus versteckten und sichtbaren Stop-Regeln an, verfolgt ausstehende Aufträge und schließt alles, sobald das Eigenkapital ein konfiguriertes Gewinnziel erreicht.

Die Implementierung abonniert einminütige Kerzen, um die `iOpen(symbol, PERIOD_M1, 1)`-Aufrufe aus dem Originalskript zu emulieren. Alle Timer werden vom Strategieplaner gesteuert und jede Sekunde ausgewertet, passend zum Verhalten der MetaTrader `EventSetTimer(1)`-Schleife.

## Funktionen

- **Equity Target Exit** – schließt alle Positionen, wenn die Portfolio-Aktiengewinne den konfigurierten Prozentsatz erreichen.
- **Statische und dynamische Stop-Level** – überwacht sowohl Stop-Abstände vom Einstiegspreis als auch minutenbasierte dynamische Anker.
- **Hidden Stop Handling** – führt Schutzexits intern aus, anstatt sich auf Börsenaufträge zu verlassen.
- **Trailing-Stop-Modul** – wird nach einem minimalen Geldgewinn aktiviert und folgt dem Stop mit Spread-Kompensation.
- **Pending Order Trailing** – Buy-Stop- und Sell-Stop-Orders werden automatisch neu registriert, damit sie dem Markt folgen.

## Parameter

| Name | Beschreibung | Standard |
| --- | --- | --- |
| `EnableTargetEquity` | Ermöglichen Sie die Liquidation von Eigenkapitalzielen. | `false` |
| `TargetEquityPercent` | Prozentsatz des aktuellen Saldos, der als Ziel verwendet wird. | `1` |
| `EnableStopLoss` | Aktivieren Sie einen statischen Stop-Loss im Broker-Stil. | `false` |
| `StopLossPoints` | Statischer Stoppabstand (MetaTrader Punkte). | `2000` |
| `EnableDynamicStopLoss` | Binden Sie den harten Anschlag an die Öffnung der vorherigen Minute. | `false` |
| `DynamicStopLossPoints` | Dynamischer Stoppabstand (Punkte). | `2000` |
| `EnableHiddenStopLoss` | Aktivieren Sie versteckten statischen Stop-Loss. | `false` |
| `HiddenStopLossPoints` | Verborgener statischer Stoppabstand (Punkte). | `800` |
| `EnableHiddenDynamicStopLoss` | Aktivieren Sie den versteckten dynamischen Stopp basierend auf der offenen Minute. | `false` |
| `HiddenDynamicStopLossPoints` | Verborgener dynamischer Stoppabstand (Punkte). | `800` |
| `EnableTrailingStop` | Aktivieren Sie das Trailing-Stop-Modul. | `false` |
| `TrailingStopPoints` | Der Rückstand hinter dem Preis (Punkte) bleibt erhalten. | `5` |
| `TrailingTargetPercent` | Mindestgewinn in % des Guthabens, bevor das Trailing aktiviert wird. | `0.2` |
| `SureProfitPoints` | Zusätzliche Punkte, die gesichert werden müssen, bevor der Trailing Stop aktiviert wird. | `30` |
| `EnableTrailPendingOrders` | Aktivieren Sie das Nachverfolgen aktiver Stop-Orders (Einträge). | `false` |
| `TrailPendingOrderPoints` | Offset in Punkten für nachlaufende ausstehende Stop-Orders. | `10` |

## Nutzungshinweise

1. Hängen Sie die Strategie an ein Wertpapier an, das bereits von einem anderen Eingabemodul oder manuellen Orders verwaltet wird. Legen Sie `Volume` entsprechend den Verträgen fest, die Sie bei der Reduzierung schließen müssen.
2. Stellen Sie ein Portfolio bereit, das `Portfolio.CurrentValue` meldet. Die Strategie verwendet diesen Wert, um `AccountBalance` und `AccountEquity` aus MetaTrader zu approximieren. Fehlt der Wert, bleibt die Eigenkapitalziellogik untätig.
3. Die Strategie bewertet die besten Geld-/Briefkurse bei der Überprüfung der Stoppbedingungen. Stellen Sie sicher, dass Level-1-Daten verfügbar sind, damit Spread-Aware-Berechnungen aussagekräftig sind.
4. Hidden Stops und Trailing Exits werden als softwaregesteuerte Market Orders umgesetzt. Brokerseitige Stop-Orders werden **nicht** erstellt; Das Verhalten spiegelt die „verborgene“ Natur des ursprünglichen EA wider.

## Unterschiede zur MQL-Version

- Stop-Levels werden durch die Erteilung von Marktaufträgen durchgesetzt, wenn Schwellenwerte überschritten werden. Der ursprüngliche EA hat das Feld `OrderStopLoss` geändert; StockSharp verwendet stattdessen aktive Überwachung.
- Dynamische Stoppberechnungen basieren auf der letzten abgeschlossenen einminütigen Kerze, die über `SubscribeCandles` geliefert wurde. Wenn dieses Abonnement fehlt, bleiben dynamische Regeln deaktiviert.
- Pending Order Trailing ignoriert schützende Stop-Orders, die von anderen Strategien erstellt wurden, da `MasterExitPlanStrategy` sie selbst nicht registriert.
- Eigenkapitalprüfungen verwenden `Portfolio.CurrentValue` (Fallback auf `Portfolio.BeginValue`) anstelle von `AccountBalance`/`AccountEquity`.

## Testen

Die Strategie enthält keine automatisierten Tests. Verwenden Sie den Tester von StockSharp mit historischen Daten, um das Verhalten Ihrer Instrumente vor der Bereitstellung in der Produktion zu überprüfen.
