# Blonde Trader-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Blonde Trader ist eine aus MQL konvertierte Grid-Handelsstrategie. Sie sucht nach Preisbewegungen weg von jüngsten Extremen und eröffnet Positionen mit einem Grid aus ausstehenden Orders.

## Konzept

- Berechnung des höchsten Hochs und des niedrigsten Tiefs über die letzten **Period X** Kerzen.
- Wenn der aktuelle Preis um mehr als **Limit** Ticks unter dem jüngsten Hoch liegt, wird eine Long-Position eröffnet und eine Reihe von Buy-Limit-Orders als Grid platziert.
- Wenn der aktuelle Preis um mehr als **Limit** Ticks über dem jüngsten Tief liegt, wird eine Short-Position eröffnet und eine Reihe von Sell-Limit-Orders als Grid platziert.
- Alle Positionen werden geschlossen, wenn der kumulierte Gewinn **Amount** erreicht.
- Optional wird nach einer Preisbewegung von **LockDown** Ticks im Gewinn eine Stop-Order auf dem Breakeven-Niveau platziert.

## Parameter

| Name | Beschreibung |
| ---- | ----------- |
| `PeriodX` | Rückblickperiode für das höchste Hoch und das niedrigste Tief. |
| `Limit` | Mindestabstand in Ticks vom aktuellen Preis zu einem Extrem. |
| `Grid` | Schrittweite in Ticks zwischen den Grid-Pending-Orders. |
| `Amount` | Gewinnziel in der Kontowährung. |
| `LockDown` | Abstand in Ticks zum Verschieben des Stops auf Breakeven. |
| `CandleType` | Kerzentyp für die Analyse. |

## Indikatoren

- `Highest` – verfolgt das höchste Hoch über den Rückblickzeitraum.
- `Lowest` – verfolgt das niedrigste Tief über den Rückblickzeitraum.

## Order-Logik

1. Wenn ein Long-Setup erscheint:
   - Kauf zum Marktpreis mit dem Standard-Strategie-Volumen.
   - Vier zusätzliche Buy-Limit-Orders unterhalb des Einstiegs, jeweils um **Grid** Ticks getrennt und mit verdoppeltem Volumen.
2. Wenn ein Short-Setup erscheint:
   - Verkauf zum Marktpreis mit dem Standard-Strategie-Volumen.
   - Vier zusätzliche Sell-Limit-Orders oberhalb des Einstiegs mit denselben Grid- und Volumenverdopplungsregeln.
3. Wenn `PnL` **Amount** erreicht, werden alle offenen Positionen und ausstehenden Orders geschlossen.
4. Wenn `LockDown` größer als null ist und sich der Preis die angegebene Anzahl von Ticks zugunsten der Position bewegt hat, wird eine Schutz-Stop-Order einen Tick jenseits des Einstiegspreises platziert.

## Hinweise

Diese Strategie demonstriert die grundlegende Grid-Handelslogik. Sie verwendet ausschließlich High-Level-API-Funktionen: `SubscribeCandles`, Indikator-Bindung und einfache Order-Hilfsfunktionen wie `BuyMarket`, `SellLimit` und `SellStop`.
