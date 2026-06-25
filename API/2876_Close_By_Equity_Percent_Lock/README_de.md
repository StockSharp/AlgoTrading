# Equity-Prozent-Sicherungsstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
- **Kategorie**: Risikomanagement / Kontoebenen-Automatisierung.
- **Ursprüngliche Quelle**: MQL5-Expertenberater "Close by Equity Percent" (#20880).
- **Zweck**: Das Konto-Equity gegenüber dem letzten Flat-Saldo überwachen und alle offenen Positionen liquidieren, sobald das Equity auf ein konfigurierbares Vielfaches dieses Saldos wächst.
- **Instrumente**: Alle bereits von anderen Strategien oder manuellen Händlern innerhalb desselben Portfolios gehandelten Wertpapiere.

## Kernidee
Der ursprüngliche MQL-Expertenberater vergleicht das aktuelle Konto-Equity mit dem Kontosaldo (der sich nur nach dem Schließen von Positionen ändert). Wenn das Equity `Balance * EquityPercentFromBalance` erreicht oder überschreitet, schließt das Skript alle offenen Positionen, um Gewinne zu sichern. Dieser StockSharp-Port behält dieselbe Kontoschutzlogik bei und integriert sich dabei in die High-Level-Strategie-API.

## Funktionsweise
1. Wenn die Strategie startet, macht sie eine Momentaufnahme des aktuellen Portfolio-Werts. Diese dient als "Saldo"-Referenz, während das Konto flat ist.
2. Die Strategie abonniert 1-Minuten-Kerzen (konfigurierbar über `CandleType`) für das konfigurierte `Security`. Der Kerzendatenstrom wird nur als Timer zur Auslösung von Equity-Prüfungen verwendet.
3. Bei jeder abgeschlossenen Kerze:
   - Wenn alle Positionen flat sind, wird die Saldo-Momentaufnahme auf den neuesten Portfolio-Wert aktualisiert.
   - Das aktuelle Equity (`Portfolio.CurrentValue`) wird mit `balanceSnapshot * EquityPercentFromBalance` verglichen.
   - Wenn das Equity den Schwellenwert erreicht oder überschreitet, wird jede offene Position im Portfolio über `ClosePosition(position.Security)` geschlossen.
4. Die Saldo-Momentaufnahme wird erneut aktualisiert, sobald alle Positionen geschlossen sind, und der Zyklus kann neu starten.

## Parameter
| Name | Typ | Standard | Beschreibung |
| ---- | --- | -------- | ------------ |
| `EquityPercentFromBalance` | decimal | 1.20 | Equity-Vielfaches, das erreicht werden muss, bevor alle Positionen liquidiert werden. Wert `1.20` bedeutet "alles schließen, wenn das Equity 120% des letzten Flat-Saldos beträgt". |
| `CandleType` | `DataType` | 1-Minuten-Zeitrahmen-Kerze | Datenstrom, der ausschließlich zur Auslösung periodischer Equity-Prüfungen verwendet wird. Anpassen, um der gewünschten Überwachungskadenz zu entsprechen. |

## Implementierungshinweise
- Verwendet `Strategy.ClosePosition(Security)` für jede offene Position und spiegelt damit die `PositionClose`-Schleife in der MQL-Version wider.
- Verfolgt die Saldo-Momentaufnahme nur nach dem Schließen aller Positionen und reproduziert damit, wie das MQL-Skript auf `AccountBalance` angewiesen war (das sich nach dem Schließen von Positionen aktualisiert).
- Die Strategie ist auf Kontoebene: Sie eröffnet keine Positionen selbst und versucht, **alle** Positionen im verbundenen Portfolio unabhängig vom Symbol zu schließen.
- Erfordert, dass sowohl `Portfolio` als auch `Security` vor dem Start zugewiesen sind. Das Wertpapier wird nur verwendet, um Kerzen zu abonnieren, die Timing-Ereignisse liefern.

## Verwendungsrichtlinien
1. Die Strategie an das zu schützende Portfolio anhängen und das `Security` festlegen, dessen Kerzendatenstrom als Timer verwendet werden soll (z. B. ein hochliquides Instrument).
2. `EquityPercentFromBalance` auf das Gewinnmitnahme-Vielfache anpassen, das zum Risikoplan passt.
3. Strategie starten. Wann immer das Equity das angegebene Vielfache des letzten Flat-Saldos erreicht, werden alle offenen Positionen im Portfolio automatisch geschlossen.
4. Nach der Liquidation wird die Saldo-Momentaufnahme aktualisiert, sodass der nächste Gewinezyklus wieder darauf wartet, dass das Equity um den konfigurierten Prozentsatz wächst, bevor eine weitere Schließung ausgelöst wird.

## Praktisches Beispiel
- Anfangs-Saldo-Momentaufnahme = 10.000 USD.
- `EquityPercentFromBalance = 1.2` → Ziel-Equity = 12.000 USD.
- Offene Positionen wachsen, bis das Equity 12.050 USD erreicht.
- Strategie schließt alle offenen Positionen; Saldo-Momentaufnahme aktualisiert sich, sobald das Portfolio flat ist (z. B. auf 12.000 USD).
- Der nächste Zyklus wartet, bis das Equity 12.000 * 1.2 = 14.400 USD überschreitet, bevor er wieder handelt.
