# CM Fishing Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die **CM Fishing Strategie** ist ein Grid-Trading-Ansatz, der vom ursprünglichen MQL-Skript `cm_fishing.mq4` adaptiert wurde. Die Strategie eröffnet Marktorders, wenn sich der Preis um eine feste Anzahl von Punkten vom letzten ausgeführten Trade bewegt. Sie kann ein Grid aus Long- oder Short-Positionen aufbauen und sie schließen, wenn ein bestimmtes Gewinnziel erreicht wird.

Diese Implementierung konzentriert sich auf die Kernhandelslogik ohne die grafische Benutzeroberfläche des ursprünglichen Skripts. Orders werden über die High-Level-API von StockSharp ausgeführt.

## Parameter

| Name | Beschreibung |
|------|--------------|
| `Buy` | Aktiviert oder deaktiviert das Öffnen von Long-Positionen. |
| `Sell` | Aktiviert oder deaktiviert das Öffnen von Short-Positionen. |
| `StepBuy` | Preisschritt in Punkten nach unten, bevor eine neue Long-Position eröffnet wird. |
| `StepSell` | Preisschritt in Punkten nach oben, bevor eine neue Short-Position eröffnet wird. |
| `CloseProfitBuy` | Gewinnschwelle zum Schließen aller Long-Positionen. |
| `CloseProfitSell` | Gewinnschwelle zum Schließen aller Short-Positionen. |
| `CloseProfit` | Gewinnschwelle, die jede offene Position unabhängig von der Richtung schließt. |
| `BuyVolume` | Ordervolumen für jeden Long-Trade. |
| `SellVolume` | Ordervolumen für jeden Short-Trade. |

## Handelslogik

1. Trade-Preise in Echtzeit verfolgen.
2. Wenn der Preis um `StepBuy` vom letzten Trade-Niveau fällt und `Buy` aktiviert ist, eine Markt-Kauforder senden.
3. Wenn der Preis um `StepSell` vom letzten Trade-Niveau steigt und `Sell` aktiviert ist, eine Markt-Verkaufsorder senden.
4. Den durchschnittlichen Einstiegspreis der aktuellen Position pflegen.
5. Positionen schließen, wenn der unrealisierte Gewinn den entsprechenden `CloseProfit*`-Parameter überschreitet.

Die Strategie arbeitet mit Tick-Daten und eignet sich für Demonstrations- und Bildungszwecke.

## Hinweise

- Die Implementierung reproduziert nicht die Benutzeroberfläche des ursprünglichen Skripts.
- Es wird zu jedem Zeitpunkt nur eine Netto-Position (Long oder Short) gehalten.
