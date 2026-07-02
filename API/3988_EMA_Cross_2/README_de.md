# EMA Cross-2-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie ist ein StockSharp-Port des MetaTrader 4 Expert Advisors **"EMA_CROSS_2"** aus dem MQL-Repository. Das Original EA überwacht zwei exponentielle gleitende Durchschnitte (EMAs) und platziert Marktaufträge, wann immer die Durchschnittswerte ausgetauscht werden. Die Konvertierung behält den konträren Charakter des Skripts bei – es kauft, wenn der Long-EMA über den Short-EMA steigt, und verkauft, wenn der Short-EMA über den Long-EMA steigt – und bindet gleichzeitig die Logik in die übergeordnete StockSharp-Strategieinfrastruktur ein.

Die Strategie arbeitet mit abgeschlossenen Kerzen, die vom konfigurierbaren Kerzendatentyp bereitgestellt werden. Signale werden bei Kerzenschluss ausgewertet, um wiederholte Auslöser innerhalb desselben Balkens zu vermeiden. Das Risikomanagement ahmt das MetaTrader-Verhalten nach, indem es Take-Profit-, Stop-Loss- und Trailing-Stop-Distanzen verwendet, die in Brokerpunkten (Preisschritten) ausgedrückt werden.

## Handelslogik
1. **Indikatorberechnung**
   - Berechnen Sie die kurz- und langfristigen EMAs für jede abgeschlossene Kerze.
   - Überspringen Sie die erste Indikatoraktualisierung und passen Sie sie an das ursprüngliche Flag `first_time` an, das die allererste Auswertung ignoriert hat.
   - Erkennen Sie anschließend eine Richtungsänderung, wenn die relative Reihenfolge zwischen dem langen und dem kurzen EMA umkehrt.
2. **Signalinterpretation**
   - Wenn sich der Long-Kurs EMA über den Short-Kurs EMA bewegt, eröffnet der ursprüngliche EA einen Kaufhandel. Der StockSharp-Port behält diese konträre Regel bei, obwohl er sich im Gegensatz zu einem klassischen Crossover-System verhält.
   - Wenn der Short-Kurs EMA über dem Long-Kurs EMA schließt, eröffnet die Strategie einen Verkaufshandel.
   - Neue Positionen sind nur zulässig, wenn derzeit kein Engagement offen ist, wodurch die Bedingung `OrdersTotal() < 1` reproduziert wird.
3. **Auftragsausführung**
   - Trades werden als Market Orders mit einem fest konfigurierbaren Volumen versendet.
   - Beim Einstieg zeichnet die Strategie Stop-Loss- und Take-Profit-Preise unter Verwendung der durch Parameter bereitgestellten Pip-Distanz auf.
4. **Risikomanagement**
   - Bei jeder abgeschlossenen Kerze prüft die Strategie, ob die Preisbewegung die gespeicherten Stop-Loss- oder Take-Profit-Niveaus berührt. Bei Überschreitung eines dieser Level wird die gesamte Position mit einer Marktorder geschlossen.
   - Ein Trailing-Stop (ebenfalls in Broker-Punkten definiert) wird angewendet, sobald sich der Preis um mehr als die Trailing-Distanz günstig bewegt. Bei langen Positionen wird der Schutzanschlag nach oben verschoben; Bei Short-Positionen sinkt der Preis.
   - Wenn die Position flach wird, werden die gespeicherten Schutzniveaus gelöscht.

## Parameter
| Name | Beschreibung | Standard |
| --- | --- | --- |
| `CandleType` | Kerzenserien zur Indikatorberechnung und Signalerkennung. | 15-minütiger Zeitrahmen |
| `OrderVolume` | Volumen jeder Marktorder in Lots/Kontrakten. | 2 |
| `TakeProfitPoints` | Abstand zum Take-Profit-Niveau, ausgedrückt in Brokerpunkten (Preisschritten). Ein Wert von `0` deaktiviert den Take-Profit. | 20 |
| `StopLossPoints` | Abstand zum Stop-Loss-Level, ausgedrückt in Broker-Punkten. Ein Wert von `0` deaktiviert den Stop-Loss. | 30 |
| `TrailingStopPoints` | Distanz, die beim Verfolgen der offenen Position verwendet wird. `0` deaktiviert den Trailing Stop. | 50 |
| `ShortEmaPeriod` | Länge des schnellen EMA. | 5 |
| `LongEmaPeriod` | Länge des langsamen EMA. | 60 |

## Implementierungshinweise
- Die Strategie verwendet `SubscribeCandles().Bind(shortEma, longEma, ProcessCandle)`, um Kerzendaten mit EMA-Indikatoren zu verbinden und folgt dabei dem bevorzugten übergeordneten API-Muster.
- Indikatorwerte werden im Bindungsrückruf als gebrauchsfertige Dezimalzahlen empfangen, sodass keine manuelle Pufferindizierung erforderlich ist.
- Schutzabstände werden durch Multiplikation mit dem Instrument `PriceStep` von MetaTrader Punkten in StockSharp Preise umgerechnet. Wenn das Instrument gebrochene Pip-Preise (3 oder 5 Dezimalstellen) verwendet, berechnet der Helfer die Pip-Größe entsprechend.
- Stop-Loss-, Take-Profit- und Trailing-Verhalten werden intern bei Marktausstiegen implementiert, da StockSharp nicht den gleichen `OrderModify`-Workflow wie MetaTrader 4 bereitstellt. Das resultierende Handelsmanagement spiegelt die ursprüngliche Logik wider: Niveaus werden bei jeder Kerze überprüft und Ausstiege erfolgen sofort, sobald sie durchbrochen werden.
- Die erste Crossover-Auswertung wird absichtlich ignoriert, um die `first_time`-Schutzmaßnahme zu reproduzieren, die vorzeitige Trades im MQL-Skript verhinderte.

## Unterschiede zur MetaTrader-Version
- Geldmanagement: Das ursprüngliche EA handelte immer mit dem Parameter `Lots`. Die Konvertierung macht dasselbe Konzept über `OrderVolume` verfügbar und weist es außerdem der Strategieeigenschaft `Volume` zu, damit Designer und Optimierer es wiederverwenden können.
- Auftragserteilung: MetaTrader hat Stop-Loss und Take-Profit direkt innerhalb von `OrderSend` angewendet. In StockSharp werden diese Niveaus von der Strategie verfolgt und bei Überschreitung mit Marktaufträgen geschlossen.
- Trailing-Stop-Präzision: Die EA verschobenen Stopps mithilfe von Tick-Daten (`Bid`/`Ask`). Der Port aktualisiert die nachgestellte Logik beim Schließen der Kerze. Dies ist die feinste Granularität, die in diesem Beispielprojekt verfügbar ist. Die Abstands- und Aktivierungsregeln bleiben identisch.
- Fehlerbehandlung und Protokollierung wurden vereinfacht; Die StockSharp-Protokollierung liefert detaillierte Informationen über das Standardstrategieprotokoll.

## Nutzungstipps
- Passen Sie `CandleType` an den Zeitrahmen an, der bei Backtests des ursprünglichen EA verwendet wurde, um ein vergleichbares Indikatorverhalten aufrechtzuerhalten.
- Stellen Sie beim Handel mit Symbolen, die mit gebrochenen Pips notiert sind, sicher, dass die konfigurierten Punktabstände die gewünschte Anzahl von Pips widerspiegeln (z. B. entsprechen bei EURUSD `10` Punkte einem Pip).
- Stellen Sie `OrderVolume` auf die von Ihrem Ausführungsplatz erwartete Kontraktgröße ein. Die Strategie führt keine automatische Volumenskalierung durch.
- Verwenden Sie die integrierten Optimierungsschalter für jeden Parameter, um Kombinationen von EMA Zeiträumen und Risikoentfernungen zu erkunden, genau wie Sie Eingaben in MetaTrader optimieren würden.

## Dateien
- `CS/EmaCross2Strategy.cs` – StockSharp Implementierung der Handelslogik.
- `README.md` – Englische Dokumentation (diese Datei).
- `README_zh.md` – Chinesische Übersetzung.
- `README_ru.md` – Russische Übersetzung.
