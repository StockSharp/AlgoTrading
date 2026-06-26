# Risk Reward Ratio-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Die **Risk Reward Ratio-Strategie** ist ein High-Level-StockSharp-Port des MetaTrader-Experten "Risk Reward Ratio". Die Strategie kombiniert mehrere Momentum- und Trendbestätigungsfilter mit einem disziplinierten Risikomanagementmodul. Einstiege werden aus einer Kombination von stochastischen Oszillatoren, einem LWMA-Crossover (Linear Weighted Moving Average), einem 14-Perioden-RSI-Filter und einer MACD-Trendprüfung generiert. Die Risikosteuerung erfolgt durch einen pip-basierten Stop-Loss, einen automatischen Reward-Ratio-Take-Profit, optionale Trailing Stops und Break-Even-Logik sowie einen Notfallschalter, der die Position sofort liquidiert.

Die Konvertierung behält den ursprünglichen Charakter des MetaTrader-Experten bei und verwendet StockSharp's Kerzen-Subscriptions und Indikator-Binding-APIs. Die gesamte Indikatorverarbeitung erfolgt auf abgeschlossenen Kerzen und vermeidet den direkten Zugriff auf Indikatorbuffer, wodurch das Streaming-Paradigma der Engine erhalten bleibt.

## Trading-Logik
1. **Stochastische Konfluenz**
   * Ein *schneller* Stochastik (5, 2, 2) liefert das primäre Momentumsignal über die %K-Linie.
   * Ein *langsamer* Stochastik (21, 10, 4) liefert den Richtungsbias über seine geglättete %D-Linie.
   * Long-Setups erfordern, dass der schnelle %K über dem langsamen %D liegt, Short-Setups das Gegenteil.
2. **RSI-Bestätigung**
   * Ein 14-Perioden-RSI muss für Long-Trades über 50 und für Short-Trades unter 50 liegen, um sicherzustellen, dass der Markt mit der vorgeschlagenen Richtung übereinstimmt.
3. **Trendfilter via LWMAs**
   * Zwei linear gewichtete gleitende Durchschnitte (Längen 6 und 85) müssen die Richtung bestätigen: schneller LWMA über dem langsamen für Longs und darunter für Shorts.
4. **MACD-Trendqualifikator**
   * Das MACD-Histogramm (12, 26, 9) muss mit der Signalrichtung übereinstimmen. Die Hauptlinie muss die Signallinie anführen und auf der entsprechenden Seite der Nulllinie bleiben.
5. **Momentum-Abweichungsfilter**
   * Ein 14-Perioden-Momentum-Indikator misst die Distanz von 100. Mindestens eine der letzten drei Momentum-Readings muss den konfigurierbaren Schwellenwert überschreiten, um zu beweisen, dass der Preis genug beschleunigt, um einen Trade zu rechtfertigen.
6. **Positionslimits**
   * Das Netto-Exposure wird durch `MaxPositions * TradeVolume` begrenzt, sodass die Strategie nicht über die ursprüngliche EA-Beschränkung hinaus pyramidieren kann.

Aufträge werden als Market-Ausführungen mit `BuyMarket` und `SellMarket` gesendet. Die Strategie ignoriert unfertige Kerzen und hält den gesamten Zustand in Klassenfeldern, um die ereignisgesteuerte Architektur von StockSharp zu respektieren.

## Risikomanagement
* **Stop-Loss in Pips** – Jeder Einstieg installiert einen Schutz-Stop bei `StopLossPips * PriceStep` vom Fill-Preis entfernt.
* **Reward-Ratio-Take-Profit** – Die Take-Profit-Distanz entspricht der Stop-Distanz multipliziert mit `RewardRatio`, um ein festes Gewinn-Risiko-Verhältnis zu erhalten.
* **Trailing Stop** – Wenn aktiviert, bewegt sich der Stop hinter dem Preis um `TrailingStopPips`, sobald der Markt mindestens diese Distanz vom Einstieg vorgerückt ist.
* **Break-Even-Verschiebung** – Nach `BreakEvenTriggerPips` günstiger Bewegung wird der Stop zum Einstieg plus einem zusätzlichen `BreakEvenOffsetPips`-Puffer verschoben (oder minus für Shorts), was Gewinne sichert.
* **Notfallschalter** – Das Setzen von `ExitSwitch` auf `true` schließt die aktuelle Position bei der nächsten abgeschlossenen Kerze und deaktiviert die weitere Verarbeitung, bis die Flagge zurückgesetzt wird.

## Parameter
| Name | Standard | Beschreibung |
| --- | --- | --- |
| `TradeVolume` | `0.1` | Volumen jeder Market-Order. |
| `CandleType` | `15m` Zeitrahmen | Primäre Kerzenserie. |
| `FastMaPeriod` | `6` | Periode des schnellen LWMA. |
| `SlowMaPeriod` | `85` | Periode des langsamen LWMA. |
| `MomentumThreshold` | `0.3` | Minimaler absoluter Abstand des Momentum-Indikators von 100, der für Einstiege erforderlich ist. |
| `RewardRatio` | `2` | Take-Profit-Vielfaches relativ zum Stop-Loss. |
| `StopLossPips` | `20` | Stop-Loss-Distanz in Pips (PriceStep-Vielfache). |
| `MaxPositions` | `10` | Maximale Anzahl gleichzeitig erlaubter Volumeneinheiten (`TradeVolume`). |
| `EnableTrailing` | `true` | Aktiviert pip-basierte Trailing-Stop-Updates. |
| `TrailingStopPips` | `40` | Trailing-Stop-Distanz in Pips. |
| `EnableBreakEven` | `true` | Aktiviert das Break-Even-Stop-Management. |
| `BreakEvenTriggerPips` | `30` | Gewinn (in Pips) erforderlich, bevor der Stop auf Break-Even verschoben wird. |
| `BreakEvenOffsetPips` | `30` | Zusätzlicher Pip-Offset beim Verschieben des Stops auf Break-Even. |
| `ExitSwitch` | `false` | Zwingt die Strategie, das gesamte Exposure bei der nächsten abgeschlossenen Kerze zu schließen. |

## Arbeitsablauf
1. Konfigurieren Sie das gewünschte Instrument und die Kerzenserie, dann setzen Sie die Risikoparameter.
2. Starten Sie die Strategie. Sie abonniert Kerzen, bindet Indikatoren und beginnt mit der Verarbeitung abgeschlossener Bars.
3. Wenn die Einstiegsbedingungen übereinstimmen, sendet die Engine eine Market-Order und speichert Stop/Ziel-Niveaus.
4. Bei jeder abgeschlossenen Kerze bewertet der Risikoblock Trailing-, Break-Even- und Notfallausgangsregeln.
5. Ausstiege werden durch Erreichen von Stop/Take-Profit-Niveaus, Trailing-Updates, Break-Even-Anpassungen oder den Notfallschalter ausgelöst.

## Hinweise
* Die Konvertierung nutzt StockSharp's Indikator-Binding statt manuellem Buffer-Zugriff, wodurch jeder Indikator synchronisierte Daten erhält.
* Alle Berechnungen basieren auf dem `PriceStep` des Instruments. Wenn der Schritt null oder fehlend ist, werden Risikoabstände deaktiviert, um ungültige Preisniveaus zu vermeiden.
* Die Strategie modifiziert keine ausstehenden Orders; sie sendet einfach Market-Orders zum Öffnen/Schließen von Positionen, was die Art widerspiegelt, wie der ursprüngliche EA Exposure schloss, wenn Schwellenwerte erreicht wurden.
