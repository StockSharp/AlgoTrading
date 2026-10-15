# Broadening-Top-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die Broadening-Top-Strategie ist ein trendfolgendes System, inspiriert vom ursprünglichen MetaTrader-Expert-Advisor "Broadening top". Die Strategie konzentriert sich darauf, Ausbrüche nach einer sich verbreiternden Formation zu erfassen, indem sie Trendrichtung und Momentum-Bestätigung kombiniert. Zwei linear gewichtete gleitende Durchschnitte, ein Momentum-Oszillator und ein MACD-Filter arbeiten zusammen, um bullische und bärische Ausbrüche zu erkennen.

## Handelslogik
1. **Trendfilter:** Die Strategie vergleicht einen schnellen und einen langsamen linear gewichteten gleitenden Durchschnitt (LWMA). Long-Trades erfordern, dass die schnelle LWMA über der langsamen LWMA liegt, während Short-Trades das Gegenteil erwarten.
2. **Momentum-Bestätigung:** Der Momentum-Oszillator wird auf den letzten drei abgeschlossenen Kerzen beobachtet. Ein Trade ist nur erlaubt, wenn einer dieser Werte mindestens um die konfigurierte Schwelle vom neutralen Niveau (100) abweicht (getrennte Werte für Longs und Shorts).
3. **MACD-Ausrichtung:** Ein zusätzlicher Filter prüft die MACD-Linie gegen ihre Signallinie. Long-Positionen werden nur ausgelöst, wenn die MACD-Linie über der Signallinie liegt; Shorts, wenn sie darunter liegt.
4. **Positionsbehandlung:** Vor dem Öffnen eines Trades in Gegenrichtung schließt die Strategie die aktuelle Position und stellt sicher, dass jeweils nur eine Position aktiv ist.

## Risikomanagement
Die Strategie verwendet `StartProtection` zur Verwaltung von Schutzorders:
- Optionale Stop-Loss- und Take-Profit-Distanzen in Preisschritten (Pips).
- Ein optionaler Trailing Stop mit konfigurierbarem Trailing-Schritt.

## Parameter
| Parameter | Beschreibung | Standard |
|-----------|-------------|---------|
| `OrderVolume` | Ordergröße in Lots/Kontrakten. | 1 |
| `FastMaLength` | Länge des schnellen linear gewichteten gleitenden Durchschnitts. | 6 |
| `SlowMaLength` | Länge des langsamen linear gewichteten gleitenden Durchschnitts. | 85 |
| `MomentumPeriod` | Rückblickperiode des Momentum-Oszillators. | 14 |
| `MomentumBuyThreshold` | Mindestabstand vom neutralen Momentum-Niveau (100), um Long-Einstiege zu erlauben. | 0.3 |
| `MomentumSellThreshold` | Mindestabstand vom neutralen Momentum-Niveau (100), um Short-Einstiege zu erlauben. | 0.3 |
| `MacdFast` | Schnelle EMA-Länge innerhalb des MACD. | 12 |
| `MacdSlow` | Langsame EMA-Länge innerhalb des MACD. | 26 |
| `MacdSignal` | Signal-EMA innerhalb des MACD. | 9 |
| `TakeProfitPips` | Take-Profit-Distanz in Preisschritten. | 50 |
| `StopLossPips` | Stop-Loss-Distanz in Preisschritten. | 20 |
| `TrailingStopPips` | Trailing-Stop-Distanz in Preisschritten. | 40 |
| `TrailingStepPips` | Zusätzliche Distanz, bevor der Trailing Stop aktualisiert wird. | 10 |
| `CandleType` | Für Berechnungen verwendeter Kerzentyp/Zeitframe. | 15-Minuten-Zeitrahmen |
| `EnableLongs` | Long-Trades aktivieren oder deaktivieren. | true |
| `EnableShorts` | Short-Trades aktivieren oder deaktivieren. | true |

## Indikatoren
- **LinearWeightedMovingAverage:** schnelle und langsame Trendfilter.
- **Momentum:** bestätigt Marktbeschleunigung weg vom neutralen Niveau.
- **MovingAverageConvergenceDivergenceSignal:** liefert Richtungsbestätigung über MACD- und Signallinien.

## Nutzungshinweise
- Momentum-Schwellen werden auf den drei jüngsten abgeschlossenen Kerzen ausgewertet, um das ursprüngliche MQL-Verhalten nachzuahmen.
- Schutzorders (Stop-Loss, Take-Profit, Trailing Stop) sind optional und können durch Setzen der entsprechenden Distanz auf null deaktiviert werden.
- Die Strategie muss an Instrumente gebunden werden, die Preisschritt- und Dezimalinformationen liefern, um die Pip-Größe korrekt zu berechnen.
