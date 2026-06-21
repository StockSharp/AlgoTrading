# SlopeDirectionLine-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie repliziert das Verhalten des *Slope Direction Line* Expert Advisors. Sie analysiert die Steigung einer linearen Regressionslinie, die auf Schlusskursen aufgebaut ist. Eine Long-Position wird eröffnet, wenn die Regressionssteigung nach einer negativen Periode positiv wird, während eine Short-Position eröffnet wird, wenn sie nach einer positiven Periode negativ wird. Entgegengesetzte Positionen werden bei jeder Richtungsänderung geschlossen. Optionale Stop-Loss- und Take-Profit-Prozentsätze schützen Positionen über den eingebauten `StartProtection`-Mechanismus.

## Details
- **Indikator** – `LinearRegression` von StockSharp. Die Strategie verwendet die `LinearRegSlope`-Komponente als Signal.
- **Signal** – Kreuzung der Steigung durch null. Eine positive Steigung zeigt einen Aufwärtstrend an; eine negative Steigung signalisiert einen Abwärtstrend.
- **Ein-/Ausstieg** – wenn die Steigung ihr Vorzeichen ändert, wird die aktuelle Position geschlossen und, falls erlaubt, eine neue Position in Richtung der Steigung eröffnet.
- **Risikokontrolle** – `StartProtection` wird mit benutzerdefinierten Take-Profit- und Stop-Loss-Prozentsätzen konfiguriert.

## Parameter
| Name | Beschreibung |
|------|--------------|
| `CandleType` | Zeitrahmen für den Kerzenaufbau. |
| `Length` | Anzahl der Balken für die lineare Regressionsberechnung. |
| `TakeProfitPercent` | Prozentuale Distanz zum Take-Profit vom Einstiegspreis. |
| `StopLossPercent` | Prozentuale Distanz zum Stop-Loss vom Einstiegspreis. |
| `AllowLong` | Eröffnung von Long-Positionen erlauben. |
| `AllowShort` | Eröffnung von Short-Positionen erlauben. |

## Verwendung
1. Strategie zu einer StockSharp-Anwendung hinzufügen.
2. Parameter entsprechend dem gewünschten Zeitrahmen und Risiko konfigurieren.
3. Strategie starten und Trades auf dem Chart überwachen.
