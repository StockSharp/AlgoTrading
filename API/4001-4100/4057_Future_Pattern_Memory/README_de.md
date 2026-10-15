# Strategie FuturePatternMemoryStrategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
`FuturePatternMemoryStrategy` ist eine StockSharp-Portierung der klassischen MetaTrader-Experten **FutureMA** und **FutureMACD**. Die ursprünglichen Roboter zeichneten Sequenzen von Indikatorunterschieden in CSV-Dateien auf, verwendeten die gespeicherten Statistiken wieder und entschieden, ob die aktuellen Bedingungen bullische oder bärische Ausbrüche begünstigten. Diese C#-Version behält die gleiche Idee bei, ersetzt aber das Dateisystem durch ein In-Memory-Pattern-Warehouse und macht jeden Knopf als Strategieparameter verfügbar. Die Strategie kann entweder auf der geglätteten gleitenden Durchschnittsspanne (die FutureMA-Logik) oder auf dem MACD-Histogramm (die FutureMACD-Logik) basieren.

Die Strategie bewertet jede fertige Kerze in fünf Stufen:

1. **Indikatorprojektion** – Berechnen Sie den ausgewählten Oszillator (MA-Spreizung oder MACD-Histogramm) und normalisieren Sie ihn mit einem konfigurierbaren Skalierungsfaktor. Die Werte werden in Ganzzahlen diskretisiert, um kompakte Mustersignaturen zu erstellen.
2. **Muster-Hashing** – verwaltet ein gleitendes Fenster der neuesten `AnalysisBars` normalisierten Werte. Jedes Mal, wenn ein neuer Balken geschlossen wird, wird das Fenster in einen eindeutigen Hash-String umgewandelt, der den aktuellen Marktkontext identifiziert.
3. **Historische Swing-Analyse** – Untersuchen Sie die vorherigen `FractalDepth`-Kerzen, messen Sie den Abstand zwischen der ältesten Eröffnung und den höchsten/tiefsten Extremwerten und wandeln Sie diese Bereiche in Punkte um. Diese Distanzen sind die Belohnungserwartungen, die die ursprünglichen Roboter in ihren CSV-Dateien gesammelt haben.
4. **Gewichtete Speicheraktualisierung** – der Hash-Schlüssel wird verwendet, um einen Eintrag im Musterwörterbuch abzurufen oder zu erstellen. Die bullischen und bärischen Take-Profit-Erwartungen werden mit einem gewichteten gleitenden Durchschnitt aktualisiert, der durch `ForgettingFactor` gesteuert wird und den „Vergesslichkeits“-Koeffizienten (`zabyvaemost`) aus dem MQL-Code reproduziert.
5. **Signalauswertung und -ausführung** – wenn die bullische Erwartung dominiert, das Muster mehr als `MinimumMatches` Mal gesehen wurde und der prognostizierte Gewinn über `MinimumTakeProfit` liegt, geht die Strategie eine Long-Position ein oder erhöht diese. Der bärische Zweig funktioniert symmetrisch. Schutzniveaus werden aus den gespeicherten Statistiken abgeleitet und optional verfolgt, wenn sich der Handel positiv entwickelt.

## Konvertierungshinweise
- Beide MetaTrader-Experten werden über den Parameter `Source` zu einer konfigurierbaren Strategie zusammengeführt, sodass Sie ohne Neukompilierung zwischen der MA-basierten Engine und der MACD-basierten Engine wechseln können.
- Die dateibasierte Persistenz wurde durch ein `Dictionary<string, PatternStats>` ersetzt, das alle Statistiken während der Ausführung im Speicher behält. Dies vermeidet Datei-E/A und bleibt innerhalb des StockSharp-Sandbox-Modells.
- Das Positionsmanagement repliziert die ursprüngliche Stop-/Zielplatzierung: Der Stop nutzt den vollen durchschnittlichen Swing, während der Take-Profit `StatisticalTakeRatio` (das Original `Stat_Take_Profit`) verwendet. Wenn `EnableTrailingStop` wahr ist, wird der Stop in Viertelschritten der Gewinndistanz verschoben, genau so, wie der MQL-Experte seine Orders geändert hat.
- Der manuelle Modus (`ManualMode`) deaktiviert die automatische Auftragserteilung, sammelt jedoch weiterhin Statistiken, was dem ursprünglichen Verhalten der `Ruchnik`-Flagge entspricht.
- Das Einskalieren (`AllowAddOn`) ahmt die Flagge `dokupka` nach und ermöglicht der Strategie, das Volumen immer dann hinzuzufügen, wenn sich das Muster auf einem neuen Balken wiederholt.

## Handelslogik im Detail
- **Indikatorquelle**
  - *MA-Spread*: Berechnet zwei geglättete gleitende Durchschnitte (SMMA 6 und SMMA 24) auf Medianpreisen und verwendet deren Differenz.
  - *MACD-Histogramm*: Berechnet die Differenz zwischen der MACD-Hauptlinie und der Signallinie (Standardkonfiguration 12/26/9).
- **Normalisierung**: `NormalizationFactor` reproduziert `tocnost`; Es skaliert die Rohdifferenz, bevor es in eine ganzzahlige Signatur konvertiert wird. Die Umrechnung wird durch `100 * MinPriceStep` dividiert, um Pip-basierte Einheiten beizubehalten.
- **Musterspeicher**: Das Wörterbuch speichert für jede Signatur die Anzahl der bullischen Übereinstimmungen, die durchschnittliche bullische Distanz, die Anzahl der bärischen Übereinstimmungen und die durchschnittliche bärische Distanz. Die Werte werden mit der gewichteten Formel `(current + input * ForgettingFactor) / (1 + ForgettingFactor)` aktualisiert.
- **Eintrittsregeln**:
  - Long: bullische Erwartung ≥ bärische Erwartung, bullische Übereinstimmungen > `MinimumMatches`, erwartete bullische Distanz > `MinimumTakeProfit`.
  - Kurz: bärische Erwartung ≥ bullische Erwartung, bärische Übereinstimmungen > `MinimumMatches`, erwartete bärische Distanz > `MinimumTakeProfit`.
- **Risikomanagement**: Stop-Loss wird auf einen vollen durchschnittlichen Swing gegenüber der Position eingestellt; Take-Profit nutzt `StatisticalTakeRatio` dieses Swings. Trailing-Stops bewegen sich, nachdem der Preis ein Viertel der Strecke zurückgelegt hat, genau wie die ursprüngliche Trailing-Routine.

## Parameter
| Parameter | Beschreibung | Standard |
|-----------|-------------|---------|
| `CandleType` | Hauptzeitraum für Berechnungen. | 30-Minuten-Kerzen |
| `Source` | Wählen Sie zwischen MA-Spread (`FutureMA`) und MACD Histogramm (`FutureMACD`). | `MaSpread` |
| `FastMaLength` / `SlowMaLength` | SMMA-Längen bei `Source = MaSpread`. | 6 / 24 |
| `MacdFastLength` / `MacdSlowLength` / `MacdSignalLength` | MACD Zeiträume, wenn `Source = MacdHistogram`. | 26.12.9 |
| `AnalysisBars` | Anzahl der Balken, die die Mustersignatur bilden. | 8 |
| `FractalDepth` | Anzahl der vergangenen Kerzen, die zur Messung der Ausbruchsentfernungen verwendet werden. | 4 |
| `MinimumMatches` | Erforderliche Anzahl gespeicherter Vorkommnisse vor Abschluss eines Handels. | 5 |
| `MinimumTakeProfit` | Erwartete Mindestentfernung (in Punkten), um das Signal zu akzeptieren. | 30 |
| `NormalizationFactor` | Skalierungsfaktor, der auf die Indikatordifferenz angewendet wird. | 10 |
| `ForgettingFactor` | Auf neue Messungen im Musterspeicher angewendetes Gewicht. | 1.5 |
| `StatisticalTakeRatio` | Take-Profit-Verhältnis im Verhältnis zum gemessenen Swing. | 0,5 |
| `EnableTrailingStop` | Aktiviert eine Viertelschritt-Trailing-Stop-Logik. | `false` |
| `ManualMode` | Sammeln Sie Statistiken, aber überspringen Sie die Auftragserteilung. | `false` |
| `AllowAddOn` | Ermöglichen Sie die Skalierung, wenn sich ein Muster wiederholt. | `true` |
| `Volume` | Auftragsgröße, die für Einträge verwendet wird. | 0,1 |

## Praktische Ratschläge
- Die Strategie basiert auf diskretisierten Hashes. Wählen Sie daher `NormalizationFactor` und `AnalysisBars` sorgfältig aus: Zu große Werte erzeugen spärliche Signaturen, während zu kleine Werte unterschiedliche Zustände miteinander vermischen.
- Wenn Sie den Live-Handel ausführen, sollten Sie erwägen, das Musterwörterbuch nach der Sitzung zu exportieren, wenn Sie Beständigkeit zwischen den Läufen benötigen.
- Da die MQL-Version Daten pro Symbol/Zeitraum speicherte, wird empfohlen, eine dedizierte Strategieinstanz pro Instrument/Zeitrahmen zu behalten, um eine Kreuzkontamination der Statistiken zu vermeiden.
