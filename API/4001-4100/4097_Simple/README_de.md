# Einfache Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
The **Simple Strategy** is the StockSharp high-level conversion of the MetaTrader 4 expert advisor `S!mple.mq4` located in `MQL/9019`. Das ursprüngliche System überwachte einen festen Korb von Forex-Symbolen und handelte immer dann, wenn ein linear gewichteter gleitender Durchschnitt über 50 Perioden einen einfachen gleitenden Durchschnitt über 200 Perioden kreuzte. Jeder Eintrag konnte beliebig oft wiederholt werden und jedem Trade wurden optionale geldbasierte Stop-Loss- und Take-Profit-Levels zugeordnet. Die Konvertierung behält dieselbe Logik bei, stellt alle Benutzereingaben als Strategieparameter bereit und protokolliert dieselben Diagnoseinformationen, die EA im Terminalkommentar MetaTrader ausgegeben hat.

## Handelslogik
1. **Datenvorbereitung.** Die Strategie abonniert einen konfigurierbaren Kerzentyp (standardmäßig Fünf-Minuten-Kerzen) und bindet beide gleitenden Durchschnitte über das übergeordnete `SubscribeCandles().Bind(...)` API.
2. **Moving average crossover.** Two historical values of every moving average are buffered. A buy signal occurs when the fast LWMA was below the slow SMA two bars ago and closed above it on the previous finished bar. A sell signal is detected when the inverse condition happens.
3. **Tracking der Trendmarge.** Der langsame SMA-Wert, der vor `TrendMargin` Balken aufgetreten ist, wird zwischengespeichert, um den textuellen Trendbericht von EA zu reproduzieren. Die Live-Verlangsamung SMA wird mit dieser Referenz verglichen, um den Hintergrundtrend als `UP`, `DOWN` oder `WAIT` zu klassifizieren, zusammen mit der in Preisschritten ausgedrückten Entfernung.
4. **Ausführungsmodell.**
   - When a buy signal is triggered, any short exposure is closed before buying up to `NumOrders * TradeVolume`. Das angeforderte Volumen spiegelt das Verhalten von EA wider, bei dem mehrere identische Bestellungen gestapelt wurden, bis die maximale Anzahl erreicht war.
   - A sell signal closes long exposure first and then sells up to the same aggregated target volume.
5. **Schutzniveaus.** Optionale geldbasierte Stopps und Ziele (`StopLossMoney`, `TakeProfitMoney`) werden mithilfe des Instruments `PriceStep`/`StepPrice` und des Auftragswerts `TradeVolume` in Preisabstände übersetzt. Sobald die Werte gespeichert sind, prüft jede fertige Kerze den Hoch-/Tief-Bereich; if a level is breached the position is flattened at market.
6. **Betriebsschutz.** Die tatsächliche Auftragserteilung wird nur ausgeführt, wenn `EnableTrading` auf `true` gesetzt ist, wodurch das ursprüngliche `makeTrades`-Flag repliziert wird, das die Ausführung von EA im Modus „Nur Analyse“ ermöglicht.

## Risikomanagement und Geldstopps
- Stop-loss and take-profit amounts are interpreted as cash risk/target per entry block (per MetaTrader order). The conversion uses the security metadata (`PriceStep`, `StepPrice`) to convert that amount into a rounded number of price steps. If either field is missing, a warning is logged and the monetary stops remain disabled.
- Die Schutzniveaus werden auf dem Hoch/Tief jeder abgeschlossenen Kerze bewertet und entsprechen den Tick-Level-Prüfungen, die von EA durchgeführt werden, während sie innerhalb des High-Level-Frameworks von StockSharp bleiben.
- `StartProtection()` is invoked on start so that account-level protections configured in StockSharp remain active while the strategy runs.

## Parameter
| Name | Standard | Beschreibung |
| --- | --- | --- |
| `TradeVolume` | `0.1` | Volumen einer einzelnen MetaTrader-ähnlichen Bestellung. The base `Strategy.Volume` is kept in sync with this value. |
| `NumOrders` | `1` | Maximum number of volume blocks that can be accumulated in the same direction. Das endgültige Zielvolumen beträgt `TradeVolume * NumOrders`. |
| `StopLossMoney` | `0` | Optionaler Stop-Loss-Betrag in Kontowährung pro Volumenblock. Auf Null setzen, um den Stopp zu deaktivieren. |
| `TakeProfitMoney` | `0` | Optionaler Take-Profit-Betrag in Kontowährung pro Volumenblock. Auf Null setzen, um das Ziel zu deaktivieren. |
| `TrendMargin` | `10` | Anzahl der fertigen Kerzen, die zur Erstellung des Hintergrundtrendtextes verwendet wurden (langsam SMA im Vergleich zu ihrem Wert vor `TrendMargin` Balken). |
| `FastLength` | `50` | Length of the fast linear weighted moving average. |
| `SlowLength` | `200` | Länge des langsamen einfachen gleitenden Durchschnitts. |
| `EnableTrading` | `false` | Bei `false` protokolliert die Strategie nur Signale, genau wie bei EA bei `makeTrades=false`. |
| `CandleType` | `5m time-frame` | Kerzentyp, der für Indikatorberechnungen verwendet wird. |

## Hinweise zur Konvertierung
- Der MetaTrader EA durchlief sechs hartcodierte Forex-Symbole. StockSharp strategies operate on the `Strategy.Security` supplied by the user. Um das Korbhandelsverhalten zu reproduzieren, starten Sie entweder mehrere Instanzen der Strategie (eine pro Instrument) oder binden Sie sie in eine übergeordnete Strategie ein, die dieselben Signale an mehrere Wertpapiere sendet.
- Geldbasierte Schutzniveaus basieren auf den Metadaten des Instruments. Stellen Sie bei Forex-Paaren sicher, dass sowohl `PriceStep` als auch `StepPrice` ausgefüllt sind (z. B. `0.0001` und der Pip-Wert pro Lot). Andernfalls wird der Stopp-/Zielabstand nach der Protokollierung einer Warnung stillschweigend als Null behandelt.
- Die bei jeder abgeschlossenen Kerze ausgegebene Protokollmeldung spiegelt den EA-Kommentar wider: Sie listet das Signal (`BUY`, `SELL` oder `WAIT`), beide gleitenden Durchschnitte, den Abstand zwischen ihnen in Preisschritten und die aus dem verzögerten langsamen SMA erhaltene Trendbewertung auf.
- Die Anzahl der gestapelten Aufträge wird als aggregiertes Zielvolumen modelliert. Dadurch bleibt das Gesamtrisiko mit der ursprünglichen Implementierung identisch, während die High-Level-Market-Order-Helfer von StockSharp anstelle mehrerer einzelner `OrderSend`-Aufrufe verwendet werden.
- Es wurde noch kein Python-Port erstellt, der den Aufgabenanforderungen entspricht.

## Nutzungstipps
- Weisen Sie ein Forex-Wertpapier mit den korrekt konfigurierten Werten `PriceStep`, `StepPrice` und `VolumeStep` zu. Stellen Sie `TradeVolume` auf die gewünschte Losgröße ein und aktivieren Sie den Handel, sobald Sie mit der protokollierten Diagnose zufrieden sind.
- Um das Standardverhalten von EA nachzuahmen (nur Analyse), belassen Sie `EnableTrading` bei `false`. Wenn Sie zum Handel bereit sind, stellen Sie den Schalter auf `true` und das nächste Crossover-Signal sendet Marktaufträge.
- Da Schutzniveaus bei Kerzenschlüssen überwacht werden, sollten Sie die Verwendung kürzerer Kerzen in Betracht ziehen, wenn Sie im Vergleich zum Tick-für-Tick-Verhalten von MetaTrader eine stärkere Intrabar-Reaktion benötigen.
