# Die durchschnittliche Kanalstrategie wurde korrigiert
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Corrected Average Channel Strategy** ist eine C#-Portierung des MetaTrader-Expertenberaters `e-CA-5`. Das System erstellt den Indikator „Corrected Average“ (CA) jedes Mal neu, wenn eine Kerze schließt und eröffnet eine Position, wenn der Preis den korrigierten gleitenden Durchschnitt um einen konfigurierbaren Sigma-Offset kreuzt. Die konvertierte Implementierung basiert auf der High-Level-Kerze API von StockSharp, verwendet Marktaufträge und verwaltet intern schützende Exits (Stop-Loss, Take-Profit, Trailing Stop), um das Verhalten des ursprünglichen Expert Advisors widerzuspiegeln.

## Korrigierter Durchschnittsindikator
Der CA-Filter kombiniert einen gleitenden Durchschnitt mit Volatilitätsrückmeldung. Die MQL-Version stellt drei Eingaben bereit: Länge des gleitenden Durchschnitts, Mittelungsmethode und angewandter Preis. Im StockSharp-Port:

1. Der Typ des gleitenden Durchschnitts wird über den Parameter `MaTypeOption` (SMA, EMA, SMMA, LWMA) und die Länge `MaPeriod` ausgewählt.
2. Ein `StandardDeviation`-Indikator mit demselben Zeitraum misst die aktuelle Volatilität.
3. Für jede fertige Kerze wird der korrigierte Wert iterativ berechnet:
   - Sei `M_t` der MA-Wert des letzten Balkens und `CA_{t-1}` der korrigierte Wert des vorherigen Balkens.
   - Berechnen Sie `v1 = StdDev_t^2` und `v2 = (CA_{t-1} - M_t)^2`.
   - Wenn `v2 <= 0` oder `v2 < v1`, behalten Sie den Korrekturfaktor `k = 0` bei. Andernfalls legen Sie `k = 1 - v1 / v2` fest.
   - Aktualisieren Sie `CA_t = CA_{t-1} + k * (M_t - CA_{t-1})`.
   - Der allererste korrigierte Wert ist standardmäßig der gleitende Durchschnitt selbst.

Diese Rückkopplungsschleife dämpft den MA in ruhigen Zeiten und ermöglicht schnelle Anpassungen, wenn der Preis über die aktuelle Volatilitätsschätzung hinaus abweicht.

## Handelslogik
1. Die Strategie abonniert den konfigurierten Kerzentyp (`CandleType`) und wartet, bis sowohl der gleitende Durchschnitt als auch die Standardabweichung vollständig gebildet sind.
2. Sobald eine Kerze endet, berechnet der Algorithmus den neuen korrigierten Wert und vergleicht den Schlusskurs der vorherigen Kerze mit dem vorherigen korrigierten Niveau.
3. Zwei Sigma-Offsets, `SigmaBuyPoints` und `SigmaSellPoints`, werden mithilfe des `PriceStep` des Instruments in Preisabstände umgewandelt.
4. Die Einstiegsregeln verwenden den vorherigen Kerzenschluss und das neu berechnete korrigierte Niveau:
   - **Kaufen**, wenn der vorherige Schlusskurs unter dem korrigierten Durchschnitt plus dem Kauf-Sigma lag und der aktuelle Schlusskurs über dieser Obergrenze endet.
   - **Verkaufen**, wenn der vorherige Schlusskurs über dem korrigierten Durchschnitt minus dem Verkaufs-Sigma lag und der aktuelle Schlusskurs unterhalb dieser unteren Grenze endet.
5. Es ist nur eine Nettoposition zulässig. Ein neuer Trade wird nur eingereicht, wenn kein Exposure vorliegt.

Da die StockSharp-Version mit fertigen Kerzen arbeitet, erfolgt die Ausbruchsbestätigung einmal pro Balken statt bei jedem Tick, wodurch ein deterministisches Verhalten bereitgestellt wird, das für Backtesting und Live-Automatisierung mit Kerzendaten geeignet ist.

## Risikomanagement
Der Port reproduziert alle drei Schutzmechanismen des ursprünglichen Expert Advisors:

- **Fester Stop-Loss**: `StopLossPoints` multipliziert mit dem Preisschritt definiert den Abstand zwischen dem Einstiegspreis und dem schützenden Stop. Ein ausgelöster Stop schließt die gesamte Position mit einer Marktorder.
- **Fester Take-Profit**: `TakeProfitPoints` wird in eine Gewinnzielentfernung umgewandelt. Wenn der Preis während einer Kerze das Niveau erreicht, wird die Position mit einer Marktorder geschlossen.
- **Trailing Stop**: Wenn `TrailingPoints` größer als Null ist, verfolgt die Strategie nicht realisierte Gewinne und speichert, sobald der Preis mindestens um diese Distanz gestiegen ist, einen Trailing-Level hinter dem letzten Schlusskurs. Der Trailing Stop bewegt sich nur vorwärts und berücksichtigt `TrailingStepPoints`, was die minimale Verbesserung darstellt, bevor ein neues Trailing-Level akzeptiert wird. Nachfolgende Level werden mit `Security.ShrinkPrice` gerundet, sodass sie mit der Tick-Größe des Instruments übereinstimmen.

Alle Exits setzen den internen Risikostatus zurück. Wenn das nächste Signal erscheint, werden die Stop-, Ziel- und Trailing-Levels aus dem neuen Füllpreis neu berechnet, wodurch ein Verhalten sichergestellt wird, das der MQL-Version ähnelt, die den ursprünglichen Orderschutz ändert.

## Parameter
| Parameter | Beschreibung |
| --- | --- |
| `OrderVolume` | Menge, die für Markteintritte verwendet wird. Muss positiv sein. |
| `TakeProfitPoints` | Gewinnziel in Preisschritten (0 deaktiviert den Take-Profit). |
| `StopLossPoints` | Stop-Loss-Distanz in Preisschritten (0 deaktiviert den Stop-Loss). |
| `TrailingPoints` | Erforderliche Gewinndistanz (in Preisschritten), bevor der Trailing Stop aktiviert wird. |
| `TrailingStepPoints` | Minimale zusätzliche Distanz, die erfasst werden muss, bevor der Trailing Stop erneut verschoben wird. |
| `MaPeriod` | Zeitraum sowohl des gleitenden Durchschnitts als auch der Standardabweichung. |
| `MaTypeOption` | Typ des gleitenden Durchschnitts: SMA, EMA, SMMA oder LWMA. |
| `SigmaBuyPoints` | Der Sigma-Offset wurde über dem korrigierten Durchschnitt hinzugefügt, bevor eine Long-Position eröffnet wurde. |
| `SigmaSellPoints` | Der Sigma-Offset wurde unter den korrigierten Durchschnitt subtrahiert, bevor eine Short-Position eröffnet wurde. |
| `CandleType` | Kerzenserien zur Indikatorberechnung und Signalauswertung. |

Alle numerischen Parameter unterstützen die Optimierung durch `SetCanOptimize(true)`, sodass die Strategie direkt in der StockSharp-Umgebung kalibriert werden kann.

## Nutzungshinweise
- Der Standardkerzentyp ist eine Stunde. Passen Sie es an den Zeitrahmen an, der bei der Optimierung der ursprünglichen MetaTrader-Strategie verwendet wurde.
- `Security.PriceStep` wird verwendet, um alle „Punkte“-Eingaben in tatsächliche Preisentfernungen zu übersetzen. Instrumente ohne konfigurierten Schritt greifen auf `1` zurück, wodurch sinnvolles Verhalten für Indizes oder Kryptowährungen erhalten bleibt.
- Die Strategie wird nur bei fertigen Kerzen ausgeführt. Wenn Intrabar-Präzision erforderlich ist, verringern Sie den Zeitrahmen auf die gewünschte Granularität.
- Trailing-Stops werden bei Verletzung mit Marktaufträgen implementiert und ahmen den ursprünglichen EA nach, der die Stop-Loss-Preise modifizierte. Dieser Ansatz vermeidet die Platzierung zusätzlicher Stop-Orders und sorgt dafür, dass das Risikomanagement in der Strategie selbst enthalten bleibt.
- Gemäß den Aufgabenanforderungen wird für diese Konvertierung keine Python-Version bereitgestellt.

## Unterschiede zum Original EA
- Das kerzenbasierte API von StockSharp ersetzt die Verarbeitung auf Tick-Ebene. Alle Entscheidungen werden getroffen, wenn eine Kerze schließt.
- Die Auftragsverwaltung erfolgt saldiert: Gegensätzliche Positionen werden nicht gleichzeitig gehalten, was der Einzelauftragslogik der MetaTrader-Version entspricht.
- Schutzstopps und Trailing-Exits werden über Marktaufträge ausgeführt, anstatt bestehende Auftragsscheine zu ändern. Dieses Verhalten ist bei Netting-Konten gleichwertig, wobei die Implementierung mit anderen StockSharp-Strategien konsistent bleibt.

Diese Anpassungen bewahren die Handelsidee von `e-CA-5` und richten die Logik gleichzeitig an den Best Practices von StockSharp und den allgemeinen API-Konventionen aus, die in den Repository-Richtlinien beschrieben sind.
