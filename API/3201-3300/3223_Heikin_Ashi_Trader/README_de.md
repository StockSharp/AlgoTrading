# Heikin Ashi Trader-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie portiert den MetaTrader 4 Experten "Heikin Ashi Trader" nach StockSharp. Sie behält die Multi-Indikator-Bestätigungslogik des ursprünglichen Roboters und implementiert sie mit der High-Level-Kerzensubskriptions-API, sodass jede Entscheidung ausschließlich auf abgeschlossenen Bars basiert.

## Details
- **Indikatoren**:
  - Heikin-Ashi-Kerzen, berechnet aus dem Arbeitszeitrahmen.
  - Zwei linear gewichtete gleitende Durchschnitte (LWMA) mit dem typischen Kerzenpreis (`(high + low + close) / 3`).
  - Ein stochastischer Oszillator (`%K/%D/Smooth`-Perioden sind benutzerkonfigurierbar).
  - Momentum (Abstand vom neutralen 100-Level).
  - Moving Average Convergence Divergence (MACD).
- **Einstiegskriterien**:
  - **Long**: Die letzte Heikin-Ashi-Kerze muss bullisch sein, mindestens einer der letzten drei stochastischen Werte muss über dem Überkauft-Level liegen, der schnelle LWMA muss über dem langsamen LWMA liegen, der Momentum-Abstand von 100 muss den Kaufschwellenwert überschreiten, und die MACD-Linie muss über ihrem Signal liegen.
  - **Short**: Spiegelbedingungen — bärische Heikin-Ashi-Kerze, Stochastik unter dem Überverkauft-Level, schneller LWMA unter langsamem LWMA, Momentum-Abstand über dem Verkaufsschwellenwert, und MACD-Linie unter ihrem Signal.
  - Optionales Glätten der entgegengesetzten Exposition vor dem neuen Trade (`CloseOppositePositions`).
- **Positionsmanagement**:
  - Fester Stop-Loss und Take-Profit in Pips (abgeleitet aus dem Wertpapier-Preisschritt).
  - Optionaler Trailing-Stop, der dem Schlusskurs folgt, sobald der Trade um `TrailingStopPips` vorrückt.
  - Break-Even-Logik, die den Stop nach `BreakEvenTriggerPips` Preisfortschritt auf `Entry ± BreakEvenOffsetPips` verschiebt.
  - Manueller Kill-Switch (`ForceExit`) zum Glätten aller Positionen bei der nächsten Kerze.
- **Unterschiede zur MT4-Version**:
  - Der ursprüngliche EA bewertete Momentum auf einem höheren Zeitrahmen. Dieser Port behält dieselben Indikatorperioden bei, liest sie aber aus dem primären Kerzenstream, um innerhalb der StockSharp High-Level-API zu bleiben. Parameter ermöglichen die Anpassung der Schwellenwerte für die ursprüngliche Sensitivität.
  - Geldbasierte Stop-Regeln aus dem MT4-Code sind nicht enthalten. Risiko wird durch preisbasierte Stops und das Break-Even-Modul verwaltet.

## Parameter
| Name | Beschreibung |
| --- | --- |
| `CandleType` | Zeitrahmen (oder ein anderer Kerzentyp) für alle Indikatoren und Handelsentscheidungen. |
| `FastMaPeriod`, `SlowMaPeriod` | Perioden des schnellen und langsamen linear gewichteten gleitenden Durchschnitts (typischer Preis). |
| `StochasticKPeriod`, `StochasticDPeriod`, `StochasticSlowing` | `%K/%D`-Längen und Glättungsfaktor des stochastischen Oszillators. |
| `StochasticOverbought`, `StochasticOversold` | Stochastische Schwellenwerte, die während der letzten drei abgeschlossenen Werte gekreuzt werden müssen. |
| `MomentumPeriod` | Länge des Momentum-Indikators. |
| `MomentumBuyThreshold`, `MomentumSellThreshold` | Minimaler absoluter Abstand von der 100-Linie für Long-/Short-Trades. |
| `MacdFastPeriod`, `MacdSlowPeriod`, `MacdSignalPeriod` | MACD-Konfiguration. |
| `CloseOppositePositions` | Entgegengesetzte Seite vor einem neuen Trade schließen. |
| `MaxPositions` | Maximale Nettoexposition pro Richtung (`0` = unbegrenzt). |
| `TradeVolume` | Volumen jeder neuen Order; auch dem Strategie-`Volume` zugewiesen. |
| `UseStopLoss`, `StopLossPips` | Schutzenden Stop in Pips aktivieren und dimensionieren. |
| `UseTakeProfit`, `TakeProfitPips` | Take-Profit in Pips aktivieren und dimensionieren. |
| `UseTrailingStop`, `TrailingStopPips` | Trailing-Stop-Logik aktivieren und Distanz definieren. |
| `UseBreakEven`, `BreakEvenTriggerPips`, `BreakEvenOffsetPips` | Break-Even-Aktivierungsdistanz und gesperrter Offset. |
| `ForceExit` | Wenn `true`, werden alle Positionen bei der nächsten verarbeiteten Kerze geschlossen. |

## Implementierungshinweise
- Die Strategie abonniert Kerzen über `SubscribeCandles().BindEx(...)`, sodass Indikatoren abgeschlossene Werte erhalten und der Code `GetValue()` nie direkt aufruft.
- Pip-Konvertierung verwendet den Instrument-`PriceStep`; wenn der Markt fraktionale Pips notiert, Wertpapier-Schritt entsprechend konfigurieren.
- Trailing- und Break-Even-Updates verschieben den Stop nur in die günstige Richtung. Reset-Logik löscht zwischengespeicherte Stop-/Zielwerte bei jedem geschlossenen Trade, damit neue Positionen mit frischen Risikoeinstellungen starten.
