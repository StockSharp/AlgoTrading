# Exp CandlesticksBW Tm-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie reproduziert den MetaTrader-Experten **Exp_CandlesticksBW_Tm** auf der StockSharp High-Level-API. Sie basiert auf Bill Williams' Candlesticks BW-Indikator, der Kerzenfarben durch Kombination des Awesome Oscillators (AO) und des Accelerator Oscillators (AC) bemalt. Momentum-Beschleunigung und -Verlangsamung werden durch Farbübergänge der Kerzen erkannt, während ein optionaler Handelssitzungsfilter Einstiege auf bestimmte Intraday-Stunden beschränkt.

## Funktionsweise

1. Abonniert den konfigurierten Zeitrahmen (Standard **H4**) und fügt jede abgeschlossene Kerze in einen Awesome Oscillator (5/34) ein. Die AO-Serie wird mit einem einfachen gleitenden Durchschnitt von 5 Perioden geglättet, um die Accelerator-Komponente zu erzeugen.
2. Jede Kerze wird in einen von sechs Farbzuständen klassifiziert: zwei bullische Momentum-Farben (AO und AC steigend), zwei bärische Momentum-Farben (AO und AC fallend) und zwei neutrale Farben (gemischte AO/AC-Richtung). Die Richtung des Kerzenkörpers entscheidet zwischen dem dunkleren oder helleren Ton in jedem Paar.
3. Ein Ringpuffer speichert die letzten Farbindizes zusammen mit ihren Öffnungszeiten. Der Parameter **SignalBar** wählt aus, welche historische Bar ausgewertet werden soll (Standard = vorherige Kerze, d.h. Offset 1). Eine Bar weiter zurück wird als Kontext verwendet.
4. Long-Einstiege werden aktiviert, wenn die ältere Bar zu einer bullischen Momentum-Zone gehörte und die Signalbar diese Zone verlässt. Short-Einstiege spiegeln die Logik mit bärischen Zonen. Ausstiegssignale verwenden dieselben Momentum-Filter, um die entgegengesetzte Richtung zu schließen.
5. Der optionale Sitzungsfilter (**UseTimeFilter**) hält ein Handelsprotokoll zwischen **StartHour:StartMinute** und **EndHour:EndMinute**. Das Verlassen des Fensters liquidiert offene Positionen sofort und verhindert Übernacht-Exposure.
6. Stop-Loss- und Take-Profit-Schutzmaßnahmen werden über `StartProtection` registriert, wobei punktbasierte Abstände in Instrumentenpreisschritte umgerechnet werden.

## Handelsregeln

- **Long öffnen**: vorheriger Farbindex `< 2` (AO und AC nach oben beschleunigend) und der Signalbar-Farbindex `> 1`. Long-Einstiege werden übersprungen, wenn bereits long oder wenn Longs deaktiviert sind.
- **Short öffnen**: vorheriger Farbindex `> 3` (AO und AC nach unten beschleunigend) und der Signalbar-Farbindex `< 4`.
- **Long schließen**: ausgelöst, wenn der ältere Farbindex `> 3` (bärische Beschleunigung) und Long-Ausstiege aktiviert sind.
- **Short schließen**: ausgelöst, wenn der ältere Farbindex `< 2` (bullische Beschleunigung) und Short-Ausstiege aktiviert sind.
- Wenn der Zeitfilter aktiv ist, werden Positionen außerhalb der erlaubten Sitzung zwangsweise geschlossen, auch ohne farbbasierte Ausstiegssignale.

## Parameter

| Name | Beschreibung | Standard |
| --- | --- | --- |
| `CandleType` | Zeitrahmen für AO/AC-Berechnungen. | `TimeSpan.FromHours(4).TimeFrame()` |
| `Volume` | Ordergröße für neue Einstiege. | `1m` |
| `SignalBar` | Anzahl abgeschlossener Kerzen, die vor der Signalauswertung übersprungen werden (1 = vorherige Kerze). | `1` |
| `StopLossPoints` | Schutzstop-Abstand in Instrumentenpunkten. Setzen Sie `0` zum Deaktivieren. | `1000m` |
| `TakeProfitPoints` | Gewinnziel-Abstand in Instrumentenpunkten. Setzen Sie `0` zum Deaktivieren. | `2000m` |
| `EnableLongEntries`, `EnableShortEntries` | Eröffnung von Trades in der jeweiligen Richtung erlauben. | `true` |
| `EnableLongExits`, `EnableShortExits` | Schließen von Trades in der jeweiligen Richtung erlauben. | `true` |
| `UseTimeFilter` | Handelssitzungsbeschränkungen aktivieren. | `true` |
| `StartHour`, `StartMinute`, `EndHour`, `EndMinute` | Sitzungsgrenzen (inklusive Beginn, exklusive Ende bei identischen Stunden). | `0/0/23/59` |

## Hinweise

- Die Strategie synchronisiert automatisch die Stop-Loss- und Take-Profit-Abstände mit dem Instrumentenpreisschritt.
- Signale werden mit der Schlusszeit der ausgewerteten Bar zeitgestempelt, sodass wiederholte Trades innerhalb derselben Bar unterdrückt werden.
- Es wird keine Python-Version bereitgestellt, entsprechend der Struktur des MQL-Quellpakets.
