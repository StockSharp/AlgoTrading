# Herkules-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Hercules-Strategie ist eine StockSharp-Portierung des MetaTrader-Experten **Hercules v1.3 (Majors)**. Es kombiniert einen schnellen/langsamen gleitenden Durchschnitt mit Multi-Timeframe-Bestätigungsfiltern und führt zwei unabhängige Gewinnziele pro Signal aus.

## Handelslogik

* **Signalarm** – Berechnen Sie einen schnellen EMA (Standard 1 Periode) beim Schließen der Kerze und einen langsamen SMA (72 Perioden) beim Öffnen der Kerze. Erkennen Sie Überkreuzungen, die im letzten oder vorletzten Takt aufgetreten sind. Der Crossover-Preis wird über beide gleitenden Durchschnitte gemittelt und ein Auslöseniveau wird `TriggerPips` darüber (für Long-Positionen) oder darunter (für Short-Positionen) platziert.
* **Ausführungsfenster** – Sobald ein Crossover erkannt wird, bleibt das Setup für zwei volle Takte gültig. Nur wenn der aktuelle Schlusskurs den Auslösepreis innerhalb dieses Fensters überschreitet, darf die Order ausgelöst werden.
* **Filter** –
  * H1 RSI (Standardlänge 10, typische Preiseingabe) muss für Long-Positionen über `RsiUpper` und für Short-Positionen unter `RsiLower` liegen.
  * Der aktuelle Schlusskurs muss das letzte Hoch/Tief von über `LookbackMinutes` Kerzen im Handelszeitraum durchbrechen.
  * Der tägliche Umschlag (SMA 24 mit ±`DailyEnvelopeDeviation` %) erfordert, dass der Preis außerhalb des Bandes in Handelsrichtung schließt.
  * Der H4-Umschlag (SMA 96 mit ±`H4EnvelopeDeviation` %) fügt eine zweite Bestätigung für einen höheren Zeitrahmen hinzu.
* **Risikomanagement** – der Stop-Loss wird auf das Hoch/Tief des Balkens vier Kerzen zurück gesetzt. Das Volumen kann fest (`OrderVolume`) oder aus `RiskPercent` des aktuellen Portfoliowerts neu berechnet werden.
* **Handelsmanagement** – jedes Signal eröffnet zwei Marktaufträge mit gleichem Volumen. Der erste wird am `TakeProfitFirstPips` liquidiert, der zweite am `TakeProfitSecondPips`. Ein Trailing Stop von `TrailingStopPips` schützt beide Orders. Wenn entweder der Stop oder beide Ziele erreicht sind, tritt für die Strategie eine Blackout-Periode von `BlackoutHours` ein, in der keine neuen Trades getätigt werden.

## Parameter

| Parameter | Beschreibung |
| --- | --- |
| `OrderVolume` | Volumen jeder Marktorder vor Money-Management-Anpassungen. |
| `UseMoneyManagement` | Wenn diese Option aktiviert ist, wird das Volumen aus `RiskPercent` des Portfolios und der aktuellen Stoppdistanz neu berechnet. |
| `RiskPercent` | Prozentsatz des Portfoliowerts zum Risiko pro Setup. |
| `TriggerPips` | Abstand vom Crossover-Preis, der überschritten werden muss, um einen Einstieg zu ermöglichen. |
| `TrailingStopPips` | Trailing-Stop-Distanz in Pips, angewendet auf die kombinierte Position. |
| `TakeProfitFirstPips` | Pip-Abstand des ersten Teil-Take-Profits. |
| `TakeProfitSecondPips` | Pip-Abstand des zweiten Teil-Take-Profits. |
| `FastPeriod` | Länge der schnellen EMA-Triggerlinie. |
| `SlowPeriod` | Länge der langsamen SMA-Basislinie. |
| `RsiPeriod` | Länge des Bestätigungsfilters RSI. |
| `RsiUpper` / `RsiLower` | RSI Schwellenwerte, die Long- und Short-Trades ermöglichen. |
| `LookbackMinutes` | Fenster (in Minuten), das zur Berechnung des aktuellen Hoch-/Tief-Breakout-Filters verwendet wird. |
| `BlackoutHours` | Stundenlange Pause nach einer Ausführung, bevor ein neues Setup akzeptiert wird. |
| `DailyEnvelopePeriod` / `DailyEnvelopeDeviation` | Parameter des täglichen Hüllkurvenfilters. |
| `H4EnvelopePeriod` / `H4EnvelopeDeviation` | Parameter des H4-Hüllkurvenfilters. |
| `CandleType` | Hauptzeitrahmen für die Handelsausführung. |
| `RsiTimeFrame` | Zeitrahmen, der den Indikator RSI speist. |
| `DailyTimeFrame` | Zeitrahmen, der in die Berechnung des täglichen Umschlags einfließt. |
| `H4TimeFrame` | Zeitrahmen, der in die Berechnung des H4-Umschlags einfließt. |

## Dateien

* `CS/HerculesStrategy.cs` – C#-Implementierung der Hercules-Strategie.
* `README.md` – dieses Dokument.
* `README_ru.md` – Russische Beschreibung.
* `README_zh.md` – Chinesische Beschreibung.
