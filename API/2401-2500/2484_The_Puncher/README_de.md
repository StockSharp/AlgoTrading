# The Puncher-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
- Konvertiert vom MetaTrader 5 Expert Advisor „The Puncher".
- Kombiniert einen Stochastik-Oszillator mit langer Periode und RSI zur Identifizierung von Erschöpfungszonen.
- Handelt nur bei geschlossener Kerze gemäß dem High-Level-API-Ansatz von StockSharp.
- Wendet schützenden Stop-Loss, Take-Profit, Break-Even und Trailing-Stop-Logik zur Risikosteuerung an.

## Indikatoren
- **Stochastik-Oszillator**: Basisperiode `StochasticPeriod`, %K-Glättung `StochasticSignalPeriod`, %D-Glättung `StochasticSmoothingPeriod`.
- **Relative Strength Index (RSI)**: Periode `RsiPeriod`.

## Parameter
| Parameter | Standard | Beschreibung |
|-----------|----------|--------------|
| `StochasticPeriod` | 100 | Basisperiode des Stochastik-Oszillators. |
| `StochasticSignalPeriod` | 3 | Glättungsperiode für die %K-Linie. |
| `StochasticSmoothingPeriod` | 3 | Glättungsperiode für die %D-Linie. |
| `RsiPeriod` | 14 | Berechnungslänge des RSI. |
| `OversoldLevel` | 30 | Schwellenwert für Stochastik und RSI zur Erkennung überverkaufter Zonen. |
| `OverboughtLevel` | 70 | Schwellenwert für Stochastik und RSI zur Erkennung überkaufter Zonen. |
| `StopLossPips` | 20 | Stop-Loss-Abstand in Pips (0 deaktiviert den Stop-Loss). |
| `TakeProfitPips` | 50 | Take-Profit-Abstand in Pips (0 deaktiviert den Take-Profit). |
| `TrailingStopPips` | 10 | Trailing-Stop-Abstand in Pips (0 deaktiviert das Trailing). |
| `TrailingStepPips` | 5 | Mindestbewegung in Pips, bevor der Trailing-Stop erneut nachgezogen wird. |
| `BreakEvenPips` | 21 | Gewinn in Pips, bevor der Stop auf Break-Even verschoben wird (0 deaktiviert). |
| `CandleType` | 5-Minuten-Zeitrahmen | Kerzentyp für Berechnungen. |
| `Volume` | Strategie-Eigenschaft | Ordervolumen für Einstiege (über `Volume` der Strategie festgelegt). |

> **Pip-Verarbeitung**: Pip-basierte Parameter werden mithilfe von `Security.PriceStep` in absolute Preise umgerechnet. Passen Sie `Security.PriceStep` für das gehandelte Instrument an.

## Handelsregeln
### Einstieg
- **Long**: wenn die Stochastik-Signallinie und der RSI beide unter `OversoldLevel` fallen und keine Long-Position besteht.
- **Short**: wenn die Stochastik-Signallinie und der RSI beide über `OverboughtLevel` steigen und keine Short-Position besteht.
- Erscheint ein entgegengesetztes Signal bei offener Position, schließt die Strategie die Position und wartet bis zur nächsten Kerze vor neuen Einstiegen.

### Ausstieg & Risikomanagement
- **Stop-Loss**: fester Abstand definiert durch `StopLossPips`.
- **Take-Profit**: festes Ziel definiert durch `TakeProfitPips`.
- **Break-Even**: sobald der Gewinn `BreakEvenPips` erreicht, wird der Stop auf den Einstiegspreis verschoben.
- **Trailing-Stop**: nach einer günstigen Kursbewegung um `TrailingStopPips` folgt der Stop dem Markt und wird alle `TrailingStepPips` nachgezogen.
- **Gegensignale**: erzwingen einen Ausstieg, auch wenn Stop oder Ziel nicht erreicht wurde.

## Hinweise
- Funktioniert mit jedem von StockSharp unterstützten Instrument; die Standardwerte sind für FX-Pip-Werte optimiert.
- Verwendet ausschließlich abgeschlossene Kerzen, entsprechend dem `TradeAtCloseBar=true`-Verhalten des Original-Robots.
- Portfolio, Instrument und Volumen vor dem Start der Strategie konfigurieren.
