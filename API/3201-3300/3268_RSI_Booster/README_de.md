# RsiBoosterStrategy-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

`RsiBoosterStrategy` ist eine StockSharp-Portierung des MetaTrader Expert Advisors *RSI booster*. Die Strategie vergleicht den schnellen RSI-Wert der aktuellen Kerze mit einem verzögerten RSI, der die vorherige Kerze verwendet. Überschreitet die Differenz ein vom Benutzer festgelegtes Verhältnis, eröffnet die Strategie eine Marktposition und verwaltet den Trade anschließend mit festen Stops, Take-Profit-Zielen, einem optionalen Trailing Stop und einer Kette von Gegenorders zur Verlustaufholung.

Die Strategie basiert auf der High-Level-API von StockSharp. Sie abonniert eine einzelne Kerzenserie, nutzt die integrierten `RelativeStrengthIndex`-Indikatoren und verwendet das Strategie-Parametersystem, damit alle Eingaben in Designer optimiert werden können.

## Handelslogik

1. Auf jeder abgeschlossenen Kerze werden zwei RSI-Indikatoren berechnet.
   * Der schnelle RSI verwendet `FirstRsiPeriod` und `FirstRsiPrice` und liest die neueste Kerze.
   * Der verzögerte RSI verwendet `SecondRsiPeriod` und `SecondRsiPrice`, die Strategie behält jedoch den vorherigen Wert bei, sodass er als Verzögerung um eine Bar wirkt.
2. Wenn `fast RSI - delayed RSI` größer als `Ratio` ist, kauft die Strategie, sofern keine Long-Position offen ist. Liegt die Differenz unter `-Ratio`, verkauft sie, sofern keine Short-Position offen ist.
3. `OnlyOnePositionPerBar` stellt sicher, dass pro Richtung höchstens ein Einstieg für denselben Kerzenzeitstempel erfolgt.
4. Nach jeder Kerze bewertet die Strategie Stop-Loss-, Take-Profit- und Trailing-Regeln. Wird eine der Bedingungen ausgelöst, wird die Position sofort geschlossen.
5. Wird eine Position mit negativem realisiertem PnL geschlossen, kann die optionale Aufhollogik eine Gegenposition (entgegengesetzte Richtung) mit demselben Volumen eröffnen. Die Anzahl verketteter Aufholtrades ist durch `ReturnOrdersMax` begrenzt.

## Risikomanagement

* **Stop-Loss** - über `StopLossPips` in Instrumentenpunkten ausgedrückt. Die Position wird geschlossen, wenn der Preis das Stop-Niveau kreuzt.
* **Take-Profit** - über `TakeProfitPips` in Instrumentenpunkten ausgedrückt.
* **Trailing Stop** - wenn über `TrailingStopPips` aktiviert, beginnt der Stop zu trailen, sobald der Gewinn die konfigurierte Distanz überschreitet. `TrailingStepPips` definiert die minimale Verbesserung, bevor das Trailing-Niveau verschoben wird.
* **Return-Order** - wird aktiviert, wenn `ReturnOrderEnabled` `true` ist. Nach einem Verlusttrade eröffnet die Strategie sofort eine Marktorder in die entgegengesetzte Richtung und zählt dabei, wie viele Aufholorders ausgegeben wurden.

## Parameter

| Parameter | Beschreibung |
|-----------|--------------|
| `Volume` | Handelsvolumen für jede Marktorder (Lots oder Kontrakte). |
| `Ratio` | Minimale RSI-Differenz, die zum Eröffnen einer Position erforderlich ist. |
| `StopLossPips` | Stop-Loss-Distanz in Instrumentenpunkten. |
| `TakeProfitPips` | Take-Profit-Distanz in Instrumentenpunkten. |
| `TrailingStopPips` | Trailing-Stop-Distanz in Instrumentenpunkten. |
| `TrailingStepPips` | Minimale Verbesserung vor dem Verschieben des Trailing Stops. |
| `OnlyOnePositionPerBar` | Verhindert mehrere Einstiege während derselben Kerze. |
| `ReturnOrderEnabled` | Aktiviert die Aufhollogik mit Gegenorder. |
| `ReturnOrdersMax` | Maximale Anzahl aufeinanderfolgender Aufholorders. |
| `FirstRsiPeriod` | Periode des schnellen RSI. |
| `FirstRsiPrice` | Preisquelle für den schnellen RSI (entspricht den angewandten Preis-Modi von MetaTrader). |
| `SecondRsiPeriod` | Periode des verzögerten RSI. |
| `SecondRsiPrice` | Preisquelle für den verzögerten RSI (entspricht den angewandten Preis-Modi von MetaTrader). |
| `CandleType` | Für die Analyse verwendete Kerzenserie. |

## Hinweise

* Die Umrechnung des Preisschritts berücksichtigt den `PriceStep` des Instruments, sofern verfügbar. Stellt das Instrument keinen Preisschritt bereit, wird als Fallback `0.0001` verwendet.
* Der Zähler der Aufholkette wird zurückgesetzt, wenn ein profitabler Trade auftritt oder die konfigurierte Höchstzahl von Aufholorders erreicht ist.
* Die Strategie zeichnet beide RSI-Indikatoren zusammen mit den ausgeführten Trades im Chartbereich, damit sie schnell visuell geprüft werden können.
