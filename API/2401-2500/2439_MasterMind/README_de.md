# Die MasterMind-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die den Stochastic-Oszillator und Williams %R nutzt, um extreme überverkaufte und überkaufte Bedingungen zu erfassen.

## Überblick
Die Strategie überwacht zwei Momentum-Indikatoren:
- **Stochastic Oscillator** mit Basislänge 100 und Glättung 3/3.
- **Williams %R** mit Länge 100.

Eine Long-Position wird eröffnet, wenn der Stochastic-%D-Wert unter 3 fällt, während Williams %R unter -99.9 liegt, was einen überverkauften Markt anzeigt.
Eine Short-Position wird eröffnet, wenn der Stochastic-%D über 97 steigt und Williams %R über -0.1 klettert, was einen überkauften Markt signalisiert.

Nach dem Einstieg verwaltet der Algorithmus das Risiko über Stop-Loss, Take-Profit, Trailing-Stop und optionale Break-Even-Bewegung.

## Parameter
- `StochasticLength` – Zeitraum für Stochastic- und Williams-%R-Berechnungen.
- `StopLoss` – Abstand vom Einstiegspreis für Stop-Loss in Punkten.
- `TakeProfit` – Take-Profit-Abstand in Punkten.
- `TrailingStop` – Aktivierungsabstand für Trailing in Punkten.
- `TrailingStep` – Schritt des Trailing-Stops in Punkten.
- `BreakEven` – Gewinn in Punkten, bei dem der Stop auf den Einstieg verschoben wird.
- `CandleType` – Kerzen-Zeitrahmen für Strategieberechnungen.

## Indikatoren
- `StochasticOscillator`
- `WilliamsR`

## Handelsregeln
1. **Kaufen** wenn `%D < 3` und `Williams %R < -99.9`.  
2. **Verkaufen** wenn `%D > 97` und `Williams %R > -0.1`.  
3. Nach dem Einstieg Stop-Loss und Take-Profit anwenden.  
4. Stop auf Break-Even verschieben, wenn der Preis um `BreakEven` vorgeschritten ist.  
5. Trailing-Stop aktivieren, wenn der Preis sich um `TrailingStop` bewegt, Verschiebung um `TrailingStep`.

## Hinweise
Die Strategie verwendet die High-Level-API von StockSharp und ist als Lehrbeispiel gedacht.
