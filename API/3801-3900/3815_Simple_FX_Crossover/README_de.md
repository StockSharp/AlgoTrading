# Einfache FX-Crossover-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Zusammenfassung
- High-Level-Port des MetaTrader 4 Expert Advisors *simplefx2.mq4* (Simple FX 2.0).
- Handelt mit Kreuzungen zwischen einem schnellen und einem langsamen einfachen gleitenden Durchschnitt bei fertigen Kerzen.
- Hält nur eine Position offen und wird umgedreht, wenn sich der vorherrschende Trend umkehrt.

## Handelslogik
1. Erstellen Sie Kerzen mithilfe des konfigurierbaren Zeitrahmenparameters.
2. Berechnen Sie zwei einfache gleitende Durchschnitte (schnell und langsam) für Kerzenschlusskurse.
3. Bestätigen Sie einen Aufwärtstrend, wenn sowohl die aktuelle als auch die vorherige Kerze den schnellen MA über dem langsamen MA anzeigen. Bestätigen Sie einen rückläufigen Trend, wenn beide Kerzen den schnellen MA unterhalb des langsamen MA anzeigen.
4. Wenn der bestätigte Trend vom gespeicherten Trendstatus abweicht, schließen Sie alle entgegengesetzten Positionen und eröffnen Sie sofort eine Marktorder in die neue Richtung mit dem konfigurierten Volumen.
5. Optional können Stop-Loss- und Take-Profit-Schutzmaßnahmen aktiviert werden, die in Preisschritten ausgedrückt werden. Sie nutzen den integrierten Schutzdienst von StockSharp, um die MT4-Risikoeinstellungen zu emulieren.

Die Strategie verarbeitet nur fertige Kerzen, niemals Intrabar-Ticks, um dem ursprünglichen Verhalten des Expertenberaters nahe zu bleiben. Jeder neue Eintrag wird protokolliert, sodass jede Crossover-Entscheidung überprüft werden kann.

## Parameter
| Name | Beschreibung | Standard | Optimierung |
| --- | --- | --- | --- |
| `ShortPeriod` | Länge des schnellen einfachen gleitenden Durchschnitts. | 50 | 10 → 150 Schritt 5 |
| `LongPeriod` | Länge des langsamen einfachen gleitenden Durchschnitts. | 200 | 50 → 400 Schritt 10 |
| `Volume` | Auftragsvolumen, das bei jedem Markthandel übermittelt wird. | 0,1 | 0,1 → 2 Schritt 0,1 |
| `StopLossPoints` | Schutzstoppabstand in Instrumentenpreisschritten (0 deaktiviert). | 0 | — |
| `TakeProfitPoints` | Gewinnzielentfernung in Instrumentenpreisschritten (0 deaktiviert). | 0 | — |
| `CandleType` | Für die Analyse verwendeter Kerzenzeitrahmen. | 1 Stunde | — |

## Hinweise und Unterschiede zur MT4-Version
- Die MT4-Persistenzdatei (`simplefx.dat`) ist nicht erforderlich; Die letzte Trendrichtung wird im Speicher vom Strategiestatus verfolgt.
- Slippage-, Order-Kommentar-, Magic Number- und Pfeilfarbenoptionen des ursprünglichen Expert Advisors werden nicht angezeigt, da StockSharp das Routing anders handhabt.
- Stop-Loss- und Take-Profit-Abstände werden in **Preisschritten** (Instrumenten-Ticks) interpretiert. Passen Sie sie an die Pip-Definition Ihres Brokers an.
- Es kann immer nur eine Position offen sein; Die Strategie basiert auf `ClosePosition()`, bevor die Richtung geändert wird, um einen sauberen Wechsel zwischen Long- und Short-Trades sicherzustellen.

## Nutzung
1. Hängen Sie die Strategie an ein Wertpapier/Instrument an und legen Sie den gewünschten Kerzenzeitrahmen fest.
2. Konfigurieren Sie gleitende Durchschnittsperioden und Risikoparameter.
3. Starten Sie die Strategie; Es abonniert Kerzen, verwaltet den Trendstatus und übermittelt Marktaufträge, wenn ein Crossover bei zwei aufeinanderfolgenden Kerzen bestätigt wird.
