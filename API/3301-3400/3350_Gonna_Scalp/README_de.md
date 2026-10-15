# Gonna Scalp-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die Gonna Scalp-Strategie ist ein hochfrequenter MetaTrader-Expertenberater, der auf die StockSharp-Hochebene API portiert wurde. Das System sucht auf einem kurzfristigen Chart nach schnellen Mean-Reversion-Einträgen und respektiert dabei den vorherrschenden Markttrend. Die Bestätigung erfolgt durch einen Abstimmungsmechanismus, der Momentum-, CCI-, ATR-, stochastische Oszillator- und MACD-Filter bewertet, bevor ein Handel zugelassen wird. Es kann jeweils nur eine Position offen sein und jeder Trade ist durch feste Stop-Loss- und Take-Profit-Abstände geschützt, ausgedrückt in MetaTrader Punkten.

## Handelslogik

1. **Indikatorvorbereitung**
   - Schnelle und langsame gewichtete gleitende Durchschnitte (WMA), berechnet auf Basis typischer Preise.
   - Momentum (Periode 14) wird im Handelszeitraum bewertet und in einen absoluten Abstand vom neutralen Wert 100 umgerechnet.
   - Commodity Channel Index (Periode 20) und Average True Range (Periode 12) werden als Richtungsfilter verwendet.
   - Stochastic-Oszillator %K/%D (5/3/3) und MACD (26.12.9) wurden auf derselben Kerzenserie verarbeitet.
2. **Signalabstimmung**
   - Jeder Indikator gibt eine Stimme für die bullische oder bärische Seite, wenn sein aktueller Wert den im ursprünglichen MetaTrader-Code identifizierten Trend unterstützt.
   - Die Strategie erfasst drei aktuelle Momentum-Distanzen und erfordert, dass mindestens eine davon einen konfigurierbaren Schwellenwert überschreitet, bevor ein neuer Trade zugelassen wird.
   - Zusätzliche Strukturprüfungen erfordern, dass das Tief des Balkens vor zwei Kerzen unter dem Hoch des vorherigen Balkens für Long-Positionen bleibt (Spiegelbedingung für Shorts).
3. **Auftragsausführung**
   - Wenn die bullischen Stimmen die bärischen Stimmen übersteigen und alle Filter übereinstimmen, eröffnet die Strategie eine Long-Position mit der konfigurierten Losgröße.
   - Wenn die bärischen Stimmen die bullischen Stimmen überwiegen und der Momentumfilter zustimmt, wird eine Short-Position eröffnet.
4. **Risikomanagement**
   - Jeder offene Handel wird von festen Stop-Loss- und Take-Profit-Abständen begleitet, die in MetaTrader Punkten gemessen und in Instrumentenpreisschritte übersetzt werden.
   - Die Schutzlogik schließt die Position auf der aktuellen Kerze, sobald eines der Niveaus durchbrochen wurde.

## Schlüsselparameter

| Name | Beschreibung | Standard |
| ---- | ----------- | ------- |
| `TradeVolume` | Basisauftragsgröße in Losen nach Volumenausrichtung. | `0.01` |
| `FastMaPeriod` | Länge des schnellen WMA-Filters. | `1` |
| `SlowMaPeriod` | Länge des langsamen WMA-Filters. | `5` |
| `MomentumPeriod` | Anzahl der vom Momentum-Indikator verwendeten Balken. | `14` |
| `MomentumBuyThreshold` | Minimale absolute Impulsabweichung, die für lange Einstiege erforderlich ist. | `0.3` |
| `MomentumSellThreshold` | Minimale absolute Impulsabweichung, die für Short-Einstiege erforderlich ist. | `0.3` |
| `StopLossSteps` | Stop-Loss-Distanz, ausgedrückt in MetaTrader Punkten. | `200` |
| `TakeProfitSteps` | Take-Profit-Distanz ausgedrückt in MetaTrader Punkten. | `200` |
| `CandleType` | Für alle Indikatoren verwendeter Zeitrahmen (standardmäßig 5-Minuten-Kerzen). | `M5` |

## Nutzungshinweise

- Passen Sie das Strategievolumen an das gehandelte Instrument an, indem Sie `TradeVolume` anpassen; Die Implementierung normalisiert es automatisch auf den Austauschlosschritt.
- Die Stop-Loss- und Take-Profit-Parameter arbeiten in MetaTrader Punkten. Sie werden basierend auf der Präzision des Instruments in Instrumentenpreiseinheiten umgerechnet.
- Aufgrund des Momentum-History-Puffers sind mindestens drei abgeschlossene Kerzen erforderlich, bevor die Abstimmungslogik Signale erzeugen kann.
- Die Strategie vermeidet bewusst Pyramidenbildung; Ein neuer Trade wird erst dann eröffnet, wenn die vorherige Position durch das Risikomanagement oder ein gegenteiliges Signal geschlossen wurde.
- Sie können die Strategie mit StockSharp-Diagrammen verbinden, um die WMAs, stochastischen und MACD-Reihen zur Signalvalidierung zu visualisieren.
