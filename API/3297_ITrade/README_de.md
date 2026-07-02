# iTrade-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist ein manueller Verkaufsmanager, der aus dem MetaTrader Expert Advisor **iTrade** konvertiert wurde. Sie bildet den Chart-Schaltflächenablauf des ursprünglichen EA nach: Jedes Mal, wenn der Benutzer einen Verkauf anfordert, wird eine Martingale-Position eröffnet. Danach überwacht die Strategie den schwebenden Gewinn aller Short-Trades und liquidiert die profitabelsten und unprofitabelsten Tickets, sobald vordefinierte Gewinnziele erreicht sind.

## Kernlogik

- Orders werden nur auf ausdrückliche Benutzeranforderungen eröffnet. Rufen Sie `QueueSellRequest()` auf, um den MetaTrader-Schaltflächendruck zu simulieren.
- Die erste Position verwendet das konfigurierte **Anfangsvolumen**. Nach jedem Verlusttrade wird die nächste Ordergröße mit dem **Martingale-Multiplikator** multipliziert. Gewinntrades setzen die Sequenz auf das Basisvolumen zurück.
- Schwebender Gewinn wird mit dem aktuellen besten Ask-Preis gemessen. Wenn der durchschnittliche Gewinn pro offenem Trade das **durchschnittliche Gewinnziel** erreicht, schließt die Strategie die profitabelsten und unprofitabelsten Trades aus dem aktiven Batch (bis zu **Basis-Trade-Anzahl** Trades).
- Wenn mehr als **Basis-Trade-Anzahl** Positionen offen sind, wird das strengere **erweiterte Gewinnziel** angewendet, bevor zwei Trades geschlossen werden.
- Gewinnberechnungen stützen sich auf die Wertpapierwerte `PriceStep` und `StepPrice`. Die Strategie wirft beim Start eine Ausnahme, wenn sie fehlen.

## Parameter

| Name | Beschreibung |
| ---- | ------------ |
| `InitialVolume` | Basis-Lotgröße für die erste Martingale-Order. |
| `MartingaleMultiplier` | Multiplikator, der nach jedem Verlusttrade angewendet wird. |
| `AverageProfitTarget` | Durchschnittlicher schwebender Gewinn (in Währung), der zum Schließen von Trades im ersten Batch erforderlich ist. |
| `ExtendedAverageProfitTarget` | Durchschnittlicher schwebender Gewinnschwellenwert, wenn mehr als der Basis-Batch aktiv ist. |
| `BaseTradeCount` | Anzahl der Trades, die als Teil des Anfangsbatches gelten. |
| `ControlInterval` | Frequenz interner Prüfungen (Timerintervall). |

## Nutzungshinweise

1. Setzen Sie `Security`, `Portfolio` und gewünschte Parameter, bevor Sie die Strategie starten.
2. Rufen Sie `QueueSellRequest()` auf, wann immer ein neuer Verkauf eröffnet werden soll. Die Strategie dimensioniert die Order gemäß den Martingale-Regeln und sendet einen Marktverkauf.
3. Der Algorithmus speichert eine Historie geschlossener Traderesultate (bis zu 200 Einträge), um das ursprüngliche Martingale-Verhalten zu reproduzieren.
4. Schließorders werden als Marktkäufe für das exakte Volumen der Zieltrades gesendet.

## Unterschiede zur MetaTrader-Version

- Die MetaTrader-Version stützte sich auf Chart-Schaltflächen; hier löst der Benutzer Verkäufe programmatisch über `QueueSellRequest()` aus.
- Orderausführung erfolgt über StockSharp-Marktorders. Teilausführungen werden von der Strategie automatisch aggregiert.
- Gewinnschwellen arbeiten mit dezimalen Währungswerten unter Verwendung von `StepPrice`, während der ursprüngliche EA MetaTrader-Ticketgewinnfunktionen nutzte.
