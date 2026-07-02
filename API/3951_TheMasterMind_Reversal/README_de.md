# Die MasterMind-Umkehrstrategie (StockSharp Port)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
- Port des MetaTrader 4-Expertenberaters „TheMasterMind“, der einen Stochastic-Oszillator mit Williams %R kombiniert, um extreme Umkehrungen zu erfassen.
- Implementiert mit StockSharps High-Level-API unter Verwendung von Kerzenabonnements und Indikatorbindungen.
- Handelt ein einzelnes Wertpapier und reagiert nur auf fertige Kerzen, was den ursprünglichen Ausführungsstil „Trade at Close“ widerspiegelt.

## Handelslogik
1. **Indikatorvorbereitung**
   - `StochasticOscillator` liefert die %D-Signalleitung mit konfigurierbarer %K/%D-Glättung und gesamter Lookback-Länge.
   - `WilliamsR` misst die relative Position des Schlusskurses innerhalb des aktuellen Hoch-/Tief-Bereichs.
2. **Eintrittsregeln**
   - **Kaufen**, wenn `%D <= 3` _und_ `Williams %R <= -99.5`, was ein stochastisches überverkauftes Extrem zusammen mit einer tiefen WPR-Penetration unterhalb der Untergrenze signalisiert.
   - **Verkaufen** wenn `%D >= 97` _und_ `Williams %R >= -0.5`, was ein überkauftes Extrem signalisiert, das dadurch bestätigt wird, dass Williams %R nahe bei 0 bleibt.
   - Liegt eine Gegenposition vor, wird diese zunächst abgeflacht und anschließend eine neue Marktorder mit dem konfigurierten Basisvolumen gesendet.
3. **Ausgangsregeln**
   - Umkehrsignale schließen die aktuelle Position und kehren die Richtung um (eine Position nach der anderen, entsprechend dem im MQL-Skript verwendeten Hedging-deaktivierten Modus).
   - Die optionalen `StartProtection` Stop-Loss-, Take-Profit- und Trailing-Stop-Dienste verarbeiten Schutzexits genau einmal pro Strategiestart.

## Risikomanagement
- Die Parameter `StopLoss`, `TakeProfit`, `UseTrailingStop`, `TrailingStop` und `TrailingStep` entsprechen den Geldverwaltungskontrollen des ursprünglichen EA.
- Alle Entfernungen werden in absoluten Preiseinheiten ausgedrückt, um Maklerunabhängig zu bleiben. Lassen Sie sie bei `0`, um die entsprechende Schutzfunktion zu deaktivieren.
- `StartProtection` wird automatisch aktiviert, wenn mindestens einer der Schutzabstände ungleich Null ist.

## Strategieparameter
| Parameter | Beschreibung | Standard |
|-----------|-------------|---------|
| `TradeVolume` | Basislosgröße für jeden neuen Eintrag. | `1` |
| `StochasticPeriod` | Totaler Lookback für den stochastischen Oszillator. | `100` |
| `KPeriod` | %K Glättungslänge. | `3` |
| `DPeriod` | %D Signallänge. | `3` |
| `WilliamsPeriod` | Lookback-Länge für Williams %R. | `100` |
| `StochasticBuyThreshold` | Obergrenze, unter der %D bleiben muss, um Long-Positionen zuzulassen. | `3` |
| `StochasticSellThreshold` | Untergrenze, über der %D bleiben muss, um Kurzschlüsse zuzulassen. | `97` |
| `WilliamsBuyLevel` | Überverkaufter Wert für Williams %R. | `-99.5` |
| `WilliamsSellLevel` | Überkaufter Wert für Williams %R. | `-0.5` |
| `StopLoss` | Absoluter Stop-Loss-Abstand. | `0` |
| `TakeProfit` | Absolute Take-Profit-Distanz. | `0` |
| `UseTrailingStop` | Aktiviert den nachgestellten Schutz, wenn `true`. | `false` |
| `TrailingStop` | Absoluter Trailing-Stop-Abstand. | `0` |
| `TrailingStep` | Beim Nachlaufen wird ein Schritt angewendet. | `0` |
| `CandleType` | Zeitrahmen für das primäre Kerzenabonnement (Standard 15 Minuten). | `15m time frame` |

## Implementierungshinweise
- Die Strategie abonniert eine einzelne Kerzenserie über `SubscribeCandles(CandleType)` und bindet die stochastischen und Williams %R-Indikatoren über `BindEx`.
- Handelsentscheidungen werden nur getroffen, wenn `candle.State == CandleStates.Finished` und `IsFormedAndOnlineAndAllowTrading()` erfüllt sind.
- Diagrammhelfer (`DrawCandles`, `DrawIndicator`, `DrawOwnTrades`) werden aufgerufen, wenn ein Diagrammbereich zur Visualisierung der Indikatoren und Trades verfügbar ist.
- Protokollierungsanweisungen (`LogInfo`) spiegeln die ursprünglichen Alarmzeichenfolgen wider und helfen dabei, den Entscheidungsprozess während des Live-Handels oder Backtestings zu verfolgen.
