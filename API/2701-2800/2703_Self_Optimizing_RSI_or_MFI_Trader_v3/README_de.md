# Selbstoptimierender RSI- oder MFI-Trader v3
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Diese Strategie portiert den MetaTrader Expert Advisor "Self Optimizing RSI or MFI Trader" auf die High-Level-API von StockSharp. Bei jeder fertigen Kerze testet der Algorithmus ein gleitendes Fenster historischer Balken und findet die profitabelsten Überkauf- und Überverkauf-Schwellenwerte für den gewählten Oszillator. Live-Trades werden nur eingegangen, wenn der aktuelle Oszillatorwert den besten Schwellenwert in der gleichen Richtung wie der historische Vorteil kreuzt, optional ohne eine Kreuzung im "aggressiven" Modus zu erfordern. Positionsausstiege basieren auf ATR-basierten oder Festabstand-Stops und -Zielen mit einem optionalen Breakeven-Schritt.

## Marktdaten
- Funktioniert mit jedem Instrument, das OHLC-Kerzen und Volumen bereitstellt (MFI benötigt Volumen).
- Verwendet den durch den `CandleType`-Parameter angegebenen Zeitrahmen. Standard sind 15-Minuten-Kerzen, aber Sie können jeden vom Venue-Adapter unterstützten Zeitrahmen anhängen.

## Indikatoren
- **Relative Strength Index (RSI)** oder **Money Flow Index (MFI)** abhängig vom `IndicatorChoice`-Parameter. Beide teilen die gleiche Durchschnittslänge.
- **Average True Range (ATR)** für ATR-basierte Stop-Loss-/Take-Profit-Dimensionierung wenn `UseDynamicTargets` aktiviert ist.

## Handelslogik
1. Eine rollende Historie von `OptimizingPeriods` + 1 fertigen Kerzen mit ihren Oszillatorwerten und Schlusspreisen pflegen.
2. Für jedes ganzzahlige Level zwischen `IndicatorBottomValue` und `IndicatorTopValue` simuliert die Strategie Trades im historischen Fenster:
   - Short-Simulation: zählen wie oft der Oszillator unter das Level gekreuzt ist und ob ein Short-Stop-Loss oder Take-Profit zuerst getroffen worden wäre.
   - Long-Simulation: zählen wie oft der Oszillator über das Level gekreuzt ist und wie profitabel die Trades gewesen wären.
3. Den Schwellenwert wählen, der die höchste simulierte Profitabilität für jede Richtung geliefert hat. Wenn `TradeReverse` aktiviert ist, werden die Profitabilitätspunkte vertauscht, so dass die entgegengesetzte Richtung bevorzugt wird.
4. Wenn der Live-Oszillator das beste Level in der profitablen Richtung kreuzt (oder sofort wenn `UseAggressiveEntries` wahr ist) öffnet die Strategie eine Position unter Berücksichtigung von `OneOrderAtATime`.
5. Ausstiegsmanagement:
   - Stop-Loss- und Take-Profit-Level werden entweder aus ATR-Vielfachen (`StopLossAtrMultiplier`, `TakeProfitAtrMultiplier`) oder aus Festpunkt-Abständen (`StaticStopLossPoints`, `StaticTakeProfitPoints`) berechnet.
   - `UseBreakEven` verschiebt den Stop auf den Einstiegspreis plus `BreakEvenPaddingPoints`, sobald der unrealisierte Gewinn `BreakEvenTriggerPoints` erreicht.
   - Positionen werden geschlossen, wenn entweder Stop-Loss- oder Take-Profit-Preise gekreuzt werden.

## Risikomanagement
- **Dynamische Dimensionierung:** wenn `UseDynamicVolume` wahr ist, riskiert die Strategie `RiskPercent` des aktuellen Portfoliowerts. Die Berechnung konvertiert den Stop-Abstand in monetäres Risiko unter Verwendung von `PriceStep` und `StepPrice` des Wertpapiers.
- **Statische Dimensionierung:** wenn deaktiviert, werden `BaseVolume` Lots bei jedem Einstieg gehandelt.
- **Breakeven-Schutz:** stellt sicher, dass gewinnende Trades geschützt werden, sobald ausreichend Gewinn aufgelaufen ist.

## Parameter
| Parameter | Beschreibung |
|-----------|-------------|
| `OptimizingPeriods` | Anzahl der Balken für die rollende In-Sample-Optimierung (Standard 144). |
| `IndicatorChoice` | Wählt RSI oder MFI als treibenden Oszillator. |
| `IndicatorPeriod` | Durchschnittszeitraum für Oszillator und ATR. |
| `IndicatorTopValue` / `IndicatorBottomValue` | Suchgrenzen für Schwellenwert-Level (typischerweise 0–100). |
| `UseAggressiveEntries` | Wenn wahr, erlaubt Einstiege ohne bestätigte Kreuzung. |
| `TradeReverse` | Tauscht Profitabilitätspunkte um, um die historisch verlierende Seite zu handeln. |
| `OneOrderAtATime` | Verhindert das Öffnen einer neuen Position während eine andere aktiv ist. |
| `UseDynamicTargets` | Wechselt zwischen ATR-basierten und Festpunkt-Stops/-Zielen. |
| `StopLossAtrMultiplier`, `TakeProfitAtrMultiplier` | ATR-Multiplikatoren für dynamische Ausstiege. |
| `StaticStopLossPoints`, `StaticTakeProfitPoints` | Punktabstände für feste Ausstiege. |
| `UseBreakEven`, `BreakEvenTriggerPoints`, `BreakEvenPaddingPoints` | Konfiguriert das Breakeven-Stop-Verhalten. |
| `UseDynamicVolume`, `RiskPercent`, `BaseVolume` | Steuert die Positionsgrößenlogik. |
| `CandleType` | Zeitrahmen für Optimierung und Handel. |

## Implementierungshinweise
- Die Strategie verwendet die `SubscribeCandles().Bind(...)`-Pipeline von StockSharp, läuft also nur auf abgeschlossenen Kerzen.
- `OneOrderAtATime` sollte beim Handel in einem Netting-Konto aktiviert bleiben, da die Implementierung eine einzelne aggregierte Position verfolgt.
- ATR-basierte Ausstiege erfordern einen gültigen ATR-Wert; die Strategie überspringt den Handel, bis der Indikator vollständig gebildet ist.
- Bei Verwendung von MFI sicherstellen, dass der Datenfeed Volumen liefert, sonst gibt der Indikator null zurück und es werden keine Trades generiert.

## Optimierungstipps
- `OptimizingPeriods`, Oszillator-Periode und ATR-Multiplikatoren gemeinsam optimieren, um das Volatilitätsregime des Instruments anzupassen.
- Verschiedene Assets können von engeren Level-Bereichen profitieren (z.B. 20–80) um Rauschen zu reduzieren.
- Walk-Forward-Analyse für Vorwärtstests erwägen, da die Strategie Schwellenwerte kontinuierlich anpasst.

## Verwendung
1. Strategie einem Connector im Designer hinzufügen oder programmatisch ausführen.
2. Gewünschtes Wertpapier, Portfolio und Parameterwerte setzen.
3. Strategie starten; sie beginnt mit dem Handel sobald genug Kerzen für die Optimierung angesammelt sind.

## Einschränkungen
- Historische Optimierung erfolgt auf jedem Balken und kann bei sehr großen `OptimizingPeriods` oder breiten Level-Bereichen CPU-intensiv sein.
- Da Level ganzzahlig sind, werden feinkörnige Schwellenwerte (z.B. 70.5) nicht getestet.
- Der Ansatz setzt voraus, dass die jüngste Vergangenheit prädiktiv bleibt; plötzliche Regimewechsel können die Leistung beeinträchtigen, also Live-Ergebnisse überwachen und Konfiguration bei Bedarf anpassen.
