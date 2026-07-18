# Zone-Recovery-Button-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **Zone-Recovery-Button-Strategie** ist eine direkte Umwandlung des MetaTrader Expert Advisors "ZONE RECOVERY BUTTON VER1" (`MQL/25347`). Der ursprüngliche Roboter stützte sich auf BUY/SELL-Schaltflächen im Chart, um einen gehedgten Basket zu starten. In dieser StockSharp-Portierung wird das manuelle Panel durch Parameter ersetzt, während Recovery-Logik, Money-/Prozent-Take-Profits, Trailing Stop in Währung und Equity-Stop-Schutz erhalten bleiben.

Sobald die Strategie eine Startrichtung erhält, eröffnet sie eine initiale Marktorder. Immer wenn der Preis die konfigurierte Zonenbreite durchläuft, stapelt das System einen Gegentrade mit erhöhtem Volumen. Der Basket wird geschlossen, wenn der Referenz-Take-Profit erreicht ist, der schwebende Gewinn das konfigurierte Geld-/Prozentziel erreicht, der Trailing Stop zu viel Gewinn zurückgibt oder die Equity-Stop-Schwelle verletzt wird.

## Handelsregeln

1. **Startrichtung** - emuliert das Drücken der BUY- oder SELL-Schaltfläche. Die Strategie eröffnet die erste Order sofort, sobald sie Daten erhält und handeln darf. Nach dem Schließen des Baskets kann sie automatisch mit derselben Richtung neu starten.
2. **Zone Recovery** - bei jedem Recovery-Schritt wechselt der Algorithmus die Richtung. Für Long-Zyklen verkauft er, sobald der Preis unter `Base Price - Zone Width` fällt, und kauft dann wieder, wenn der Markt über die Basis zurückkehrt. Für Short-Zyklen ist die Logik gespiegelt.
3. **Volumenskalierung** - jeder zusätzliche Hedge multipliziert entweder das vorherige Volumen oder fügt einen festen Zuwachs hinzu und reproduziert damit die "Lots"/"Multiply"-Einstellungen des EA.
4. **Take-Profit-Steuerungen** - der Basket wird geschlossen durch:
   - pipbasierten Take-Profit, gemessen vom Referenzpreis;
   - Geldziel in Kontowährung;
   - Prozentziel, berechnet aus dem aktuellen Portfoliowert;
   - Trailing-Logik, die Gewinne sperrt, sobald der schwebende Gewinn eine Schwelle überschreitet und danach mehr als den erlaubten Drawdown zurückgibt;
   - Notfall-Equity-Stop, der den aktuellen schwebenden Verlust mit der höchsten während des Zyklus beobachteten Equity vergleicht.

## Parameter

| Parameter | Standard | Beschreibung |
|-----------|----------|--------------|
| `CandleType` | `TimeSpan.FromMinutes(5)` | Kerzentyp zur Überwachung von Preisbewegungen. |
| `StartDirection` | `Buy` | Anfängliche Zyklusrichtung (BUY/SELL/NONE). |
| `AutoRestart` | `true` | Startet automatisch einen neuen Zyklus, nachdem der vorherige Basket geschlossen wurde. |
| `TakeProfitPips` | `200` | Pip-Distanz zwischen Basispreis und pipbasiertem Take-Profit-Ziel. |
| `ZoneRecoveryPips` | `10` | Pip-Distanz, die den nächsten Hedge in Gegenrichtung auslöst. |
| `InitialVolume` | `0.01` | Volumen (Lots) des ersten Trades. |
| `UseVolumeMultiplier` | `true` | Wenn aktiviert, multipliziert jeder Hedge das vorherige Volumen; andernfalls wird `VolumeIncrement` addiert. |
| `VolumeMultiplier` | `2` | Multiplikator, der angewendet wird, wenn `UseVolumeMultiplier` `true` ist. |
| `VolumeIncrement` | `0.01` | Volumenzuwachs, wenn `UseVolumeMultiplier` `false` ist. |
| `MaxTrades` | `100` | Maximale Anzahl von Trades im Basket. |
| `UseMoneyTakeProfit` | `false` | Aktiviert das Schließen, wenn schwebender Gewinn `MoneyTakeProfit` überschreitet. |
| `MoneyTakeProfit` | `40` | Gewinnziel in Kontowährung. |
| `UsePercentTakeProfit` | `false` | Aktiviert das Schließen, wenn schwebender Gewinn `PercentTakeProfit` Prozent des Saldos überschreitet. |
| `PercentTakeProfit` | `10` | Gewinnziel in Prozent des aktuellen Portfoliowerts. |
| `EnableTrailing` | `true` | Aktiviert Gewinn-Trailing in Währung. |
| `TrailingProfitThreshold` | `40` | Gewinnniveau, das Trailing aktiviert. |
| `TrailingDrawdown` | `10` | Zulässiger Drawdown vom höchsten schwebenden Gewinn vor dem Schließen des Baskets. |
| `UseEquityStop` | `true` | Aktiviert den Notfall-Equity-Stop. |
| `TotalEquityRiskPercent` | `1` | Maximaler schwebender Verlust (in Prozent des Equity-Hochs) vor dem Glattstellen. |

## Hinweise

- Die Strategie arbeitet mit jedem Instrument, das `PriceStep`- und `StepPrice`-Werte bereitstellt. Diese Parameter werden benötigt, um Pip-Distanzen in Preis- und Währungseinheiten umzuwandeln.
- Da StockSharp ein Nettopositionsmodell verwendet, wird das Hedging-Grid intern simuliert. Die Strategie führt eine eigene Liste von Tradeschritten, um die MetaTrader-Gewinnberechnung zu reproduzieren.
- Die Trailing-Logik arbeitet auf dem schwebenden Gewinn des aktiven Baskets. Sie verwendet keine orderbasierten Trailing Stops.
