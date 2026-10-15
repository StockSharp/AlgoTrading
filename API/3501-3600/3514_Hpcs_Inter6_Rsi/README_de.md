# Hpcs Inter6 RSI Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die Hpcs Inter6 RSI-Strategie portiert den MetaTrader-Experten `_HPCS_Inter6_MT4_EA_V01_WE` zum StockSharp-High-Level-API. Der Algorithmus beobachtet den Relative Strength Index (RSI) einer konfigurierbaren Kerzenreihe und reagiert auf schnelle Umkehrungen um die klassischen 70/30-Schwellenwerte. Immer wenn RSI 70 überschreitet, geht die Strategie in eine Short-Position über, während ein Wert unter 30 in eine Long-Position übergeht. Mit jedem Trade werden sofort symmetrische Take-Profit- und Stop-Loss-Werte verbunden, die in Pips gemessen werden.

## Daten und Indikatoren
- **Kerzenquelle** – vom Benutzer ausgewählter Zeitrahmen (Standard 1 Stunde).
- **Indikator** – Relativer Stärkeindex mit konfigurierbarer Länge (Standard 14). Der Indikator wird über die Indikatorbindungspipeline StockSharp neu berechnet.

## Eingabelogik
1. Die Strategie wartet auf eine fertige Kerze, um den Handel mit unvollständigen Daten zu vermeiden.
2. Bei jeder abgeschlossenen Kerze wird der neue RSI-Wert mit dem vorherigen Wert verglichen.
3. **Kurzeinstellung:** Wenn RSI gerade von unten über `UpperLevel` (Standard 70) gekreuzt ist, verkauft die Strategie mithilfe einer Marktorder. Bestehende Long-Positionen werden geschlossen, bevor die Short-Position hergestellt wird, sodass die resultierende Nettoposition genau um das konfigurierte Volumen short ist.
4. **Langes Setup:** Wenn RSI gerade von oben unter `LowerLevel` (Standard 30) gekreuzt ist, kauft die Strategie mithilfe einer Marktorder. Vorhandene Short-Positionen werden zunächst abgedeckt, so dass die Nettoposition um das konfigurierte Volumen long wird.
5. Es ist nur ein Eintrag pro Kerze zulässig. Mehrere Signale innerhalb desselben Balkens werden ignoriert, um die MetaTrader-Implementierung widerzuspiegeln, die den Zeitstempelschutz des Balkens verwendet.

## Exit-Logik
- Jeder Eintrag definiert ein festes Ziel und einen Stopp bei derselben Entfernung, gemessen in Pips.
- Bei einer Long-Position wird die Strategie beendet, wenn das Kerzenhoch das Ziel oder das Tief den Schutzstopp berührt.
- In einer Short-Position deckt die Strategie ab, ob das Kerzentief das Ziel erreicht oder ob das Hoch den Schutzstopp erreicht.
- Wenn die Position flach ist, werden alle Schutzebenen gelöscht.

Der Pip-Abstand wird anhand der Tick-Größe des Instruments in Preiseinheiten umgerechnet. Bei Instrumenten mit drei oder fünf Dezimalstellen multipliziert der Algorithmus den Abstand mit zehn, um dem MetaTrader-Konzept eines Pip zu entsprechen.

## Parameter
| Parameter | Standard | Beschreibung |
|-----------|---------|-------------|
| `CandleType` | 1-stündiger Zeitrahmen | Zeitrahmen, der den Indikator RSI speist. |
| `RsiLength` | 14 | Lookback-Zeitraum des RSI. |
| `UpperLevel` | 70 | RSI-Stufe, die bei Überschreitung von unten kurze Einstiege auslöst. |
| `LowerLevel` | 30 | RSI-Ebene, die bei Überschreitung von oben lange Einträge auslöst. |
| `TradeVolume` | 1 | Ordergröße für Markteintritte. Vorhandenes Exposure wird vor der Umkehrung geschlossen. |
| `OffsetInPips` | 10 | Abstand von Take-Profit und Stop-Loss vom Einstiegspreis, ausgedrückt in Pips. |

Alle Parameter werden über `StrategyParam`-Objekte verfügbar gemacht, sodass sie innerhalb von StockSharp optimiert werden können.

## Notizen
- Die Strategie basiert auf dem Hoch/Tief der Kerze, um Take-Profit- und Stop-Loss-Füllungen zu simulieren, was dem Verhalten von Festpreiszielen in MetaTrader entspricht.
- Es werden keine ausstehenden Bestellungen aufgegeben; Alle Ausführungen sind Marktaufträge, die vom Strategiekern abgewickelt werden.
- Die Indikator- und Diagrammbindungen werden automatisch erstellt, wenn ein Diagrammbereich verfügbar ist, und bieten eine visuelle Überlagerung von Kerzen und RSI.
