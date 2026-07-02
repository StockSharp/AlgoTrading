# Trailing Stop FrCnSar-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die Trailing Stop FrCnSar-Strategie portiert das MetaTrader-Toolkit, das als **TrailingStopFrCnSARen_v4.mq4** und **OrderBalansEN_v3_4.mq4** geliefert wird. Der Fachberater verwaltete bestehende Positionen, indem er ihre Stop-Losses mithilfe verschiedener Techniken anpasste (vorherige Kerzen, Fraktale, Preisgeschwindigkeit oder Parabolic SAR), während der Begleitindikator den aktuellen Kontostand und offene Aufträge anzeigte. Die StockSharp-Konvertierung konzentriert sich auf Nettopositionen und implementiert die abschließende Logik mit API-Grundelementen auf hoher Ebene erneut. Es bietet außerdem einen optionalen Auftragszusammenfassungslogger, sodass die Informationsüberlagerung des ursprünglichen Indikators in Textform verfügbar bleibt.

Die Strategie eröffnet neue Trades nicht automatisch. Stattdessen beobachtet es kontinuierlich die aktuelle Position auf `Strategy.Security`, aktualisiert das gewünschte Trailing-Stop-Level entsprechend dem ausgewählten Modus und den benutzerdefinierten Filtern und schließt das Engagement, sobald der Preis die Trailing-Barriere erreicht. Da StockSharp mit Nettopositionen und nicht mit einzelnen Tickets arbeitet, beziehen sich alle Berechnungen auf die Gesamtmenge.

## Handelslogik
1. Abonnieren Sie das konfigurierte `CandleType` und verarbeiten Sie nur fertige Kerzen, um vorzeitige Stoppanpassungen zu vermeiden.
2. Halten Sie kurze rollierende Puffer mit Kerzenhochs und -tiefs ein, damit Fraktale und aktuelle Extremwerte abgerufen werden können, ohne verbotene Indikatormethoden aufzurufen.
3. Berechnen Sie optional eine geglättete Nah-zu-Nah-Geschwindigkeit in Punkten, wenn der Geschwindigkeits-Trailing-Modus aktiv ist.
4. Erzeugen Sie für jede abgeschlossene Kerze den potenziellen Trailing-Stop-Preis basierend auf dem ausgewählten Modus:
   - Niedrigster Tiefststand aus der jüngsten Kerzenhistorie abzüglich des Offsets von `DeltaPoints`.
   - Letztes bestätigtes Fraktal angepasst um `DeltaPoints`.
   - Der Schlusskurs wurde um eine geschwindigkeitsabhängige Distanz verschoben.
   - Aktueller Parabolic SAR-Wert, versetzt um `DeltaPoints`.
   - Eine feste Entfernung, ausgedrückt in Instrumentenpunkten.
5. Überprüfen Sie den Kandidaten anhand von Money-Management-Filtern: Erfordern Sie vorhandene Stopps, erlauben Sie nur profitables Trailing, stoppen Sie, sobald die Gewinnschwelle erreicht ist, oder stützen Sie den Gewinntest auf den durchschnittlichen Einstiegspreis.
6. Ersetzen Sie das gespeicherte Stopp-Level, wenn der Kandidat das bestehende um mindestens `StepPoints` verbessert.
7. Wenn die Kerze das gespeicherte Niveau überschreitet (Tief für Long-Positionen, Hoch für Short-Positionen) und der Handel zulässig ist, schließen Sie die Nettoposition mit einer Marktorder.
8. Protokollieren Sie optional eine Textzusammenfassung mit Saldo, Positionsgröße, Einstiegspreis, aktuellem Stop und nicht realisiertem PnL, die den OrderBalans-Indikator MetaTrader emuliert.

## Nachlaufmodi
- **Kerze** – liegt hinter dem jüngsten signifikanten Kerzenextrem zurück. Über `DeltaPoints` werden Offsets angewendet, um den Stopp leicht von der Unterstützung/dem Widerstand entfernt zu halten.
- **Fraktal** – verwendet das letzte im verarbeiteten Zeitrahmen erkannte Fraktal mit fünf Balken. Dies ahmt die Standardimplementierung von MetaTrader nach, arbeitet jedoch mit Nettopositionen.
- **Geschwindigkeit** – schätzt die Preisgeschwindigkeit durch Mittelung der Änderungen nahe zum Schluss über `VelocityPeriod`. Wenn sich der Impuls in Richtung der Position beschleunigt, wird der Anschlag proportional zur Geschwindigkeitsdifferenz skaliert durch `VelocityMultiplier` festgezogen.
- **Parabolic** – folgt dem von StockSharp verwalteten Indikator Parabolic SAR. Der Anschlag schmiegt sich an die SAR-Punkte und erbt die Schritt- und Maximalbeschleunigungsparameter.
- **Fixpunkte** – erzwingt einen konstanten Abstand zum Preis und spiegelt effektiv das „>4 Pips“-Verhalten des Originalskripts wider.
- **Aus** – deaktiviert das Nachlaufen und lässt den aktuellen Stopp unberührt.

## Parameter
| Name | Typ | Standard | Beschreibung |
| --- | --- | --- | --- |
| `Mode` | `TrailingStopMode` | `Candle` | Bestimmt, welcher Trailing-Algorithmus aktiv ist. |
| `CandleType` | `DataType` | 15-Minuten-Kerzen | Zeitrahmen, der zur Analyse von Kerzen und zur Berechnung von Trailing-Daten verwendet wird. |
| `DeltaPoints` | `int` | `0` | Zusätzlicher Abstand (in Instrumentenpunkten), der unterhalb/über dem rohen Trailing-Preis hinzugefügt wird. |
| `StepPoints` | `int` | `0` | Mindestverbesserung in Punkten, die erforderlich ist, bevor ein vorhandener Trailing Stop aktualisiert wird. |
| `FixedDistancePoints` | `int` | `50` | Distanz für den festen Nachlaufmodus. Wird von anderen Modi ignoriert. |
| `TrailOnlyProfit` | `bool` | `true` | Bei `true` beginnt das Trailing erst, nachdem der Stop mit einem Gewinn im Verhältnis zum Einstiegspreis enden würde. |
| `TrailOnlyBreakEven` | `bool` | `false` | Stoppen Sie die Aktualisierung, sobald der gespeicherte Stopp die Gewinnschwelle überschritten hat. |
| `RequireExistingStop` | `bool` | `false` | Ignorieren Sie nachfolgende Aktualisierungen, bis bereits ein Stoppniveau berechnet wurde. |
| `UseGeneralBreakEven` | `bool` | `false` | Bewerten Sie den Rentabilitätsfilter anhand des durchschnittlichen Einstiegspreises der Nettoposition (entspricht dem ursprünglichen `TProfit`-Helfer). |
| `VelocityPeriod` | `int` | `30` | Anzahl der Schließungen, die zur Durchschnittsgeschwindigkeit im Geschwindigkeitsmodus verwendet werden. |
| `VelocityMultiplier` | `decimal` | `1` | Skaliert die auf die Nachlaufstrecke angewendete Geschwindigkeitsanpassung. |
| `ParabolicStep` | `decimal` | `0.02` | Beschleunigungsschritt für den Indikator Parabolic SAR. |
| `ParabolicMaximum` | `decimal` | `0.2` | Maximale Beschleunigung für den Indikator Parabolic SAR. |
| `LogOrderSummary` | `bool` | `true` | Ermöglicht die Textprotokollierung ähnlich dem OrderBalans-Bedienfeld. |
| `TradeVolume` | `decimal` | `1` | Standardvolumen, das beim Reduzieren von Positionen über Hilfsmethoden verwendet wird. |

## Unterschiede zu den ursprünglichen MetaTrader-Skripten
- Die Konvertierung funktioniert mit StockSharp Nettopositionen statt mit Einzeltickets. Stoppaktualisierungen gelten daher für die gesamte Position, unabhängig davon, wie sie aufgebaut wurde.
- Magische Zahlen- und Multisymbolfilter wurden entfernt. Die Strategie überwacht nur `Strategy.Security` und geht davon aus, dass die Positionsgrößenbestimmung extern erfolgt.
- Der benutzerdefinierte Indikator MetaTrader `Velocity` wird durch eine durchschnittliche Nah-zu-Schluss-Differenz angenähert, die in Instrumentenpunkten gemessen wird. Dadurch bleibt das Verhalten intuitiv, stimmt jedoch möglicherweise nicht genau mit dem proprietären Indikator überein.
- Visuelle Diagrammobjekte (Trendlinien, Pfeile, Beschriftungen) wurden durch textuelle Protokolleinträge ersetzt. Der Parameter `LogOrderSummary` erstellt das von *OrderBalansEN_v3_4.mq4* erstellte Informationspanel neu, ohne auf die manuelle Diagrammzeichnung angewiesen zu sein.
- Stop-Änderungen verwenden StockSharp-Hilfsmethoden (`BuyMarket`, `SellMarket`), da die Plattform kein direktes Äquivalent zu MetaTraders `OrderModify` für einzelne Tickets bereitstellt.

## Anwendungstipps
- Hängen Sie die Strategie an ein Diagramm an, um die Wirkung jedes Trailing-Modus zu visualisieren. Aktivieren Sie für Parabolic SAR den Diagrammbereich, um Punkte und Trades gleichzeitig anzuzeigen.
- Passen Sie `DeltaPoints` und `StepPoints` entsprechend der Tick-Größe des Instruments an. Die Implementierung konvertiert Punkte automatisch mithilfe von `Security.PriceStep` oder `Security.MinPriceStep`.
- Lassen Sie `TrailOnlyProfit` aktiviert, wenn Sie das ursprüngliche Verhalten nachahmen, da das MetaTrader-Skript eine Verschärfung der Stopps vermeidet, bevor Positionen profitabel wurden.
- Deaktivieren Sie `LogOrderSummary`, wenn Sie eine leisere Ausgabe bevorzugen oder Hunderte von Strategien gleichzeitig ausführen.
- Testen Sie den Geschwindigkeitsmodus mit verschiedenen `VelocityMultiplier`-Werten; Höhere Multiplikatoren sorgen dafür, dass der Trailing Stop schneller auf plötzliche Impulsausbrüche reagiert.

## Indikatoren
- Parabolic SAR (`ParabolicSar`)
- Rollende Kerzenhochs und -tiefs (native Datenpuffer)
- Optionale durchschnittliche Nah-zu-Schluss-Geschwindigkeit, abgeleitet aus Kerzenschlüssen
