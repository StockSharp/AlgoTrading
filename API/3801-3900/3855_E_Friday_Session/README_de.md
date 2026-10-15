# Strategie für die E-Freitag-Sitzung
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die E-Friday Session-Strategie repliziert den klassischen MetaTrader-Expertenberater, der nur freitags handelt. Es beobachtet die vorherige Tageskerze und eröffnet zu einer konfigurierten Stunde zu Beginn der Freitagssitzung eine Position. Die Richtung ist konträr: Wenn der Vortag unter seinem Eröffnungskurs (bärische Kerze) schloss, kauft die Strategie; Wenn der Vortag über seinem Eröffnungskurs schloss (bullische Kerze), wird die Strategie verkauft. Positionen werden untertägig verwaltet und können automatisch nach einer konfigurierbaren Stunde oder durch Schutzstopps geschlossen werden.

## Handelsregeln
1. Sammeln Sie tägliche Kerzen (Standard: 1 Tag), um die Eröffnungs- und Schlusskurse des Vortages zu erhalten.
2. Überwachen Sie freitags die Intraday-Kerzen (Standard: 1 Minute), um die konfigurierte Eintrittsstunde zu erkennen.
3. Bei der ersten Kerze der Eintrittsstunde:
   - Gehen Sie long, wenn der Vortag bärisch war.
   - Gehen Sie short, wenn der Vortag bullisch war.
   - Überspringen Sie den Handel, wenn der Vortag ein Doji war (eröffnet gleich geschlossen).
4. Optional können Sie die Position automatisch schließen, sobald die konfigurierte Ausstiegsstunde erreicht ist.
5. Verwalten Sie Exits mithilfe von Stop-Loss, Take-Profit und optionaler Trailing-Stop-Logik, die den ursprünglichen Expert Advisor nachahmt, einschließlich der Gewinnaktivierung und der Trailing-Step-Schwellenwerte.

## Implementierungshinweise
- Verwendet StockSharp Kerzenabonnements auf hoher Ebene sowohl für den täglichen Kontext als auch für das Intraday-Timing.
- Konvertiert punktbasierte Risikokontrollen aus der MQL-Version mithilfe der Preisstufe des Wertpapiers in absolute Preisversätze.
- Behält Trailing-Stops im Code bei, aktualisiert sie bei jeder abgeschlossenen Kerze und schließt die Position, wenn Preisextreme durchbrochen werden.
- Stellt nur einen Handel pro Freitag sicher, indem der tägliche Status verfolgt wird.
- Unterstützt sowohl Long- als auch Short-Einträge unter Berücksichtigung des ursprünglichen Magic-Number-Gatings durch den Handel mit einem einzelnen Symbol pro Strategieinstanz.

## Parameter
| Name | Beschreibung | Standard |
| --- | --- | --- |
| `Volume` | Handelsgröße in Losen/Kontrakten. | `0.1` |
| `StopLossPoints` | Stop-Loss-Distanz in Preisschritten (0 deaktiviert). | `75` |
| `TakeProfitPoints` | Take-Profit-Distanz in Preisschritten (0 deaktiviert). | `0` |
| `HourOpen` | Stunde des Tages (0-23), um die Position zu eröffnen. | `7` |
| `UseClosePositions` | Aktivieren Sie das automatische Schließen nach der Ausgangsstunde. | `true` |
| `HourClose` | Stunde des Tages (0-23), um die Position zu schließen, falls aktiviert. | `19` |
| `UseTrailing` | Aktivieren Sie Trailing-Stop-Anpassungen. | `true` |
| `ProfitTrailing` | Der Gewinn muss die Trailing-Distanz überschreiten, bevor das Trailing aktiviert wird. | `true` |
| `TrailingStopPoints` | Trailing-Stop-Distanz in Preisschritten. | `60` |
| `TrailingStepPoints` | Vor dem Festziehen des Trailing Stop sind zusätzliche Punkte erforderlich. | `5` |
| `IntradayCandleType` | Kerzentyp für Intraday-Timing (Standard-1-Minuten-Kerzen). | `TimeSpan.FromMinutes(1)` |
| `DailyCandleType` | Kerzentyp für die tägliche Stimmungserkennung (Standard-1-Tages-Kerzen). | `TimeSpan.FromDays(1)` |

## Nutzungstipps
- Richten Sie die Handelssitzung des Instruments so aus, dass die Einstiegsstunde am Freitag mit der gewünschten Marktöffnung übereinstimmt.
- Wenn Sie Stop-Loss- und Trailing-Werte konfigurieren, drücken Sie diese in denselben „Punkten“ aus, die von der Preisstufe des Symbols verwendet werden, um das Verhalten von MetaTrader zu reproduzieren.
- Die Strategie ist auf einen einzelnen Trade pro Freitag ausgelegt. Um mit mehreren Symbolen zu handeln, führen Sie für jedes Symbol separate Strategieinstanzen aus.

## Unterschiede zum Original EA
- Verwendet Kerzenschlussdaten für die Entscheidungsfindung, während die ursprünglich abgefragten Preise pro Tick verwendet werden.
- Schutzausstiege werden über Marktaufträge ausgeführt, wenn Kerzen anzeigen, dass Stop- oder Zielniveaus innerhalb des Intervalls berührt wurden.
- Strategieparameter werden über das `StrategyParam`-System von StockSharp bereitgestellt und unterstützen die Optimierung und UI-Bindung.
