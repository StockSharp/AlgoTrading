# RSI RFTL Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie portiert den **RSI RFTL EA** von MetaTrader 5 zur StockSharp-High-Level-API. Sie behält die ursprüngliche Idee bei, RSI-Swing-Trendlinien zu handeln, ergänzt durch die Recursive Filter Trend Line (RFTL) als Richtungsfilter. Die Implementierung reproduziert die Balken-für-Balken-Entscheidungsfindung des Experten und verwendet dabei idiomatische StockSharp-Konstrukte wie `StrategyParam`, Indikator-Bindungen und Kerzen-Abonnements.

## Funktionsweise

1. **RSI-Swing-Erkennung** – die letzten 500 RSI-Werte werden auf lokale Hochs und Tiefs gescannt. Gipfel müssen über 40 und 60 steigen, während Täler unter 60 und 40 fallen müssen, entsprechend der MQL-Wendepunktlogik.
2. **Trendlinienprojektion** – sobald zwei gültige Hochs oder Tiefs gefunden wurden, projiziert die Strategie die entsprechende RSI-Trendlinie auf den aktuellen und den vorherigen Balken. Zwischenliegende Swings, die die 40/60-Schwellen brechen, machen die Linie ungültig, genau wie im Experten.
3. **RFTL-Bestätigung** – der vorherige Wert der Recursive Filter Trend Line (berechnet mit der originalen Koeffiziententabelle) muss für Shorts über dem vorherigen Schlusskurs oder für Longs darunter liegen. Dies hält Einstiege mit dem RFTL-Filter ausgerichtet.
4. **Einstiegsfilterung** – RSI muss sich auch auf der richtigen Seite des Neutralpunkts befinden: Shorts erfordern RSI über 47/50, während Longs RSI unter 55/50 erfordern.
5. **Risikoschicht** – Schutz-Stop, Take-Profit und Trailing-Stop-Abstände werden in Pips ausgedrückt und bei jeder abgeschlossenen Kerze aktualisiert, womit die MQL-Trailing-Modifikationsroutine imitiert wird. Zusätzliche Ausstiege erfolgen, wenn RSI 70 überschreitet (Longs schließen) oder unter 30 fällt (Shorts schließen).

## Einstiegslogik

- **Short-Aufstellung**
  - Zwei RSI-Tiefs unter 60/40 definieren eine steigende Trendlinie, deren Projektion jetzt nach unten gebrochen wird (`RSI[1] < Linie`, `RSI[2] > Linie(vorherige)`).
  - Der vorherige RFTL-Wert liegt über dem vorherigen Schluss, was Abwärtsdruck bestätigt.
  - RSI bleibt auf der bullischen Seite (`RSI[2] > 50`, `RSI[0] > 47`) und die erkannten Hochs liegen weiter zurück in der Geschichte als die Tiefs (`pos₂ > pos₄`), entsprechend der MQL-Reihenfolgebeschränkung.
- **Long-Aufstellung**
  - Zwei RSI-Hochs über 40/60 definieren eine fallende Trendlinie, deren Projektion jetzt nach oben gebrochen wird (`RSI[1] > Linie`, `RSI[2] < Linie(vorherige)`).
  - Der vorherige RFTL-Wert liegt unter dem vorherigen Schluss.
  - RSI bleibt auf der bärischen Seite (`RSI[2] < 50`, `RSI[0] < 55`) und die jüngsten Tiefs sind aktueller als die Hochs (`pos₄ > pos₂`).

Signale werden nur ausgewertet, nachdem alle Indikatoren gebildet sind und die notwendige Geschichte angesammelt wurde, was vorzeitige Trades auf unvollständigen Daten verhindert.

## Risikomanagement

- **Stop Loss / Take Profit** – in Pips konfigurierbar. Wenn die aktuelle Kerze über das jeweilige Preisniveau hinaus handelt, wird die Position sofort geschlossen und der Trailing-Zustand zurückgesetzt.
- **Trailing Stop** – optional. Sobald sich der Preis um `TrailingStopPips + TrailingStepPips` zugunsten des Trades bewegt, folgt der Stop dem Schlusskurs, wobei vor dem nächsten Anziehen dieselbe Mindestvorwärtsbewegung (`TrailingStepPips`) durchgesetzt wird.
- **RSI-Notausstieg** – Longs schließen, wenn RSI 70 kreuzt; Shorts schließen, wenn er unter 30 fällt. Dies spiegelt die Hard-Exits im Original-EA wider.

## Parameter

| Parameter | Standard | Beschreibung |
| --- | --- | --- |
| `CandleType` | 1 Stunde | Zeitrahmen für RSI- und RFTL-Berechnungen. |
| `TradeVolume` | 1 | Ordervolumen bei jedem Einstieg. |
| `RsiPeriod` | 30 | Lookback-Periode des RSI-Oszillators. |
| `StopLossPips` | 50 | Schutz-Stop-Abstand in Pips (0 deaktiviert den Stop). |
| `TakeProfitPips` | 50 | Take-Profit-Abstand in Pips (0 deaktiviert das Ziel). |
| `TrailingStopPips` | 5 | Trailing-Stop-Offset in Pips (0 deaktiviert das Trailing). |
| `TrailingStepPips` | 5 | Zusätzliche Pip-Verbesserung vor dem Trailing-Update. |

Alle Abstände werden mit dem Instrument-`PriceStep` multipliziert, entsprechend der Punkt/Pip-Handhabung der MQL-Version.

## Verwendung

1. Die Strategie an ein Wertpapier anhängen und `CandleType` auf die in MetaTrader-Tests verwendete Balkengröße setzen.
2. Die Risikoparameter (Stop, Take, Trailing) auf die zuvor verwendeten Pip-Abstände anpassen. Ein Parameter auf `0` zu setzen deaktiviert diesen Schutz.
3. Strategie starten; sie wird die angegebenen Kerzen abonnieren, RSI und RFTL berechnen und mit der Signalüberwachung beginnen, sobald genug Geschichte gesammelt wurde.
4. Die Diagramm-Widgets überwachen – der Preisbereich zeigt Kerzen und die RFTL-Linie, während das zweite Fenster den RSI-Oszillator anzeigt.

## Hinweise und Unterschiede

- Der RFTL-Indikator ist direkt in C# mit der originalen Koeffiziententabelle implementiert; keine externen Dateien erforderlich.
- Das Trade-Management bleibt auf eine einzelne Position beschränkt: Die Strategie wechselt zwischen Long, Short und Flat, genau wie der EA, der nur eine Position pro Symbol/Magic verfolgte.
- Da Stop- und Trailing-Ausstiege innerhalb der Strategie gehandhabt werden (StockSharp führt MT5-Stops nicht automatisch aus), werden Wiedereinstiege auf dem Balken übersprungen, wo ein Schutzausstieg ausgelöst wird, was eine konservative, aber sichere Näherung ist.
- Historische Puffer sind auf 600 Datensätze begrenzt, um die 500-Element-Arrays im Quellcode zu spiegeln und gleichzeitig unbegrenztes Speicherwachstum zu vermeiden.
- Alle Inline-Kommentare wurden in Englisch umgeschrieben und der Code folgt den StockSharp-High-Level-API-Stilrichtlinien.
