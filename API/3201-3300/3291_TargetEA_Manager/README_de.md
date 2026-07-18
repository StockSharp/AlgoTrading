# Target-EA-Manager-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Target-EA-Manager-Strategie** ist eine getreue StockSharp-Portierung des MetaTrader-Experts *TargetEA_v1.5*. Die Strategie eröffnet selbst keine neuen Trades. Stattdessen überwacht sie ständig den schwebenden Gewinn und Verlust der Orders, die bereits zur Strategie gehören, und liquidiert bei Bedarf Positionen und storniert Pending Orders, wenn benutzerdefinierte Schwellen erreicht werden. Das Verhalten reproduziert die "Basket"-Management-Logik des ursprünglichen Experts: Kauf- und Verkaufsorders können unabhängig oder als ein kombinierter Basket bewertet werden.

Die Strategie abonniert Level1-Daten (bester Bid und Ask) und nutzt die High-Level-API für Positionsschließungen und Orderstornierungen. Echtzeit-Bid- und Ask-Kurse werden in unrealisierte Gewinnmetriken für die offene Exposure übersetzt.

## Hauptfunktionen
- **Unabhängige oder kombinierte Baskets** - wählen Sie über `ManageBuySellOrders`, ob Long- und Short-Orders getrennt oder zusammen behandelt werden.
- **Mehrere Zieltypen** - Schwellenwerte können in Pips, in Kontowährung pro Lot oder als Prozentsatz des Portfoliosaldos ausgedrückt werden, passend zum `TypeTargetUse`-Flag der MQL-Version.
- **Trigger für beide Seiten** - getrennte Schalter für Reaktionen auf schwebende Gewinne (`CloseInProfit`) und schwebende Verluste (`CloseInLoss`).
- **Bereinigung von Pending Orders** - optionales Stornieren von Kauf- und/oder Verkauf-Pending-Orders jedes Mal, wenn ein Basket geschlossen wird.
- **High-Level-Operationen** - Marktausstiege werden mit `BuyMarket` / `SellMarket` ausgeführt, Pending Orders werden über die Strategie-Orderkollektion storniert.

## Parameter
| Parameter | Beschreibung |
|-----------|--------------|
| `ManageBuySellOrders` | `Separate` emuliert zwei Baskets (Long und Short), `Combined` führt beide Seiten zusammen. |
| `CloseBuyOrders` / `CloseSellOrders` | Aktiviert Liquidierung für die jeweilige Seite. |
| `DeleteBuyPendingPositions` / `DeleteSellPendingPositions` | Storniert aktive Pending Orders, nachdem ein Basket geschlossen wurde. |
| `TypeTargetUse` | `Pips`, `CurrencyPerLot` oder `PercentageOfBalance` wählen die Messung für offenen PnL. |
| `CloseInProfit` / `CloseInLoss` | Aktiviert Gewinn- oder Verlusttrigger. |
| `TargetProfitInPips`, `TargetLossInPips` | Schwellenwerte in Pips. Wenn das Instrument `PriceStep` bereitstellt, wird der Pip-Wert als `priceDifference / PriceStep * (volume / VolumeStep)` berechnet. |
| `TargetProfitInCurrency`, `TargetLossInCurrency` | Schwebender Gewinn oder Verlust pro Lot, vor dem Vergleich mit dem aktuellen Volumen multipliziert. |
| `TargetProfitInPercentage`, `TargetLossInPercentage` | Prozentsatz des Portfoliosaldos, der vor dem Schließen erreicht werden muss. Der ursprüngliche Expert vergleicht rohen schwebenden Gewinn mit `Balance ± Balance * Percentage / 100`, und diese Portierung behält diese Konvention unverändert bei. |

## Verhalten
1. **Zustandsverfolgung** - ausgeführte Trades aktualisieren interne Long- und Short-Volumentotale sowie deren gewichtete Durchschnittspreise. Gehedgte Positionen (Long und Short) werden daher korrekt behandelt.
2. **PnL-Berechnung** - jede Level1-Aktualisierung erneuert Bid-/Ask-Werte, aus denen Pip- und Währungsgewinne für beide Seiten berechnet werden.
3. **Zielbewertung** - abhängig vom Zielmodus und Basket-Modus werden die entsprechenden Schwellen geprüft. Gewinnprüfungen verlangen Werte *größer oder gleich* den konfigurierten Zielen, während Verlustprüfungen *kleiner oder gleich* verwenden, passend zur MQL-Logik.
4. **Basket-Liquidierung** - wenn eine Bedingung erfüllt ist, storniert die Strategie optional Pending Orders auf dieser Seite und sendet die erforderliche Marktorder, um die offene Exposure glattzustellen.

Die Implementierung vermeidet bewusst zusätzliche Collections oder Indikatorspeicher und verlässt sich wie der ursprüngliche EA auf die High-Level-API von StockSharp.
