# Auto Adjusting-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

AutoAdjustingStrategy repliziert den MetaTrader-Experten *Aouto Adjusting1* mit der High-Level-API von StockSharp. Die Portierung behält den ursprünglichen Multi-Timeframe-Momentum-Filter, die monatliche MACD-Trendbestätigung und einen Drei-EMA-Stapel zur Erkennung von trendkonformen Rücksetzern. Stops und Ziele werden aus aktuellen Schwingungs-Extremen projiziert und bei jeder abgeschlossenen Kerze automatisch angepasst.

## Kernlogik

1. **Trendstruktur** – drei exponentielle gleitende Durchschnitte auf dem Handelszeitrahmen (6, 14, 26) müssen ausgerichtet sein (`EMA6 < EMA14 < EMA26` für Longs, umgekehrt für Shorts). Die vorherige Kerze muss die mittlere EMA berühren, während die Kerze davor ein höheres Tief / niedrigeres Hoch bildet, um einen Rücksetzer zu bestätigen.
2. **Momentum-Bestätigung** – Momentum auf dem höheren Zeitrahmen (abgebildet vom Handelszeitrahmen, z.B. H1 → D1) muss bei mindestens einer der letzten drei abgeschlossenen Balken um mindestens `MomentumBuyThreshold` / `MomentumSellThreshold` von 100 abweichen.
3. **Makro-Filter** – ein monatliches MACD(12, 26, 9)-Signal stellt sicher, dass Trades mit dem dominanten Trend ausgerichtet sind (`MACD > Signal` für Käufe, `<` für Verkäufe).
4. **Ausführung** – Market-Orders werden gesendet, sobald alle Filter übereinstimmen und keine entgegengesetzte Exposition vorhanden ist. Entgegengesetzte Positionen werden vor dem Einstieg in die neue Richtung abgebaut.
5. **Schutz** – Stop-Loss-Niveaus werden um einen konfigurierbaren Pip-Puffer über das niedrigste Tief / höchste Hoch der letzten `CandlesBack` Balken platziert. Take-Profit-Abstände werden mit `RewardRatio` skaliert. Sowohl Stop als auch Ziel werden bei jedem Kerzenschluss neu aktiviert, während die Position aktiv ist.

## Risiko und Positionsgröße

Die Strategie spiegelt die ursprüngliche Risikoparametrisierung wider:

- `RiskPercent` berechnet eine adaptive Positionsgröße, wenn Portfoliowert und Preisschritt-Metadaten verfügbar sind. Der Algorithmus teilt den erlaubten Geldverlust durch den Verlust pro Einheit, der durch die aktuelle Stop-Distanz impliziert wird.
- Wenn risikobasiertes Sizing nicht bewertet werden kann (z.B. fehlende Portfolio-Statistiken), fällt die Engine auf den festen `TradeVolume`-Parameter zurück.

## Parameter

| Name | Typ | Standard | Beschreibung |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | `TimeFrame(H1)` | Handelszeitrahmen für den EMA-Stapel. |
| `MomentumCandleType` | `DataType` | Abgeleitet von `CandleType` | Höherer Zeitrahmen für den Momentum-Indikator (H1→D1, H4→W1, usw.). |
| `MacroMacdCandleType` | `DataType` | `TimeFrame(30 days)` | Zeitrahmen für die Makro-MACD-Bestätigung (standardmäßig monatlich). |
| `PadAmount` | `decimal` | `3` | Extra Pips über Schwingungs-Extreme bei der Stop-Berechnung. |
| `RiskPercent` | `decimal` | `0.1` | Prozent des Portfolio-Eigenkapitals pro Trade riskiert. |
| `RewardRatio` | `decimal` | `2` | Multiplikator für die Stop-Distanz zur Take-Profit-Platzierung. |
| `CandlesBack` | `int` | `3` | Anzahl der Kerzen für die Schwingungs-Hoch/Tief-Erkennung. |
| `MomentumBuyThreshold` | `decimal` | `0.3` | Mindest-Momentum-Abweichung für Long-Einstiege. |
| `MomentumSellThreshold` | `decimal` | `0.3` | Mindest-Momentum-Abweichung für Short-Einstiege. |
| `TradeVolume` | `decimal` | `1` | Fallback-Losgröße wenn risikobasiertes Sizing nicht verfügbar. |

## Diagramme und Visualisierung

- Den Handelszeitrahmen abonnieren und die drei EMAs einzeichnen, um Rücksetzer zu beobachten.
- Die Momentum-Serie im Panel des höheren Zeitrahmens verfolgen, um Energie-Schwellenwerte zu bestätigen.
- MACD-Werte des Makro-Zeitrahmens überwachen, um den Trendfilter zu validieren.

## Hinweise

- Das automatische Zeitrahmen-Mapping entspricht dem MQL-Experten: M1→M15, M5→M30, M15→H1, M30→H4, H1→D1, H4→W1, D1→MN1. Andere Zeitrahmen behalten ihren ursprünglichen Wert.
- Die Strategie vermeidet `GetValue`-Aufrufe auf Indikatoren, indem sie die jüngsten Werte innerhalb der Strategie speichert und über die Bind-Callbacks weiterleitet.
- Das Trailing-Verhalten spiegelt den ursprünglichen EA wider, indem die Schutzniveaus bei jedem Kerzenschluss neu berechnet werden.
