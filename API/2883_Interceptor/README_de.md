# Interceptor-Strategie (StockSharp-Port)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Die Interceptor-Strategie ist ein C#-Port des originalen MetaTrader5-Expertenberaters. Sie kombiniert Multi-Timeframe-EMA-"Fächer"-Ausrichtung mit Stochastic-Oszillatoren, Flat-Range-Ausbruchserkennung, Divergenzanalyse, Hammer-Kerzenmuster-Filter und Horn-Bestätigung (Fächerkonvergenz). Das Ziel ist die Nutzung starker Trendfortsetzungen nach Konsolidierungsphasen auf dem GBP/USD-5-Minuten-Chart.

## Kernlogik
- **Trendstruktur** – Die Strategie bewertet exponentiell gleitende Durchschnitte (Längen 34/55/89/144/233) auf M5-, M15- und H1-Zeitrahmen. Ein gültiger Trend erfordert, dass alle EMA-Fächer ausgerichtet sind (aufsteigend für bullisch, absteigend für bärisch) und der maximale Abstand zwischen dem langsamsten und schnellsten EMA unter konfigurierbaren Schwellenwerten bleibt.
- **Momentum-Bestätigung** – M5- und M15-Stochastic-Oszillatoren müssen aus überkauften/überverkauften Bereichen kreuzen, um zu bestätigen, dass der Preis Kongestionszone verlässt.
- **Flat-Breakout-Filter** – Ein Volatilitätskompressionsetektor sucht nach engen Ranges (Länge und Breite durch `FlatnessCoefficient`, `MinFlatBars` und `MaxFlatPoints` gesteuert). Ausbrüche aus diesen Zonen erhöhen das Signalvertrauen.
- **Hammer-Filter** – Kürzliche Hammer- oder invertierte Hammer-Kerzen (validiert durch Körper/Langeschatten-Regeln und lokale Hochs/Tiefs) fungieren als Erschöpfungssignale in Richtung des beabsichtigten Trades.
- **Divergenzprüfung** – Die Strategie sucht nach bullischen/bärischen Divergenzen zwischen Kurs und M5-Stochastic-Oszillator, um Reversals nach Fächerausrichtung zu antizipieren.
- **Horn-Bestätigung** – Wenn der M5-EMA-Fächer konvergiert (das "Horn"), löst ein Ausbruch über/unter eine aktuelle Range zusätzliche Einstiege aus, wenn die höheren Zeitrahmen die Bewegung unterstützen.

## Einstiegsbedingungen
Ein Long-Setup kann durch eine oder mehrere Bedingungen ausgelöst werden (jede fügt der Entscheidung Gewicht hinzu):
1. EMA-Fächer auf allen drei Zeitrahmen ausgerichtet, M5-Stochastic-bullischer-Crossover, starker bullischer Kerzenkörper.
2. M5-EMA-Fächer-Ausbruchskerze, die am Tief öffnet und über den schnellen EMAs schließt.
3. Flat-Range-Ausbruch in bullische Richtung.
4. M5 + M15-Ausbruchsübereinstimmung, während EMA-Fächerabstände unter den erlaubten Schwellenwerten bleiben.
5. Bullische Divergenz zwischen Stochastic und Kurs, während Fächer nach oben zeigen.
6. Kürzliche bullische Hammer-Kerze innerhalb des erlaubten Lookback-Fensters.
7. M15-Stochastic-bullischer Crossover mit bullischen Kerzenkörpern.
8. Horn-Ausbruch über die aktuelle Range nach EMA-Fächerkonvergenz.

Short-Setups folgen der Spiegellogik. Wenn sowohl Long- als auch Short-Bedingungen gleichzeitig vorhanden sind, überspringt die Strategie den Handel für diese Bar.

## Ausstieg & Risikomanagement
- Konfigurierbarer fester Stop-Loss und Take-Profit in Punkten.
- Optionale Breakeven-Logik (`StopLossAfterBreakeven`, `TakeProfitAfterBreakeven`), die den Stop strafft, sobald der Kurs einen Gewinnschwellenwert erreicht.
- Trailing Stop basierend auf dem Kursabstand vom letzten Schlusskurs (`TrailingDistancePoints` mit `TrailingStepPoints`).
- Wenn eine neue Position eröffnet wird, schließt die Strategie zuerst alle bestehenden entgegengesetzten Positionen.

## Parameter
| Parameter | Beschreibung |
|-----------|-------------|
| `Volume` | Ordervolumen für jeden Einstieg. |
| `FlatnessCoefficient` | Multiplikator zur Steuerung der maximal erlaubten Breite einer erkannten Flat-Range. |
| `StopLossPoints` | Anfänglicher Stop-Loss-Abstand in Preispunkten. |
| `TakeProfitPoints` | Anfänglicher Take-Profit-Abstand in Preispunkten (0 deaktiviert). |
| `TakeProfitAfterBreakeven` | Erforderlicher Gewinn (Punkte) bevor Breakeven-Logik aktiviert wird. |
| `StopLossAfterBreakeven` | Abstand des Breakeven-Stops nach Aktivierung. |
| `MaxFanDistanceM5/M15/H1` | Maximale EMA-Spreizung auf jedem Zeitrahmen. |
| `StochasticKPeriodM5/M15` | %K-Länge für Stochastic-Oszillatoren auf M5 und M15. |
| `StochasticUpperM5/M15` | Überkauftschwellenwerte. |
| `StochasticLowerM5/M15` | Überverkauftschwellenwerte. |
| `MinBodyPoints` | Mindestkerzenkörpergröße für eine starke Bar. |
| `MinFlatBars` | Mindestbars für eine Flat-Range-Definition. |
| `MaxFlatPoints` | Maximale Flat-Range-Breite (Punkte). |
| `MinDivergenceBars` | Mindestabstand zwischen Divergenz-Pivots. |
| `HammerLongShadowPercent` | Minimaler Langschatten-Prozentsatz für Hammer-Erkennung. |
| `HammerShortShadowPercent` | Maximaler Gegenschatten-Prozentsatz für Hammer-Erkennung. |
| `HammerMinSizePoints` | Minimale Gesamtspanne der Hammer-Kerze. |
| `HammerLookbackBars` | Lookback-Fenster zur Suche nach Hammer-Mustern. |
| `HammerRangeBars` | Anzahl der Bars zur Validierung von Hammer-Hochs/Tiefs. |
| `MaxFanWidthAtNarrowest` | Maximale EMA-Spreizung wenn der Fächer als konvergiert gilt. |
| `FanConvergedBars` | Anzahl der Bars, die der Fächer für Horn-Signale konvergiert bleiben kann. |
| `RangeBreakLookback` | Lookback-Fenster für die Range-Ausbruchserkennung. |
| `TrailingStepPoints` | Minimales Inkrement für Trailing-Stop-Anpassungen. |
| `TrailingDistancePoints` | Abstand zwischen Kurs und Trailing Stop. |
| `CandleType` | Primäre Kerzenserie (Standard M5-Zeitkerzen). |

## Verwendungshinweise
- Der ursprüngliche Expertenberater wurde für GBP/USD-M5-Charts konzipiert. Parameter müssen möglicherweise für andere Instrumente oder Zeitrahmen angepasst werden.
- Die Strategie erfordert die StockSharp High-Level-API und Kerzendaten für M5-, M15- und H1-Intervalle.
- Es wird nur eine Netto-Position gehalten; entgegengesetzte Positionen werden vor neuen Trades geschlossen.

## Haftungsausschluss
Die Strategie wird zu Bildungszwecken bereitgestellt. Vergangene Performance garantiert keine zukünftigen Ergebnisse. Validieren Sie Parameter und Logik immer in einer sicheren Testumgebung, bevor Sie mit realem Kapital handeln.
