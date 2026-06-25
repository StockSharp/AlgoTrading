# Rückkehr-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie repliziert den klassischen "Return Strategy"-Expertenberater. Sie bereitet ein Gitter aus gepaarten Buy-Limit- und Sell-Limit-Orders zu Beginn eines konfigurierten Handelsfensters vor. Das Gitter ist symmetrisch um den Marktpreis, verwendet feste Abstände in Pips und kann entweder nach einem festen Volumen oder einem prozentualen Risikomodell dimensioniert werden. Sobald Orders ausgeführt werden, überwacht die Strategie die Position mit statischer und Trailing-Stop-Loss-Logik, überwacht den kumulativen offenen Gewinn und erzwingt ein vollständiges Glattstellen zur täglichen Cut-off-Zeit oder jeden Freitag.

Das ursprüngliche System wurde für Netting-Konten entwickelt und konzentrierte sich auf das Erfassen von Mean-Reversion-Bewegungen nach geplanten Zeiten. Die Konvertierung behält diese Struktur bei und adaptiert Orderverwaltung, Trailing und Kapitalkontrolle an die StockSharp-High-Level-API.

## Trading-Regeln

- **Tägliche Vorbereitung** – Zur `StartHour` prüft die Strategie, dass keine Gitterorders aktiv sind, und platziert `PendingOrderCount` Buy-Limits unterhalb und Sell-Limits oberhalb des aktuellen Preises. Das erste Level ist um `DistancePips` versetzt und jedes weitere Level fügt `StepPips` Abstand hinzu.
- **Risikosteuerung** – Jede ausstehende Order kann entweder ein festes `OrderVolume` oder eine risikobasierte Größe aus `RiskPercent` verwenden. Wenn Risikosizing verwendet wird, bestimmen das verfügbare Kapital und der Stop-Loss-Abstand das Volumen pro Order, sodass das Gesamtgitterrisiko dem konfigurierten Prozentsatz entspricht.
- **Stop-Verwaltung** – Jede ausgeführte Position erhält einen initialen Stop-Loss basierend auf `StopLossPips`. Wenn `TrailingStopPips` größer als null ist, wird der Stop, sobald der Preis den Trailing-Schwellenwert überschreitet, in Schritten von `TrailingStepPips` nachgezogen.
- **Gewinnziel und Sitzungsausstieg** – Der netto offene Gewinn wird in Pips verfolgt. Wenn er `TotalProfitPips` erreicht, markiert die Strategie alle Positionen und Orders zum Schließen. Dasselbe Leeren erfolgt auch zur konfigurierten `EndHour` und jeden Freitag unabhängig vom Gewinn.
- **Order-Ablauf** – Ausstehende Orders können nach `ExpirationHours` automatisch ablaufen. Abgelaufene oder manuell stornierte Orders werden aus der Verfolgungsliste entfernt, um am nächsten Tag ein neues Gitter platzieren zu können.

## Parameter

| Parameter | Beschreibung |
| --- | --- |
| `StopLossPips` | Initialer Stop-Abstand für jede ausgeführte Position (in angepassten Pips). |
| `StartHour` | Stunde (0–23), wann das ausstehende Order-Gitter erstellt wird. |
| `EndHour` | Stunde (0–23), die einen vollständigen Ausstieg aus Positionen und Orders auslöst. |
| `TotalProfitPips` | Netto offenes Gewinnziel (in Pips), das alle Trades zum Schließen zwingt. |
| `TrailingStopPips` | Abstand des Trailing-Stops vom Preis nach Aktivierung. Auf null setzen, um Trailing zu deaktivieren. |
| `TrailingStepPips` | Zusätzlicher Vorschub erforderlich, bevor der Trailing-Stop bewegt wird. Muss positiv sein, wenn Trailing aktiviert ist. |
| `DistancePips` | Initialer Versatz für die erste ausstehende Order auf jeder Marktseite. |
| `StepPips` | Inkrementeller Abstand zwischen aufeinanderfolgenden ausstehenden Orders. |
| `PendingOrderCount` | Anzahl der Buy-Limits und Sell-Limits, die zur `StartHour` registriert werden. |
| `ExpirationHours` | Lebensdauer ausstehender Orders in Stunden. Null deaktiviert den Ablauf. |
| `OrderVolume` | Festes Volumen pro ausstehender Order. Auf null lassen, um risikobasiertes Sizing zu aktivieren. |
| `RiskPercent` | Portfolio-Prozentsatz, der dem gesamten Gitter zugeordnet wird. Das Volumen pro Order wird aus diesem Wert abgeleitet, wenn `OrderVolume` null ist. |
| `CandleType` | Kerzenserie zur Steuerung der Timing- und Stop-Verwaltungslogik. |

## Zusätzliche Hinweise

- Die Pip-Konvertierung spiegelt die ursprüngliche MetaTrader-Logik wider, indem die Schrittgröße für Instrumente mit drei und fünf Dezimalstellen angepasst wird.
- Wenn `RiskPercent` verwendet wird, gilt der Prozentsatz für das kombinierte Gitter und wird gleichmäßig auf alle ausstehenden Orders aufgeteilt.
- Die Strategie erzwingt Validierungsregeln identisch zum Quell-EA: Stunden müssen innerhalb des täglichen Bereichs liegen, Trailing erfordert einen Nicht-Null-Schritt, und nur eines von `OrderVolume`/`RiskPercent` darf gleichzeitig aktiv sein.
- Alle öffentlichen Kommentare im Code werden aus Konsistenzgründen mit den Repository-Richtlinien auf Englisch bereitgestellt.
