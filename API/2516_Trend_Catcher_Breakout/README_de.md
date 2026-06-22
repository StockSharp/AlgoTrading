# Trend Catcher Ausbruch-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die Trend Catcher-Strategie ist eine Konvertierung des MetaTrader 5 Expertenberaters "Trend_Catcher_v2". Sie kombiniert drei exponentielle gleitende Durchschnitte mit dem Parabolic SAR-Indikator, um Trendumkehrungen und Trendfortsetzungsmöglichkeiten zu identifizieren. Das System arbeitet auf einem einzelnen Symbol und Zeitrahmen und stützt sich auf End-of-Candle-Berechnungen, was es sowohl für Backtesting im StockSharp Designer als auch für Live-Ausführung über StockSharp API-basierte Runner geeignet macht.

## Indikatoren und Filter
- **Parabolic SAR** — erkennt bullische und bärische Flips, die auf mögliche Umkehrungen hinweisen.
- **Langsame EMA** — der übergeordnete Zeitrahmen-Trendfilter, der die dominante Richtung definiert.
- **Schnelle EMA** — reagiert schneller auf Preisveränderungen, um die Richtung des aktuellen Swings zu bestätigen.
- **Trigger-EMA** — hält den Einstieg nahe an der Preisbewegung und vermeidet Trades, die zu weit vom Mittelwert entfernt sind.
- **Handelstag-Schalter** — optionale Filter zum Deaktivieren des Handels an ausgewählten Wochentagen.

## Handelslogik
### Long-Einstiege
1. Der Schlusskurs endet über dem aktuellen Parabolic SAR-Wert.
2. Die vorherige Kerze schloss unterhalb des vorherigen Parabolic SAR-Wertes (bullischer Flip).
3. Die schnelle EMA liegt über der langsamen EMA, was einen Aufwärtstrend bestätigt.
4. Der Schlusskurs liegt über der Trigger-EMA, um Gegentrend-Signale zu vermeiden.
5. Keine Position ist offen und keine Position wurde während der aktuellen Kerze geschlossen.

### Short-Einstiege
Alle obigen Bedingungen sind gespiegelt:
1. Der Schlusskurs endet unter dem aktuellen Parabolic SAR-Wert.
2. Die vorherige Kerze schloss oberhalb des vorherigen Parabolic SAR-Wertes (bärischer Flip).
3. Die schnelle EMA liegt unter der langsamen EMA.
4. Der Schlusskurs liegt unter der Trigger-EMA.
5. Keine Position ist offen und keine Position wurde während der aktuellen Kerze geschlossen.

Wenn der **Reverse Signals**-Schalter aktiviert ist, werden die Long- und Short-Bedingungen invertiert, sodass die Strategie Ausbrüche in die entgegengesetzte Richtung handeln kann.

## Positionsmanagement
- **Automatischer Stop-Loss** – wenn aktiviert, wird der Stop aus der Distanz zwischen Preis und Parabolic SAR multipliziert mit dem `StopLossCoefficient` berechnet. Die Distanz wird zwischen `MinStopLoss` und `MaxStopLoss` begrenzt.
- **Automatischer Take-Profit** – multipliziert die Stop-Distanz mit `TakeProfitCoefficient`. Manuelle Distanzen können verwendet werden, wenn die Automatisierung deaktiviert ist.
- **Risikobezogene Positionsgröße** – die Handelsgröße wird aus dem Portfoliokapital und `RiskPercent` abgeleitet. Wenn der zuletzt geschlossene Trade ein Verlust ist und **Use Martingale** aktiviert ist, wird die berechnete Größe mit `MartingaleMultiplier` multipliziert.
- **Breakeven und Trailing Stop** – nach Erreichen des `BreakevenTrigger`-Gewinns wird der Stop zum Einstiegspreis plus `BreakevenOffset` verschoben (oder minus für Short-Trades). Sobald die Position `TrailingTrigger` gewinnt, folgt der Stop dem Preis um `TrailingStep`.
- **Schließen bei entgegengesetztem Signal** – wenn aktiv, verlässt die Strategie eine bestehende Position, sobald ein entgegengesetztes Setup erscheint.
- **Ein Trade pro Kerze** – der Algorithmus speichert den Zeitstempel des letzten Ausstiegs und überspringt Einstiege bis die nächste Kerze öffnet.

## Parameter
| Name | Beschreibung | Standardwert |
| --- | --- | --- |
| `CandleType` | Haupt-Zeitrahmen für alle Indikatoren. | 15-Minuten-Zeitrahmen |
| `CloseOnOppositeSignal` | Sofort aussteigen, wenn das umgekehrte Setup erkannt wird. | `true` |
| `ReverseSignals` | Long- und Short-Bedingungen tauschen. | `false` |
| `TradeMonday` … `TradeFriday` | Handel an bestimmten Wochentagen aktivieren oder deaktivieren. | `true` |
| `SlowMaPeriod` | Periode des langsamen EMA-Trendfilters. | `200` |
| `FastMaPeriod` | Periode der schnellen EMA-Bestätigung. | `50` |
| `FastFilterPeriod` | Periode der Trigger-EMA. | `25` |
| `SarStep` | Parabolic SAR Beschleunigungsschritt. | `0.004` |
| `SarMax` | Maximale Parabolic SAR-Beschleunigung. | `0.2` |
| `AutoStopLoss` | Dynamische Stop-Loss-Berechnung aktivieren. | `true` |
| `AutoTakeProfit` | Dynamische Take-Profit-Berechnung aktivieren. | `true` |
| `MinStopLoss` / `MaxStopLoss` | Untere und obere Grenzen für die Stop-Distanz. | `0.001` / `0.2` |
| `StopLossCoefficient` | Multiplikator für die SAR-Distanz. | `1` |
| `TakeProfitCoefficient` | Multiplikator für die Take-Profit-Distanz. | `1` |
| `ManualStopLoss` | Feste Stop-Distanz wenn Automatisierung deaktiviert. | `0.002` |
| `ManualTakeProfit` | Feste Zieldistanz wenn Automatisierung deaktiviert. | `0.02` |
| `RiskPercent` | Prozentsatz des Portfoliokapitals, das pro Trade riskiert wird. | `2` |
| `UseMartingale` | Größe nach einem Verlust-Trade erhöhen. | `true` |
| `MartingaleMultiplier` | Multiplikator nach einem Verlust. | `2` |
| `BreakevenTrigger` | Gewinn erforderlich, bevor der Stop auf Breakeven verschoben wird. | `0.005` |
| `BreakevenOffset` | Puffer beim Verschieben des Stops auf Breakeven. | `0.0001` |
| `TrailingTrigger` | Gewinn erforderlich, um den Stop zu trailen. | `0.005` |
| `TrailingStep` | Distanz, die vom Trailing Stop gehalten wird. | `0.001` |

## Verwendungshinweise
- Die Strategie sendet Marktorders sowohl für Einstiege als auch für Ausstiege; Slippage-Kontrollen sollten bei Bedarf auf der Brokerage-Adapter-Ebene hinzugefügt werden.
- Da die Logik End-of-Candle-Daten verwendet, hängt die Genauigkeit der Backtests von der Granularität der der Strategie gelieferten Kerzenserie ab.
- Parameter sind vollständig über `StrategyParam`-Objekte exponiert, sodass sie für die Optimierung im StockSharp Designer verfügbar sind.
