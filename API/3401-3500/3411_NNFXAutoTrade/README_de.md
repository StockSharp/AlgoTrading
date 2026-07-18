# NNFX Autohandelsstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **NNFX Auto Trade Strategy** repliziert den Risikodimensionierungs- und Management-Workflow des ursprünglichen NNFX MetaTrader 4-Panels innerhalb von StockSharp. Anstelle einer grafischen Oberfläche stellt die Strategie manuelle Befehle über Parameter bereit. Händler können Long- oder Short-Einträge anfordern, das Engagement sofort abflachen oder Breakeven- und Trailing-Logik anwenden, die den Expertenberater widerspiegelt.

Hauptmerkmale:

- ATR-gesteuerte Volatilitätsgrößenbestimmung mit einer optionalen Überschreibung für manuelle Stop- und Take-Profit-Distanzen.
- Positionseinträge sind in zwei Teile aufgeteilt: einen mit einem prognostizierten Ziel und einen Läufer, der für die diskretionäre Verwaltung offen bleibt.
- Breakeven- und Trailing-Befehle werden bei Bedarf ausgeführt und aktualisieren die gespeicherten Stop-Level, ohne automatisch bei jedem Balken ausgelöst zu werden.
- Bei der Berechnung des monetären Risikos kann zusätzliches Kapital einbezogen werden, das dem Verhalten des MQL-Skripts entspricht.

## Handelslogik
1. **ATR-Sammlung** – Die Strategie abonniert den konfigurierten Kerzentyp und verarbeitet einen Average True Range-Indikator. Wenn `UsePreviousDailyAtr` aktiviert ist, kopiert es den ATR-Wert des Vortages während der ersten 12 Stunden des neuen Handelstages und imitiert so das ursprüngliche Skript.
2. **Risikobasierte Dimensionierung** – Bei einem manuellen `Buy`- oder `Sell`-Befehl berechnet die Engine das monetäre Risiko pro Einheit anhand der Schutzstoppdistanz und wandelt den gewünschten Risikoprozentsatz in ein ausführbares Volumen um.
3. **Positionssplit** – Das Einstiegsvolumen wird in zwei Hälften geteilt. Die erste Hälfte wird automatisch liquidiert, wenn das projizierte Ziel erreicht wird, während die zweite Hälfte so lange verbleibt, bis der Händler weitere Befehle erteilt.
4. **Stopp-Handhabung** – Die anfänglichen Stopps werden intern gespeichert und bei jeder fertigen Kerze ausgewertet. Manuelle Befehle können den Stopp auf die Gewinnschwelle bringen oder ihn gemäß der NNFX-Trailing-Formel erhöhen.
5. **Ausstiegskontrollen** – `CloseAll` glättet das Buch sofort, während Stop-Verletzungen oder Teilziele Marktausstiege auslösen, die die berechneten Volumina respektieren.

## Parameter
| Parameter | Standard | Beschreibung |
|-----------|---------|-------------|
| `RiskPercent` | `2.0` | Prozentsatz des Kontokapitals (plus `AdditionalCapital`), das pro Trade riskiert wird. |
| `AdditionalCapital` | `0` | Bei der Größenbestimmung von Positionen wird der Eigenkapitalbasis zusätzliches Kapital hinzugefügt. |
| `UseAdvancedTargets` | `false` | Ändert die Risikoentfernungen von ATR-Vielfachen auf manuelle Pip-Werte. |
| `AdvancedStopPips` | `0` | Stoppdistanz in Pips, wenn der erweiterte Modus aktiv ist. |
| `AdvancedTakeProfitPips` | `0` | Zielentfernung in Pips für den teilweisen Ausgang, wenn der erweiterte Modus aktiv ist. |
| `UsePreviousDailyAtr` | `true` | Kopiert den vorherigen Tageswert ATR während der ersten 12 Stunden eines neuen Tages. |
| `AtrPeriod` | `14` | ATR Lookback-Länge. |
| `AtrStopMultiplier` | `1.5` | Bei der Berechnung der Stoppentfernung wird ein Multiplikator auf ATR angewendet. |
| `AtrTakeProfitMultiplier` | `1.0` | Bei der Berechnung der Take-Profit-Distanz wird ein Multiplikator auf ATR angewendet. |
| `CandleType` | `1 Minute` | Kerzentyp, der für ATR und die Preisüberwachung verwendet wird. |
| `BuyCommand` | `false` | Manuelles Flag – auf `true` gesetzt, um einen langen Eintrag anzufordern. Wird automatisch zurückgesetzt. |
| `SellCommand` | `false` | Manuelles Flag – auf `true` gesetzt, um einen kurzen Eintrag anzufordern. Wird automatisch zurückgesetzt. |
| `BreakevenCommand` | `false` | Manuelle Flagge – Bewegen Sie den Schutzstopp auf den Einstiegspreis. Wird automatisch zurückgesetzt. |
| `TrailingCommand` | `false` | Manuelle Markierung – wenden Sie die NNFX-Nachlaufformel einmal an. Wird automatisch zurückgesetzt. |
| `CloseAllCommand` | `false` | Manuelle Markierung – alle offenen Positionen sofort schließen. Wird automatisch zurückgesetzt. |

## Nutzungshinweise
- Die Strategie erfordert ein verbundenes Portfolio und Sicherheit mit gültigen `Step`-, `StepPrice`- und `VolumeStep`-Metadaten für genaue Risikoberechnungen.
- Befehle werden für fertige Kerzen ausgewertet, daher muss nach dem Umschalten eines manuellen Parameters ein neuer Balken (oder eine Kerzenaktualisierung) empfangen werden.
- Wenn Sie erweiterte Entfernungen verwenden, stellen Sie sicher, dass sowohl `AdvancedStopPips` als auch `AdvancedTakeProfitPips` ausgefüllt sind. andernfalls bleiben die ATR-basierten Standardeinstellungen in Kraft.
