# Exp Zeitzonenpivots-Offenem-System Tm Plus Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist ein High-Level-StockSharp-Port des Expert Advisors **Exp_TimeZonePivotsOpenSystem_Tm_Plus**. Er recreiert den proprietären *TimeZonePivotsOpenSystem*-Indikator, der zwei Ausbruchszonen rund um das tägliche Sessions-Open projiziert und die Pullbacks handelt, die einem Ausbruch folgen. Jede Komponente des ursprünglichen Skripts—Signalverzögerung, Zeitfilter, asymmetrische Ausstiegslogik und die Geldmanagement-Voreinstellungen—wurde auf explizite Parameter abgebildet, damit das Verhalten mit der MQL5-Implementierung konsistent bleibt.

## Handelslogik

1. Zur konfigurierten `StartHour` zeichnet die Strategie den Session-Eröffnungspreis auf. Zwei dynamische Niveaus werden dann bei `OffsetPoints` (in Punkten) über und unter diesem Anker gezeichnet.
2. Immer wenn eine abgeschlossene Kerze **über** dem oberen Niveau schließt, ist die Strategie:
   - Plant einen Long-Einstieg, der auf der nächsten Kerze ausgeführt wird (unter Beachtung der `SignalBar`-Verzögerung), nur wenn der aktuelle Balken nicht mehr über der Band liegt.
   - Schließt jede offene Short-Position sofort, wenn `SellPosClose` aktiviert ist.
3. Immer wenn eine abgeschlossene Kerze **unter** dem unteren Niveau schließt, ist die Strategie:
   - Plant einen Short-Einstieg für die nächste Kerze, sofern der aktuelle Balken nicht mehr unter der Band liegt.
   - Schließt jede offene Long-Position sofort, wenn `BuyPosClose` aktiviert ist.
4. Einstiege werden beim ersten Update der nächsten Kerze dank `TryExecutePendingEntries` ausgeführt. Dies entspricht dem ursprünglichen Experten, der die Order bis zum Beginn der neuen Kerze verzögert.

Der Signalverzögerungsparameter `SignalBar` reproduziert den ursprünglichen `CopyBuffer`-Shift. Ein Wert von `0` reagiert auf den zuletzt geschlossenen Balken, während `1` einen zusätzlichen Balken wartet, bevor gehandelt wird, was zusätzliche Bestätigung gibt.

## Order-Management

* **Stop-Loss / Take-Profit** – Die Abstände werden in Punkten (`StopLossPoints`, `TakeProfitPoints`) angegeben und unter Verwendung des Instrumentenschritts in Preis umgerechnet. Beide Niveaus werden anhand der Kerzenhochs/-tiefs überwacht, sodass intrabar-Berührungen einen Ausstieg auslösen.
* **Zeitbasierter Ausstieg** – Wenn `TimeTrade` wahr ist, wird die Position nach `HoldingMinutes` Minuten zwangsliquidiert, was den `nTime`-Timer aus dem MQL5-Code spiegelt.
* **Manuelle Schließungen** – Ausbruchssignale in entgegengesetzter Richtung schließen den laufenden Trade, wenn das entsprechende `BuyPosClose`- oder `SellPosClose`-Flag aktiviert ist.

## Geldmanagement

Der `MoneyMode`-Parameter reproduziert die `MarginMode`-Aufzählung:

- `Lot` – festes Volumen gleich `MoneyManagement`.
- `Balance` und `FreeMargin` – verwenden Kontoeigenkapital- oder freie-Margin-Vielfache (`MoneyManagement * Eigenkapital / Preis`).
- `LossBalance` und `LossFreeMargin` – risikobasierte Größenbestimmung, die den gewünschten Kapitalanteil durch den Stop-Abstand teilt.

Wenn `StopLossPoints` auf null gesetzt ist, fallen die Risikomodi auf preisbasierte Größenbestimmung zurück.

## Parameter

| Parameter | Beschreibung | Standard |
|-----------|--------------|---------|
| `MoneyManagement` | Basiskoeffizient zur Positionsgrößenbestimmung abhängig von `MoneyMode`. | `0.1` |
| `MoneyMode` | Positionsgrößenmodell (`Lot`, `Balance`, `FreeMargin`, `LossBalance`, `LossFreeMargin`). | `Lot` |
| `StopLossPoints` | Stop-Loss-Abstand in Punkten vom Ausführungspreis. | `1000` |
| `TakeProfitPoints` | Take-Profit-Abstand in Punkten vom Ausführungspreis. | `2000` |
| `DeviationPoints` | Informativer Parameter aus dem Experten (Slippage-Einstellung in Punkten). | `10` |
| `BuyPosOpen` / `SellPosOpen` | Long- und Short-Einstiege aktivieren oder deaktivieren. | `true` |
| `BuyPosClose` / `SellPosClose` | Dem entgegengesetzten Ausbruch erlauben, Positionen zwangsweise zu schließen. | `true` |
| `TimeTrade` | Den maximalen Haltezeit-Filter aktivieren. | `true` |
| `HoldingMinutes` | Maximale Positionslebensdauer in Minuten. | `720` |
| `OffsetPoints` | Abstand der Pivot-Bänder vom Session-Open in Punkten. | `200` |
| `SignalBar` | Anzahl der Balken zur Verzögerung der Signalauswertung (0 = letzter geschlossener Balken). | `1` |
| `CandleType` | Haupt-Zeitrahmen zur Berechnung des Indikators. | `TimeSpan.FromHours(1).TimeFrame()` |
| `StartHour` | Stunde des Tages (0-23), die den Session-Eröffnungspreis definiert. | `0` |

## Verwendungshinweise

- Die Strategie setzt voraus, dass das Wertpapier einen gültigen `PriceStep` bereitstellt. Wenn dem Instrument diese Metadaten fehlen, wird ein Fallback von `0.0001` verwendet.
- Da Einstiege beim ersten Update einer neuen Kerze ausgelöst werden, folgt der tatsächliche Ausführungspreis dem Markt zu diesem Zeitpunkt, genau wie beim Experten, was in schnellen Märkten vom theoretischen Eröffnungspreis abweichen kann.
- Um die ursprüngliche Indikator-Überlagerung zu replizieren, halten Sie den Backtest-Zeitrahmen bei oder unter H1, da das MQL5-Skript nur auf stündlichen oder niedrigeren Perioden arbeitet.
- Setzen Sie `SignalBar` auf `0` für reaktiveres Verhalten oder auf `1` (Standard), um nach einem Ausbruch eine extra Kerze zu warten.
