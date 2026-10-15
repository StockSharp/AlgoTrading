# Exp Cronex Chaikin-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie portiert den MetaTrader-Expert-Advisor **Exp_CronexChaikin.mq5** auf die High-Level-API von StockSharp. Der ursprüngliche Roboter rekonstruiert den Chaikin-Oszillator aus Akkumulations-/Distributionswerten, glättet ihn zweimal mit Cronex-„XMA"-Filtern und handelt Kreuzungen zwischen der schnellen und langsamen Linie. Die StockSharp-Version reproduziert dieselbe Logik und macht jede Phase als konfigurierbare Parameter verfügbar.

## Handelslogik

1. Die konfigurierte Kerzenserie (`CandleType`) abonnieren.
2. Die Akkumulations-/Distributionslinie (AD) für jede abgeschlossene Kerze mit dem ausgewählten `VolumeSource` (Tick- oder echtes Volumen) neu berechnen.
3. Den Chaikin-Oszillator anwenden, indem die AD-Linie mit zwei gleitenden Durchschnitten (`ChaikinFastPeriod`, `ChaikinSlowPeriod`, `ChaikinMethod`) geglättet und ihre Differenz genommen wird.
4. Den resultierenden Oszillator zweimal mit den Cronex-Filtern glätten, die durch `SmoothingMethod`, `FastPeriod`, `SlowPeriod` und `Phase` gesteuert werden. Diese beiden geglätteten Werte entsprechen den „schnellen" und „Signal"-Linien im ursprünglichen Indikator.
5. `SignalBar` abgeschlossene Kerzen zurückblicken und beide Cronex-Linien auf dieser und der vorherigen Kerze vergleichen.
6. Wenn die schnelle Linie über der langsamen liegt, schließt die Strategie optional Short-Positionen und, wenn `BuyOpenEnabled` true ist, öffnet eine Long-Position, wenn ein frisches Aufwärtskreuz auf der Lookback-Kerze erkannt wurde.
7. Wenn die schnelle Linie unter der langsamen liegt, werden für Short-Trades die entgegengesetzten Aktionen ausgeführt, gesteuert durch `SellOpenEnabled` und `BuyCloseEnabled`.
8. Wann immer eine neue Position geöffnet wird, werden Stop-Loss- und Take-Profit-Orders (in Punkten ausgedrückt) mit `StopLoss` und `TakeProfit` neu berechnet.

Es wird nur eine einzige Nettoposition gehalten. Wenn sich die Signalrichtung ändert, kombiniert die Strategie das Volumen, das zum Schließen der aktuellen Position erforderlich ist, mit der neuen Handelsgröße, um das Netting-Verhalten von MetaTrader nachzuahmen.

## Indikatoren und Glättungsoptionen

- **Chaikin-Oszillator**: Aufgebaut durch Anwenden des ausgewählten `ChaikinMethod`-Gleitdurchschnittstyps auf die Akkumulations-/Distributionslinie. Verfügbare Optionen umfassen einfache, exponentielle, geglättete und linear gewichtete Durchschnitte.
- **Cronex-Glätter**: Der Parameter `SmoothingMethod` stellt die Cronex-XMA-Familie zur Verfügung (SMA, EMA, SMMA, LWMA, Jurik JJMA/JurX, Parabolic MA, T3, VIDYA, AMA). Der Parameter `Phase` beeinflusst Jurik-basierte Filter genau wie in der MQL-Implementierung.

## Parameter

| Parameter | Beschreibung |
|-----------|-------------|
| `CandleType` | Datentyp der Kerzen zur Berechnung des Indikators. Standard ist ein Vier-Stunden-Zeitrahmen. |
| `ChaikinMethod` | Gleitdurchschnittstyp im Chaikin-Oszillator. |
| `ChaikinFastPeriod` / `ChaikinSlowPeriod` | Schnelle und langsame Perioden für die Akkumulations-/Distributionslinie. |
| `SmoothingMethod` | Cronex-Glättungsalgorithmus für die Chaikin-Oszillatorwerte. |
| `FastPeriod` / `SlowPeriod` | Längen der schnellen und langsamen Cronex-Linien. |
| `Phase` | Phasenparameter für Jurik-basierte Glätter (Bereich -100 bis +100). |
| `VolumeSource` | Wählt Tick- oder echtes Volumen für die Akkumulations-/Distributionslinie. |
| `SignalBar` | Anzahl der abgeschlossenen Balken zurück, die das Kreuzsignal enthalten müssen. |
| `BuyOpenEnabled` / `SellOpenEnabled` | Long- oder Short-Trades aktivieren oder deaktivieren. |
| `BuyCloseEnabled` / `SellCloseEnabled` | Schließen der entgegengesetzten Position bei inversem Signal erlauben. |
| `TakeProfit` / `StopLoss` | Gewinnziel und schützende Stop-Distanzen in Instrumentenpunkten nach jedem Einstieg. |
| `Volume` | Standard-StockSharp-Positionsgröße (entspricht der Lot-Größe im ursprünglichen Experten). |

## Unterschiede zur MQL-Version

- Geldmanagement- und Slippage-Routinen aus `TradeAlgorithms.mqh` werden durch die integrierten `Volume`-, `SetStopLoss`- und `SetTakeProfit`-Helfer ersetzt.
- Die StockSharp-Implementierung berechnet die AD-Linie nur bei abgeschlossenen Kerzen neu, was deterministisches Verhalten für Tests und Live-Trading sicherstellt.
- Cronex-Glättungsoptionen basieren auf StockSharp-Indikatoren: Jurik-Filter werden durch `JurikMovingAverage` (mit Phasenkontrolle) unterstützt, während VIDYA und ParMA exponentielle Näherungen verwenden, die mit anderen Cronex-Konvertierungen konsistent sind.
