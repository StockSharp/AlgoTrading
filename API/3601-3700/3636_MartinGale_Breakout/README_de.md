# MartinGale Breakout-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **MartinGale Breakout Strategy** ist ein Breakout-Folgesystem, das aus dem MetaTrader 4-Expertenberater *MartinGaleBreakout* abgeleitet wurde. Der ursprüngliche Roboter betritt Positionen, nachdem er ungewöhnlich große Kerzen erkannt hat, und wendet einen Wiederherstellungsmechanismus im Martingal-Stil an, um frühere Verluste auszugleichen. Dieser StockSharp-Port reproduziert das Verhalten mithilfe der übergeordneten Strategie API mit Kerzenabonnements und Geldverwaltungsparametern.

Die Strategie überwacht eine konfigurierbare Kerzenreihe und sucht nach Kerzen, deren Spanne mindestens dreimal größer ist als die durchschnittliche Spanne der vorherigen zehn Balken. Wenn eine solche Kerze stark in eine Richtung schließt, eröffnet die Strategie eine Marktposition in dieser Richtung. Wenn die Position mit einem Verlust geschlossen wird, der einen konfigurierbaren Schwellenwert überschreitet, erhöht der Wiederherstellungsmodus die Take-Profit-Distanz, um den realisierten Drawdown auszugleichen.

## Handelslogik
1. Abonnieren Sie die ausgewählte Kerzenserie (standardmäßig 15-Minuten-Kerzen).
2. Behalten Sie die letzten 11 abgeschlossenen Kerzen bei, um die abnormale Volatilität zu bewerten.
3. Erkennen Sie einen bullischen Ausbruch, wenn:
   - Die aktuelle Kerze ist dreimal größer als die durchschnittliche Spanne der vorherigen zehn Kerzen.
   - Die Kerze schließt in der oberen Hälfte ihrer Spanne.
4. Erkennen Sie einen rückläufigen Ausbruch anhand der symmetrischen Bedingungen.
5. Eröffnen Sie eine Marktposition in Ausbruchsrichtung, wenn:
   - Derzeit ist keine weitere Stelle offen.
   - Das geschätzte Kapitalrisiko liegt unter dem konfigurierten Saldoprozentsatz.
6. Schließen Sie Positionen und setzen Sie Gewinn-/Verlustziele zurück, wenn:
   - Der variable Gewinn erreicht die Take-Profit-Schwelle.
   - Der schwebende Verlust erreicht die Stop-Loss-Schwelle.
7. Wenn ein Stop-Loss auftritt, wechseln Sie in den Wiederherstellungsmodus:
   - Erhöhen Sie die Take-Profit-Distanz um den konfigurierten Multiplikator.
   - Erweitern Sie das Stop-Loss-Limit auf den maximal zulässigen Prozentsatz.
   - Setzen Sie den Handel fort, bis das nächste Ziel erreicht ist, und setzen Sie dann auf die Basiskonfiguration zurück.

## Parameter
| Name | Beschreibung | Standard |
| --- | --- | --- |
| `TakeProfitPoints` | Basis-Take-Profit-Distanz, ausgedrückt in Instrumentenpunkten. | `50` |
| `BalancePercentageAvailable` | Maximaler Anteil des Kontostandes, der einem einzelnen Trade zugeordnet werden kann. | `50%` |
| `TakeProfitBalancePercent` | Zielgewinn als Prozentsatz des Kontostands. | `0.1%` |
| `StopLossBalancePercent` | Maximaler Drawdown vor Auslösung der Erholung. | `10%` |
| `StartRecoveryFactor` | Teil des Stop-Loss, der vor der Aktivierung des Wiederherstellungsmodus verwendet wird. | `0.2` |
| `TakeProfitPointsMultiplier` | Multiplikator, der während der Erholung auf die Take-Profit-Distanz angewendet wird. | `1` |
| `CandleType` | Kerzenserien, die für Ausbruchsberechnungen verwendet werden. | `15-minute` |

## Positionsgrößenbestimmung und Risikokontrolle
- Die Strategie berechnet anhand der Tick-Größe und des Tick-Werts des Instruments das erforderliche Volumen, um den konfigurierten monetären Take-Profit zu erzielen.
- Die Volumina werden auf Austauschbeschränkungen (Schritt, Minimum, Maximum) normalisiert.
- Das geschätzte Kapitalrisiko darf den konfigurierten Saldoprozentsatz nicht überschreiten.
- Der Wiederherstellungsmodus erweitert das Take-Profit-Ziel nach einem Verlust dynamisch und emuliert dabei das ursprüngliche Martingal-Verhalten, während die Positionen auf einen einzigen offenen Trade beschränkt bleiben.

## Notizen
- Die Strategie basiert auf Informationen zum Portfoliogleichgewicht. Initialisieren Sie es vor dem Start mit einer Portfolio-Verbindung.
- Die Provisionsabwicklung spiegelt die ursprüngliche EA wider, indem sie sich auf variable Gewinne und Verluste konzentriert, die aus der aktuellen Position abgeleitet werden.
- Es werden keine ausstehenden Aufträge verwendet – Ein- und Ausstiege werden nur mit Marktaufträgen durchgeführt.
