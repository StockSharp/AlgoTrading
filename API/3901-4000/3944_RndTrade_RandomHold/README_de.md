# RndTrade-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Umwandlung des ursprünglichen MQL4 „RndTrade“-Expertenberaters in eine StockSharp-High-Level-Strategie, die völlig zufällige Markteintritte durchführt und diese nach einer festgelegten Haltedauer wieder verlässt.

## Kernlogik

1. Abonnieren Sie den konfigurierten Kerzentyp (standardmäßig 1-Minuten-Kerzen) und warten Sie auf abgeschlossene Balken.
2. Wenn die Strategie flach ist, generieren Sie eine Zufallszahl. Ein Wert über 0,5 löst einen Marktkauf aus, andernfalls einen Marktverkauf, jeweils unter Verwendung des konfigurierten Handelsvolumens.
3. Notieren Sie die Kerzenzeit des Einstiegs und halten Sie die Position für die ausgewählte Haltedauer (standardmäßig vier Stunden) offen.
4. Nach Ablauf der Haltezeit schließen Sie die gesamte Position mit der entsprechenden Marktorder.

## Parameter

| Name | Beschreibung | Standard |
| --- | --- | --- |
| `CandleType` | Datentyp von Kerzen, die die Zufallsentscheidungslogik auslösen. | 1 Minute Kerzen |
| `TradeVolume` | Für jede zufällige Marktorder verwendetes Volumen. | 1 |
| `HoldDuration` | Zeitspanne, um jede geöffnete Zufallsposition aktiv zu halten, bevor sie geschlossen wird. | 4 Stunden |

## Zusätzliche Hinweise

- Der Zufallsgenerator wird automatisch neu gesetzt, wenn die Strategie beginnt, das MQL4-Verhalten der Verwendung der Ortszeit als Startwert nachzuahmen.
- Es werden nur Marktaufträge verwendet, die den ursprünglichen Expertenberater widerspiegeln, der Geschäfte ohne ausstehende Aufträge sofort ausführte.
- Es sind keine zusätzlichen Indikatoren oder historischen Puffer erforderlich; Die Strategie basiert nur auf den Zeitstempeln der eingehenden Kerzen und dem internen Timer.
