# CloseDeleteEA-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die CloseDeleteEA-Strategie reproduziert das MetaTrader-Hilfsprogramm, das Positionen massenhaft schließt und Pending Orders entfernt. Sie durchsucht regelmäßig das ausgewählte Portfolio und sendet Marktorders oder Stornierungsanfragen gemäß benutzerdefinierten Filtern. Dadurch eignet sie sich für Notliquidationen oder Bereinigungsszenarien, wenn manuelles Ordermanagement zu langsam ist.

## Hauptfunktionen
- Schließt Long- und/oder Short-Exposure mit Marktorders.
- Storniert Pending Orders, die den konfigurierten Filtern entsprechen.
- Optionale Gewinn-/Verlustfilter, um bestimmte Positionen nicht zu berühren.
- Beschränkt den Scan auf das aktuelle Wertpapier oder verarbeitet das gesamte Portfolio.
- Filtert Positionen und Orders nach Strategiekennung.

## Parameter
| Name | Beschreibung |
| --- | --- |
| `CloseBuyPositions` | Schließt Long-Exposure, die den Filtern entspricht. |
| `CloseSellPositions` | Schließt Short-Exposure, die den Filtern entspricht. |
| `CloseMarketPositions` | Aktiviert das Modul zum Schließen von Marktpositionen. |
| `CancelPendingOrders` | Aktiviert das Stornieren von Pending Orders. |
| `CloseOnlyProfitable` | Schließt Positionen nur, wenn der aktuelle PnL nicht negativ ist. |
| `CloseOnlyLosing` | Schließt Positionen nur, wenn der aktuelle PnL nicht positiv ist. |
| `ApplyToCurrentSecurity` | Wenn true, wird nur das Strategiewertpapier durchsucht. Andernfalls werden alle Wertpapiere im Portfolio verarbeitet. |
| `TargetStrategyId` | Optionaler Filter für Strategiekennung (leerer Wert passt auf alles). |
| `TimerInterval` | Timerfrequenz für die Verwaltungsschleife. |

## Nutzungshinweise
1. Binden Sie die Strategie an einen Connector mit zugewiesenem Portfolio.
2. Konfigurieren Sie optional Filter, bevor Sie die Strategie starten.
3. Starten Sie die Strategie, um den Close/Delete-Zyklus auszulösen. Die Strategie stoppt automatisch, sobald keine passenden Positionen oder Orders verbleiben.
4. Beachten Sie, dass Stornierungsanfragen nur Orders betreffen können, die für die Strategie über den Connector sichtbar sind.

## Unterschiede zur MQL-Version
- StockSharp arbeitet mit aggregierten Positionen; daher wird individuelle Ticketkontrolle durch volumenbasiertes Nettoexposure-Management ersetzt.
- Strategie-ID-Filterung imitiert das ursprüngliche Magic-Number-Konzept.
- Visuelle Chart-Bereinigungselemente aus MetaTrader werden nicht reproduziert.
