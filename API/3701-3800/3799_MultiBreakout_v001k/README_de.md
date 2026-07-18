# MultiBreakout V001k-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die MultiBreakout V001k-Strategie reproduziert den klassischen MT4-Expertenberater „Multibreakout_v001k“. Es handelt Ausbrüche aus der vorherigen stündlichen Sitzung, indem es Buy-Stop- und Sell-Stop-Orders stapelt, sobald die Referenzstunde endet. Das Positionsmanagement folgt der ursprünglichen abgestuften Take-Profit- und Break-Even-Logik, einschließlich des optionalen gleitenden Break-Even, der Stopps anhand der neuesten Stundentiefs/-höchststände verfolgt.

## Handelsregeln
1. **Referenzstunde** – Es können bis zu vier Handelssitzungen definiert werden. Nach Abschluss jeder aktivierten Sitzungsstunde misst die Strategie die fertige stündliche Kerze und bereitet Aufträge für die nächste Stunde vor.
2. **Eintrittsplatzierung** –
   - Buy-Stop-Orders werden auf dem Hoch der vorherigen Stunde zuzüglich des aktuellen Spreads und eines zusätzlichen Einstiegspuffers (`PipsForEntry`) positioniert.
   - Verkaufsstopp-Orders werden auf dem Tief der Vorstunde abzüglich des Einstiegspuffers positioniert.
   - Jede Seite platziert `NumberOfOrdersPerSide` ausstehende Aufträge mit identischem Volumen.
3. **Take-Profit-Leiter** – Jeder Eintrag erhält ein individuelles Gewinnziel im Abstand von `TakeProfitIncrement` Punkten. Wenn der Markt jedes Niveau erreicht, schließt die Strategie eine Tranche zum Marktwert, um die ursprüngliche MT4-Take-Profit-Warteschlange nachzuahmen.
4. **Stop-Loss-Management** – Ein anfänglicher Stop wird `StopLoss` Punkte vom Einstiegspreis entfernt gesetzt. Sobald sich der Preis um `BreakEven` Punkte zu seinen Gunsten bewegt, springt der Stop auf die Gewinnschwelle. Wenn `MovingBreakEven` aktiviert ist und die konfigurierte Verzögerung verstrichen ist, verwendet der Stop die aktuellsten Stundentiefs (für Long-Positionen) oder Höchststunden (für Short-Positionen), wenn sich diese Niveaus weiter verschärfen.
5. **Sitzungsbeendigung** – Um `ExitMinute` innerhalb der konfigurierten Sitzungsstunde schließt die Strategie alle Positionen vollständig und entfernt alle ausstehenden Aufträge.

## Parameter
| Parameter | Beschreibung |
|-----------|-------------|
| `TradeVolume` | Volumen für jede Breakout-Order. |
| `NumberOfOrdersPerSide` | Menge der gestapelten ausstehenden Bestellungen für beide Richtungen. |
| `TakeProfitIncrement` | Abstand (in Punkten) zwischen aufeinanderfolgenden Take-Profit-Zielen. |
| `PipsForEntry` | Zusätzliche Punkte werden dem Breakout-Trigger über/unter dem Sitzungsbereich hinzugefügt. |
| `StopLoss` | Anfänglicher Stoppabstand vom Einstiegspreis. |
| `BreakEven` | Erforderlicher Gewinn (in Punkten), bevor der Stop die Gewinnschwelle erreicht. |
| `MovingBreakEven` | Aktiviert die gleitende Break-even-Trailing-Logik. |
| `MovingBreakEvenHoursToStart` | Verzögerung (in Stunden) nach der Referenzsitzung, bevor der gleitende Break-Even zurückbleiben kann. |
| `BrokerOffsetToGmt` | Stundenversatz zwischen Brokerzeit und GMT, der vom gleitenden Break-Even-Planer verwendet wird. |
| `TradeSession1..4` | Schaltet zwischen den vier unabhängigen Handelssitzungen um. |
| `SessionHour1..4` | Stunde (0-23), die jede Referenzsitzung definiert. |
| `ExitMinute` | Minute innerhalb der Sitzungsstunde, um Positionen zu liquidieren und Aufträge zu stornieren. |
| `CandleType` | Kerzentyp, der zur Messung der Referenzstunde verwendet wird (standardmäßig 1-Stunden-Kerzen). |

## Nutzungshinweise
- Stellen Sie sicher, dass das Instrument über einen gültigen `PriceStep` verfügt, damit die Punktwertberechnungen mit der MT4-Version übereinstimmen.
- Die Strategie geht davon aus, dass die Broker-Zeiten mit den Kerzen-Zeitstempeln übereinstimmen. Passen Sie `BrokerOffsetToGmt` an, wenn in der Vergangenheit ein anderer MT4-Server-Offset verwendet wurde.
- Beim gleitenden Break-Even werden die beiden zuletzt beendeten Stundenkerzen ausgewertet, bevor der Stop verschärft wird, was dem Verhalten des ursprünglichen Expertenberaters entspricht.
