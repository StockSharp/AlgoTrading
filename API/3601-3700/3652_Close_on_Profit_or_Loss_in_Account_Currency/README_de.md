# Schließen Sie den Gewinn oder Verlust in der Kontowährung ab
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie reproduziert den Expertenberater MetaTrader *Close_on_PROFIT_or_LOSS_inAccont_Currency*. Es überwacht kontinuierlich das Portfolio-Eigenkapital, an das die Strategie gebunden ist, und sobald ein konfiguriertes Gewinnziel oder eine Drawdown-Untergrenze erreicht ist, wird jede offene Position liquidiert und alle von der Strategie verwalteten ausstehenden Aufträge storniert. Die Klasse basiert auf dem hohen Level API von StockSharp: Ein Kerzenabonnement sorgt für den Herzschlag, `CancelActiveOrders()` entfernt Arbeitsaufträge und `ClosePosition()` glättet das Engagement durch Marktaufträge.

## Wie es funktioniert

1. Die Strategie fragt immer dann das aktuelle Eigenkapital (`Portfolio.CurrentValue`) ab, wenn eine Heartbeat-Kerze geschlossen wird.
2. Wenn das Eigenkapital größer oder gleich **Positive Closing** ist, sendet die Strategie eine vollständige Close-Anfrage.
3. Wenn das Eigenkapital kleiner oder gleich dem **Negative Closing** ist, wird die gleiche Liquidationsroutine ausgeführt, um die Verluste zu begrenzen.
4. Während der Liquidation storniert die Strategie jeden ausstehenden Auftrag, sendet Marktaufträge zum Schließen aller aktiven Positionen und stoppt sich schließlich selbst (was den `ExpertRemove()`-Aufruf des ursprünglichen EA widerspiegelt).

> **Wichtig:** Legen Sie die Schwellenwerte in der Kontowährung fest. Um das ursprüngliche Verhalten zu emulieren, wählen Sie einen **Positive-Closure**-Wert über dem aktuellen Eigenkapital und einen **Negative-Closure**-Wert darunter; andernfalls wird der Exit sofort beim Start ausgelöst.

## Parameter

| Name | Beschreibung | Standard |
|------|-------------|---------|
| `PositiveClosureInAccountCurrency` | Eigenkapitalniveau, bei dessen Überschreitung eine Vollliquidation ausgelöst wird. | `0` |
| `NegativeClosureInAccountCurrency` | Eigenkapitaluntergrenze, die bei Erreichen die Liquidation erzwingt. | `0` |
| `CandleType` | Zeitrahmen, der für die Heartbeat-Kerzen verwendet wird, die die Eigenkapitalprüfungen steuern. Reduzieren Sie es für schnellere Reaktionen. | `1 minute` |

## Notizen

- `StartProtection()` wird beim Start aktiviert, um das ursprüngliche Sicherheitsverhalten zu kopieren.
- Die Strategie interagiert nur mit den von ihr verwalteten Positionen und Aufträgen; Fügen Sie es dem Portfolio hinzu, das die Trades enthält, die Sie schützen möchten.
- Es gibt keine separate Spread-/Slippage-Eingabe, da StockSharp Marktaufträge bereits konnektorspezifische Ausführungskosten berücksichtigen.
