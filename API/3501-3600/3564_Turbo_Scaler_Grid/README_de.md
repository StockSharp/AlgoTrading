# Turbo Scaler Grid-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die Turbo Scaler Grid-Strategie ist eine High-Level-StockSharp-Implementierung des MQL5-Expertenberaters „Turbo Scaler Grid Pending“. Die Strategie konzentriert sich auf die Verwaltung ausstehender Stop-Raster um vordefinierte Preisniveaus, den dynamischen Schutz offener Positionen mit Break-Even- und Trailing-Logik sowie die Überwachung des Kontokapitals, um Positionen zu schließen, wenn Gewinn- oder Verlustschwellen erreicht werden.

Die Logik funktioniert in mehreren Zeitrahmen gleichzeitig:

- Ein konfigurierbarer Trigger-Zeitrahmen überwacht Preisnähesignale, die das ausstehende Raster aktivieren.
- Zusätzliche 30-Minuten-, 2-Stunden- und Tageskerzen dienen als Bestätigung für optionale bedingte Auslöser.
- Level1-Daten liefern die neuesten Geld-/Briefwerte, die zur Positionierung ausstehender Aufträge und zur Verwaltung von Trailing Stops verwendet werden.

## Handelsregeln
1. **Ausstehendes Raster**
   - Buy-Stop- und Sell-Stop-Orders werden aus konfigurierbaren Ankerpreisen (`BuyStopEntry` und `SellStopEntry`) platziert.
   - Die Bestellungen haben einen Abstand von `PendingStepPoints` und sind auf `PendingQuantity` begrenzt.
   - Der Preisauslöser überprüft die letzten Kerzen im Auslösezeitrahmen, um zu bestätigen, dass sich der Preis mit ausreichender Dynamik dem Ankerniveau angenähert hat.
   - Der Bedingungsauslöser validiert zusätzliche Multi-Timeframe-Filter (tägliche Blockbereiche, H2- und M30-Kerzenrichtung und mittleres Bereichsniveau), bevor ausstehende Aufträge erteilt werden.
2. **Positionssicherung**
   - Der anfängliche Stop-Loss wird aus `StopLossPoints` (oder Festpreisüberschreibungen) berechnet.
   - Wenn der Preis um `BreakevenTriggerPoints` steigt, wird der Stop auf den Einstiegspreis plus `BreakevenOffsetPoints` (für Long-Positionen) oder abzüglich des Offsets (für Short-Positionen) verschoben.
   - Ein Trailing Stop wird erst aktiviert, nachdem die Gewinnschwelle erreicht ist, und wird aktualisiert, sobald der Preis den vorherigen Stop um `TrailMultiplier * TrailPoints` überschreitet.
3. **Aktienaufsicht**
   - Die Strategie überwacht den schwebenden PnL und erzwingt die Positionsauflösung, wenn der Drawdown `MaxFloatLoss` überschreitet (skaliert auf das ausgewählte Auftragsvolumen).
   - Ein variabler Gewinnauslöser sperrt Gewinne, indem er eine interne Eigenkapitallinie bei `EquityBreakeven` platziert und diese um `EquityTrail` nachzieht, sobald der Gewinn `EquityTrigger` übersteigt.

## Parameter
| Name | Beschreibung |
| --- | --- |
| `StopLossPoints` | Anfängliche Stop-Loss-Distanz in Punkten. |
| `BreakevenTriggerPoints` | Erforderliche Punkte, um die Break-Even-Bewegung zu aktivieren. |
| `BreakevenOffsetPoints` | Der Offset wird dem Einstiegspreis hinzugefügt, wenn der Stop auf die Gewinnschwelle verschoben wird. |
| `TrailPoints` | Distanz, die für das Nachlaufen nach der Gewinnschwelle verwendet wird. |
| `TrailMultiplier` | Der Multiplikator wird angewendet, bevor ein neuer Trailing Stop gesetzt wird. |
| `BuyStopLossPrice` / `SellStopLossPrice` | Optionale feste Stop-Preise für Long-/Short-Positionen. |
| `BuyStopEntry` / `SellStopEntry` | Grundpreise für die ausstehenden Stoppgitter. |
| `OrderVolume` | Volumen pro ausstehender Bestellung. |
| `PendingQuantity` | Maximale Anzahl aktiver ausstehender Bestellungen. |
| `PendingStepPoints` | Abstand zwischen aufeinanderfolgenden ausstehenden Bestellungen. |
| `TriggerCandleType` | Kerzenserie, die für die Preisauslöselogik verwendet wird. |
| `PendingPriceTrigger` | Aktiviert den Preisnähe-Trigger. |
| `PendingConditionTrigger` | Aktiviert den Multi-Timeframe-Bestätigungstrigger. |
| `OrderBuyBlockStart` / `OrderBuyBlockEnd` | Täglicher Low-Block zur Validierung langer Setups. |
| `OrderSellBlockStart` / `OrderSellBlockEnd` | Täglicher hoher Block zur Validierung kurzer Setups. |
| `MaxFloatLoss` | Maximal zulässiger Floating-Verlust (skaliert nach Volumen). |
| `EquityBreakeven` | Das Eigenkapitalniveau bleibt erhalten, nachdem der Gewinnauslöser aktiviert wurde. |
| `EquityTrigger` | Zur Schaffung der Eigenkapitalsperre ist ein variabler Gewinn erforderlich. |
| `EquityTrail` | Auf die Equity-Sperre angewendete Nachlaufdistanz. |

## Notizen
- Das Bestellvolumen wird so skaliert, dass es dem ursprünglichen EA-Verhalten entspricht (`0.01`-Lots werden als Basisschritt behandelt).
- Alle Kommentare im Code sind auf Englisch verfasst, während dieses Dokument eine detaillierte Beschreibung für ein schnelles Onboarding bietet.
- Die Strategie verwendet nur High-Level-StockSharp-APIs (`SubscribeCandles`, `Bind`, `BuyStop`, `SellStop`, `SellMarket`, `BuyMarket`) entsprechend den Projektanforderungen.
