# Fortrader 10-Pips-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Fortrader 10-Pips-Strategie** ist eine StockSharp-Portierung des MetaTrader 4-Expertenberaters `10pips.mq4` (Strategie-ID 8074). Der Roboter hält gleichzeitig eine Long- und eine Short-Position offen. Für jedes Segment werden feste Take-Profit-, Stop-Loss- und Trailing-Stop-Abstände verwendet, die in Symbolpunkten gemessen werden.

Diese Konvertierung stellt das Absicherungsverhalten innerhalb der übergeordneten Ebene API von StockSharp wieder her. Unmittelbar nach Beginn der Strategie wird eine Marktkauf- und eine Marktverkaufsorder gesendet. Immer wenn eine Schutzorder ein Bein schließt, eröffnet die Strategie sofort eine neue Order in die gleiche Richtung und hält so jederzeit zwei gegensätzliche Positionen am Leben.

## Parameter
| Name | Beschreibung |
| --- | --- |
| `Take Profit Buy` | Take-Profit-Distanz für die lange Strecke, in Punkten. |
| `Stop Loss Buy` | Stop-Loss-Distanz für die lange Strecke, in Punkten. |
| `Trailing Stop Buy` | Trailing-Stop-Distanz für den langen Abschnitt, in Punkten. Auf Null setzen, um das Nachziehen zu deaktivieren. |
| `Take Profit Sell` | Take-Profit-Distanz für die kurze Strecke, in Punkten. |
| `Stop Loss Sell` | Stop-Loss-Distanz für das kurze Bein, in Punkten. |
| `Trailing Stop Sell` | Trailing-Stop-Distanz für den kurzen Abschnitt, in Punkten. Auf Null setzen, um das Nachziehen zu deaktivieren. |
| `Volume` | Volumen jeder Marktorder in Lots. |

Alle Entfernungen werden mit dem `PriceStep` des Instruments multipliziert, um Punkte in absolute Preiswerte umzurechnen. Jeder Parameter wird über `StrategyParam<T>` verfügbar gemacht, sodass die Strategie über die GUI angepasst oder optimiert werden kann.

## Handelslogik
1. **Startup** – `OnStarted` abonniert Level-1-Daten, um die aktuell besten Geld- und Briefkurse zu verfolgen. Die Strategie sendet sofort einen Marktkauf- und einen Marktverkaufsauftrag.
2. **Schutzaufträge** – Nach jeder Eintragserfüllung (`OnNewMyTrade`) erstellt die Strategie die zugehörigen Stop-Loss- und Take-Profit-Aufträge, wenn die Abstände größer als Null sind. Bestellungen werden auf die nächste Preisstufe gerundet.
3. **Wiedereintritt** – Wenn eine Stop-Loss- oder Take-Profit-Order ausgeführt wird, wird das geschlossene Segment sofort mit einer neuen Marktorder wieder geöffnet, sodass das bidirektionale Engagement bestehen bleibt.
4. **Trailing Stops** – Level-1-Aktualisierungen lösen `UpdateTrailingStops` aus, der die Stop-Loss-Orders immer dann anpasst, wenn sich das aktuelle Geld/Brief über die konfigurierte Trailing-Distanz vom Einstiegspreis hinaus bewegt hat. Die Logik spiegelt die ursprüngliche EA wider: Das Trailing beginnt, sobald der Gewinn die Trailing-Distanz überschreitet, und Stops werden nur in Richtung des Gewinns verschoben.

## Implementierungshinweise
- Der ursprüngliche MT4-Code wartete 10 Sekunden zwischen den ersten Kauf- und Verkaufsaufträgen. Für StockSharp ist diese Verzögerung nicht erforderlich, daher werden beide Bestellungen sofort gesendet.
- Da StockSharp standardmäßig Nettopositionen verwendet, kann eine echte Absicherung davon abhängen, dass der Broker/Connector gegensätzliche Positionen unterstützt. Die Strategie verfolgt jede Etappe unabhängig und stellt sie nach jedem Ausstieg wieder her.
- `StartProtection()` wird einmal während `OnStarted` aufgerufen, sodass globale Risikoschutzmaßnahmen aktiv sind, sofern in den Framework-Einstellungen konfiguriert.

## Nutzungstipps
- Stellen Sie sicher, dass der ausgewählte Connector gleichzeitige Long- und Short-Positionen unterstützt, wenn das Absicherungsverhalten erforderlich ist.
- Setzen Sie die Nachlaufdistanzen auf Null, um das Nachlaufen für den entsprechenden Abschnitt zu deaktivieren.
- Optimieren Sie die Risikoparameter (`Take Profit`, `Stop Loss`, `Trailing Stop`) für historische Daten, damit sie zum gehandelten Symbol und Zeitrahmen passen.
