# Spezifische Tages- und Uhrzeitbestellungen
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie repliziert den MetaTrader Experten *„Expert Advisor spezifischer Tag und Uhrzeit“*.
Es platziert Kauf- und/oder Verkaufsaufträge zu einem geplanten Zeitstempel und entfernt optional jedes Engagement zu einem anderen Zeitstempel.
Die StockSharp-Version behält das ursprüngliche Risikomanagementverhalten bei, einschließlich optionaler Trailing Stops und Break-Even-Bewegungen.

## Kernlogik

1. **Planung**
   - `OpenTime` – Zeitpunkt, an dem Bestellungen erstellt werden.
   - `CloseTime` – Zeitpunkt, an dem Positionen abgeflacht werden und ausstehende Aufträge entfernt werden können.
Beide Prüfungen akzeptieren ein Ein-Minuten-Fenster, das dem im MT4-Code verwendeten `TimeToString(..., TIME_MINUTES)`-Vergleich entspricht.

2. **Auftragserteilung**
   - `OrderPlacement` wählt zwischen Markt-, Stop- oder Limit-Orders.
   - `OpenBuyOrders` / `OpenSellOrders` aktivieren Sie die gewünschten Richtungen.
   - `OrderDistancePoints` gleicht den Preis ausstehender Aufträge um eine Anzahl von Punkten (Pips) aus.
   - `PendingExpireMinutes` storniert ausstehende Orders automatisch, wenn ihr Gültigkeitsfenster endet.

3. **Volumenverwaltung**
   - `LotSizing = Manual` sendet das feste `ManualVolume`.
   - `LotSizing = Automatic` berechnet das Volumen aus dem aktuellen Portfoliowert und der Vertragsgröße des Instruments:
`volume = (portfolio / contractSize) * RiskFactor`.
Das Ergebnis wird an `Security.VolumeStep` ausgerichtet und zwischen `MinVolume`/`MaxVolume` eingeklemmt, sofern verfügbar.

4. **Schutzlogik**
   - `StopLossPoints` und `TakeProfitPoints` übersetzen die ursprünglichen punktbasierten Distanzen mithilfe der Pip-Größe des Instruments in absolute Preise.
   - `TrailingStopEnabled` + `TrailingStepPoints` und `BreakEvenEnabled` verschieben den Schutzstopp genau wie das MQL-Skript, wobei Bid/Ask-Aktualisierungen als Auslöser verwendet werden.
   - Wenn Stop-Loss- oder Take-Profit-Bedingungen erreicht werden, wird die Position mit einer Marktorder geschlossen, was das MT4-Verhalten widerspiegelt, bei dem Stops auf einen neuen Preis geändert werden.

5. **Abschlussphase**
   - Wenn `CloseOwnOrders` oder `CloseAllOrders` aktiviert ist, verlässt die Strategie jede offene Position im Schließfenster.
   - `DeletePendingOrders` entfernt gleichzeitig alle verbleibenden ausstehenden Bestellungen.

## Parameter

| Name | Beschreibung |
|------|-------------|
| `OpenTime`, `CloseTime` | UTC-Zeitstempel für den Markteintritt und -austritt. |
| `OrderPlacement` | Markt-, Stop- oder Limit-Auftragserteilung. |
| `OpenBuyOrders`, `OpenSellOrders` | Anweisungen zur Aktivierung. |
| `TakeProfitPoints`, `StopLossPoints` | Schutzabstände ausgedrückt in Punkten (0 deaktiviert). |
| `TrailingStopEnabled`, `TrailingStepPoints` | Aktivieren Sie den Trailing Stop und legen Sie den Mindestvorschub fest, bevor Sie ihn verschieben. |
| `BreakEvenEnabled`, `BreakEvenAfterPoints` | Verschieben Sie den Stop auf die Gewinnschwelle, sobald der Gewinn den Schwellenwert überschreitet. |
| `OrderDistancePoints` | Offset, der für ausstehende Orders verwendet wird. |
| `PendingExpireMinutes` | Ablauffenster für ausstehende Orders. |
| `LotSizing` | Manuelle oder automatische Volumenanpassung. |
| `RiskFactor`, `ManualVolume` | Eingaben für die Größenmodi. |
| `CloseOwnOrders`, `CloseAllOrders`, `DeletePendingOrders` | Steuern Sie, wie Positionen und ausstehende Aufträge am Ende geschlossen werden. |

## Notizen

- Die Klasse befindet sich im Namespace `StockSharp.Samples.Strategies` mit Tabulatoreinzug, wie in den Projektrichtlinien erforderlich.
- Level1-Daten werden verwendet, um die Gebots-/Brief-sensitive Logik aus der MQL-Version (Trailing Stop, ausstehende Auftragserteilung) zu reproduzieren.
- `MagicNumber`-Einstellungen von MT4 sind nicht erforderlich, da StockSharp bereits Strategieaufträge isoliert.

## Nutzung

1. Kompilieren Sie das Projekt über `AlgoTrading.sln` und hängen Sie die Strategie an ein Wertpapier-/Portfoliopaar an.
2. Passen Sie den Zeitplan, die Auftragsart und die Risikoparameter nach Bedarf an.
3. Starten Sie die Strategie vor `OpenTime`; Bestellungen werden automatisch versendet, sobald das Fenster beginnt.
4. Lassen Sie die Strategie bis nach `CloseTime` laufen, wenn Sie möchten, dass der automatische Reduzierungsschritt ausgelöst wird.
