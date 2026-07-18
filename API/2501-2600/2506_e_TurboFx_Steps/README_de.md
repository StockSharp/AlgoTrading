# e-TurboFx Steps-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **e-TurboFx**-Strategie ist ein Momentum-Erschöpfungs-Umkehrsystem, das ursprünglich für MetaTrader 5 geschrieben wurde. Es überwacht die zuletzt abgeschlossenen Kerzen und sucht nach Sequenzen, bei denen die Kerzenkörper immer mehr in dieselbe Richtung expandieren. Eine wachsende Serie bearischer Kerzen zeigt Kapitulation an und stellt damit ein potenzielles Long-Setup dar, während eine wachsende Serie bullischer Kerzen eine mögliche Short-Gelegenheit ankündigt. Der StockSharp-Port verwendet die High-Level-API mit Kerzenabonnements und automatisiertem Positionsschutz.

## Handelslogik
- Inspizieren Sie die letzten `DepthAnalysis` abgeschlossenen Kerzen des ausgewählten `CandleType`.
- Zählen Sie, wie viele aufeinanderfolgende Kerzen unter ihrer Eröffnung schlossen (bearisch) und wie viele über ihrer Eröffnung schlossen (bullisch).
- Verfolgen Sie die Körpergrößenprogression: jede neue Kerze in der Sequenz muss einen größeren absoluten Körper als die vorherige haben. Wenn diese Bedingung nicht erfüllt ist, wird die Sequenz zurückgesetzt.
- **Long-Einstieg:** `DepthAnalysis` aufeinanderfolgende bearische Kerzen mit streng expandierenden Körpern lösen einen Markt-Kauf aus, sofern derzeit keine Position offen ist.
- **Short-Einstieg:** `DepthAnalysis` aufeinanderfolgende bullische Kerzen mit streng expandierenden Körpern lösen einen Markt-Verkauf aus, ebenfalls nur wenn die Position flach ist.
- Während eine Position aktiv ist, pausiert die Strategie die Signalerkennung, um das Stapeln von Trades zu vermeiden. Das Risikomanagement wird an den beim Start konfigurierten eingebauten Schutzblock delegiert.

## Positionsverwaltung
- `StartProtection` registriert automatisch Stop-Loss- und Take-Profit-Orders mit Distanzen, die in Preisschritten (Exchange-Ticks) gemessen werden. Das Setzen einer Distanz auf null deaktiviert den entsprechenden Schutzauftrag.
- Die Strategie hält immer nur eine offene Position. Wenn nach dem Schließen des vorherigen Trades ein neues Signal erscheint, werden die Kerzensequenzen von Grund auf neu aufgebaut, basierend auf frischen Marktdaten.
- Markteinstiege verwenden den `TradeVolume`-Parameter. Das Ändern des Parameters in der UI aktualisiert sofort das Strategie-Volumen.

## Parameter
| Parameter | Beschreibung | Standard |
|-----------|--------------|---------|
| `DepthAnalysis` | Anzahl der zuletzt abgeschlossenen Kerzen, die zur Validierung des Expansionsmusters verwendet werden. Höhere Werte verlangen längere Strähnen vor dem Handel. | `3` |
| `TakeProfitSteps` | Take-Profit-Distanz in Exchange-Preisschritten (Ticks). `0` deaktiviert den Take-Profit. | `120` |
| `StopLossSteps` | Stop-Loss-Distanz in Exchange-Preisschritten (Ticks). `0` deaktiviert den Stop-Loss. | `70` |
| `TradeVolume` | Ordervolumen, das mit jedem Markteinstieg gesendet wird. | `0.1` |
| `CandleType` | Kerzendatentyp (Zeitrahmen), der für die Analyse abonniert wird. | `1 Stunde` Zeitrahmen |

Alle numerischen Parameter haben Optimierungsmetadaten, sodass sie bei Bedarf in StockSharp-Optimierungen einbezogen werden können.

## Hinweise und Empfehlungen
- Der ursprüngliche MQL5 Expert Adviser berechnete Kerzendaten bei jedem Tick neu; die StockSharp-Implementierung erreicht dasselbe Verhalten mit abgeschlossenen Kerzenereignissen und internen Zählern.
- Da die Strategie auf Kerzenkörpervergleichen basiert, ist sie sensibel gegenüber dem ausgewählten Zeitrahmen. Kürzere Zeitrahmen erzeugen mehr Signale, können aber engere Stops erfordern.
- Stellen Sie sicher, dass das verbundene Instrument einen gültigen `PriceStep` bereitstellt, damit Stop-Loss- und Take-Profit-Distanzen, die in Schritten definiert sind, korrekt in Preise übersetzt werden.
- Validieren Sie das Verhalten vor dem Live-Handel im Designer/Backtester, um zu bestätigen, dass die Stop- und Zieldistanzen mit dem gewählten Instrument übereinstimmen.
