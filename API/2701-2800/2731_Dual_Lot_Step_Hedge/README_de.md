# Dual-Lot-Schritt-Hedge-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht

Die **Dual-Lot-Schritt-Hedge-Strategie** ist ein C#-Port der MetaTrader 5-Experten *"x1 lot from high to low"* und *"x1 lot from low to high"* (Ordner `MQL/19543`). Die ursprünglichen Roboter öffnen sofort einen gehedgten Korb aus Kauf- und Verkaufspositionen, zyklieren das Ordervolumen nach jedem neuen Einstieg und schließen den gesamten Korb, sobald ein festes Gewinnziel erreicht wird. Diese Implementierung reproduziert dieses Verhalten auf der StockSharp High-Level-API unter Bereitstellung sauberer Parameter und detailliertem Zustandsmanagement.

Zwei Betriebsmodi sind verfügbar:

- **HighToLow** – startet mit dem maximalen Lot-Multiplikator, öffnet den ersten gehedgten Korb mit dem größten Volumen und verringert dann auf den nächsten Lot-Schritt nach den ersten Einstiegen.
- **LowToHigh** – beginnt mit dem minimalen Lot-Schritt, erhöht die Lotgröße nach jedem neuen Einstieg, bis der konfigurierte Multiplikator erreicht ist, und handelt dann bei dieser Größe.

Die Strategie hält sowohl Kauf- als auch Verkaufsbeine gleichzeitig aktiv, verwaltet Stop-Loss- und Take-Profit-Niveaus pro Bein und überwacht das Portfolio-Eigenkapital zur Durchsetzung eines korbweiten Gewinnziels.

## Handelslogik

1. Wenn keine Positionen existieren, öffnet die Strategie **sowohl** eine Long- als auch eine Short-Marktorder mit der aktuellen Lotgröße.
2. Wenn genau ein Bein aktiv ist (zum Beispiel wurde die entgegengesetzte Seite gestoppt), wird das fehlende Bein zum Markt mit der aktuellen Lotgröße wieder geöffnet.
3. Nach jedem erfolgreichen Einstieg wird die Lotgröße abhängig vom gewählten Modus (`HighToLow` oder `LowToHigh`) aktualisiert.
4. Pro-Bein-Schutzausstiege werden bei jedem eingehenden Trade-Tick ausgewertet:
   - Ein Long-Bein wird geschlossen, wenn der Preis seinen Stop-Loss (`StopLossPips` unter dem durchschnittlichen Long-Einstieg) oder seinen Take-Profit (`TakeProfitPips` über dem durchschnittlichen Einstieg) erreicht.
   - Ein Short-Bein wird geschlossen, wenn der Preis seinen Stop-Loss (`StopLossPips` über dem durchschnittlichen Short-Einstieg) oder seinen Take-Profit (`TakeProfitPips` unter dem durchschnittlichen Einstieg) erreicht.
5. Sobald der Portfolio-Eigenkapitalgewinn `MinProfit` überschreitet, schließt die Strategie alle verbleibenden Positionen und setzt den Lot-Zustand auf die Startgröße des Modus zurück.
6. Sicherheitslogik schließt den Korb und setzt alles zurück, wenn mehr als eine Kauf- oder Verkaufsposition unerwartet erkannt wird.

Alle Orders werden über die High-Level-Helfer `BuyMarket` und `SellMarket` eingereicht. Die Strategie verfolgt Fills mit `OnOwnTradeReceived`, pflegt aggregierte Exposure pro Bein und verhindert doppelte Orders, während Einstiege oder Ausstiege noch ausstehen.

## Parameter

| Parameter | Beschreibung |
|-----------|--------------|
| `LotMultiplier` | Maximaler Lot-Multiplikator ausgedrückt in minimalen Volumenschritten (Standard `10`). |
| `StopLossPips` | Stop-Loss-Distanz in Pips für jedes Bein (Standard `50`). Auf `0` setzen, um zu deaktivieren. |
| `TakeProfitPips` | Take-Profit-Distanz in Pips für jedes Bein (Standard `150`). Auf `0` setzen, um zu deaktivieren. |
| `MinProfit` | Korbgewinnziel in Kontowährung. Sobald der Eigenkapitalgewinn diesen Wert überschreitet, werden alle Positionen geschlossen (Standard `27`). |
| `ScalingMode` | Lot-Schrittverhalten. `HighToLow` spiegelt den "x1 lot from high to low"-EA, `LowToHigh` spiegelt "x1 lot from low to high". |

Die Strategie leitet automatisch den minimalen Volumenschritt aus `Security.VolumeStep` ab und berechnet den Pip-Wert mit dem Sicherheitspreisschritt (mit der traditionellen 4/5-stelligen Forex-Anpassung).

## Zurücksetzen und Volumen-Cycling

- **HighToLow** – öffnet den ersten Korb mit dem höchsten Volumen (`VolumeStep * LotMultiplier`). Nach jedem Einstieg wird das interne Volumen um einen Schritt reduziert. Wenn das Korbgewinnziel erreicht wird, wird das Volumen auf `0` zurückgesetzt, damit der nächste Zyklus wieder vom Maximum beginnt.
- **LowToHigh** – startet vom minimalen Lot-Schritt. Nach jedem Einstieg wird das Lot um einen Schritt erhöht, bis die Multiplikator-Obergrenze erreicht ist. Wenn das Korbgewinnziel erreicht wird, wird das Volumen auf den minimalen Schritt zurückgesetzt.

## Verwendungshinweise

- Die Strategie abonniert Tick-Trades (`DataType.Ticks`), weil die ursprünglichen MetaTrader-Bots auf Tick-Ereignissen laufen. Konfigurieren Sie den Historiendatenanbieter oder Live-Connector entsprechend.
- Stop-Loss- und Take-Profit-Prüfungen erfolgen innerhalb des Algorithmus, daher werden keine zusätzlichen Schutzorders am Exchange registriert.
- Da beide Beine zu Marktpreisen eröffnet werden, funktioniert die Strategie am besten bei Brokern, die gehedgte Positionen und kleine Spreads unterstützen. Auf Netting-Plätzen wird sie weiterhin funktionieren, aber Beine kompensieren sich effektiv, bis eines davon durch die interne Logik geschlossen wird.
- Die Standardparameter kopieren die ursprünglichen MQL-Einstellungen. Passen Sie sie sorgfältig an: Das Hedgen hoher Volumen kann erhebliche Drawdowns erzeugen, bevor das Korbgewinnziel erreicht wird.

## Zuordnung zur ursprünglichen MQL-Logik

| MetaTrader Variable | C#-Eigenschaft / Verhalten |
|---------------------|---------------------------|
| `InpLots` | `LotMultiplier` mit automatischer Volumenschritt-Behandlung. |
| `InpStopLoss` & `InpTakeProfit` | `StopLossPips` und `TakeProfitPips` mit Pip-Konvertierung basierend auf `PriceStep`. |
| `InpMinProfit` | `MinProfit` und die Portfolio-Eigenkapital-Prüfung. |
| `LotCheck` | `LotCheck`-Helfer, der den Mindestschritt und das maximale Volumen durchsetzt. |
| `CalculatePositions` | Interne Long/Short-Exposure-Verfolgung durch `OnOwnTradeReceived`. |
| `CloseAllPositions()` | `CloseAllPositions`-Methode mit ausstehender Order-Koordination und Zustandsreset. |

## Risikomanagement-Überlegungen

Die Strategie hält absichtlich sowohl Long- als auch Short-Positionen offen, was zu kontinuierlicher Exposure gegenüber Spread-Kosten und Swap-Raten führt. Vor dem Betrieb mit echtem Kapital:

- Das Verhalten im StockSharp-Emulator oder im Paper-Trading validieren.
- Sicherstellen, dass Ihr Broker Hedging unterstützt; andernfalls werden Long/Short-Beine sofort genetzt.
- Stop-Loss-, Take-Profit- und Gewinnzielwerte an die Volatilität des Instruments anpassen.
- Margennutzung überwachen, da gleichzeitige Long/Short-Beine die nominale Exposure verdoppeln.

## Dateien

- `CS/DualLotStepHedgeStrategy.cs` – StockSharp-Strategieimplementierung mit umfangreichen Inline-Kommentaren.
- `README_ru.md` – Russische Übersetzung mit detaillierten Anweisungen.
- `README_zh.md` – Chinesische Übersetzung mit detaillierten Anweisungen.
