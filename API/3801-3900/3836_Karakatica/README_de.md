# Karakatica-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die Karakatica-Strategie ist ein mittelfristiges Trendfolgesystem, das vom ursprünglichen MetaTrader 4-Expertenberater „Exp_karakatica“ portiert wurde. Die Strategie handelt standardmäßig **EUR/USD im M15-Zeitrahmen** und verwendet eine benutzerdefinierte Signal-Engine, die das Verhalten des ursprünglichen „iKarakatica“-Indikators mit einem Crossover-Modell mit gleitendem Durchschnitt emuliert. Der Crossover wird bei jedem Balken neu berechnet und die Signalperiode wird kontinuierlich neu optimiert, um dem profitabelsten aktuellen Regime zu folgen.

Die Strategie geht nur dann mit Marktaufträgen in den Markt, wenn derzeit keine Position offen ist. Schutzaufträge (Stop-Loss und Take-Profit) werden automatisch über das Schutzsubsystem StockSharp angehängt.

## Handelslogik
1. **Signalgenerierung** – Die Strategie berechnet einen einfachen gleitenden Durchschnitt (SMA) der Schlusskurse der Kerze. Ein zinsbullisches Signal erscheint, wenn die vorherige Kerze unter oder bei SMA schloss, während die zuletzt beendete Kerze darüber schließt. Ein rückläufiges Signal wird erzeugt, wenn die vorherige Kerze über oder bei SMA schloss und die letzte Kerze darunter schließt. Signale werden immer auf dem *vorherigen* abgeschlossenen Balken ausgewertet, um die MT4-Implementierung widerzuspiegeln, die Shift=1-Werte vom `iKarakatica`-Indikator verwendet hat.
2. **Positionsmanagement** –
   - Tritt während einer offenen Position ein gegenteiliges Signal auf, wird die Position sofort mit einer Market-Order geschlossen.
   - Neue Trades sind nur zulässig, wenn keine Position vorhanden ist und die Strategie nicht durch die Optimierungsphase blockiert wird. Aufeinanderfolgende Geschäfte in die gleiche Richtung werden blockiert, bis der Markt ein bestätigtes Gegensignal erzeugt.
3. **Auftragsgröße** – Die Positionsgröße wird vom konfigurierten `Risk`-Parameter abgeleitet. Der Algorithmus wandelt das Risiko basierend auf dem aktuellen Portfoliowert in ein gewünschtes Volumen um und gleicht es dann mit dem Volumenschritt des Instruments ab, wobei er die Losberechnungsmethode des ursprünglichen Expertenberaters nachahmt.
4. **Handelsschutz** – Stop-Loss- und Take-Profit-Abstände werden in Preispunkten festgelegt. Sie werden durch Multiplikation des Punktwerts mit der Preisstufe des Instruments in absolute Preise umgerechnet.

## Adaptive Optimierung
Der Fachberater optimiert die Signalperiode kontinuierlich neu, um sich an das sich ändernde Marktverhalten anzupassen:

1. Alle `ReoptimizeEvery` Balken startet die Strategie eine historische Simulation, die `OptimizationDepth` vorherige Balken abdeckt.
2. Für jeden Kandidatenzeitraum im Bereich `[OptimizationStart, OptimizationEnd]` mit einem Schritt `OptimizationStep` simuliert der Backtester ein einfaches Crossover-Modell mit gleitendem Durchschnitt:
   - Der Simulator verfolgt eine aktive virtuelle Position und aktualisiert ihren Gewinn, wann immer das entgegengesetzte Signal ausgelöst wird.
   - Zusätzlich zum kombinierten Gewinn werden separate Gewinnzähler für Long- und Short-Trades geführt.
3. Nach dem Scannen aller Kandidaten wendet die Strategie die folgenden Regeln an:
   - Wenn sowohl Long- als auch Short-Gewinne negativ sind, wird der Handel in beide Richtungen bis zum nächsten Optimierungszyklus deaktiviert.
   - Wenn die besten Long- und Short-Ergebnisse gleich sind, wird der insgesamt beste Zeitraum verwendet und beide Richtungen bleiben aktiviert.
   - Ansonsten bleibt nur die Richtung mit dem höchsten Gewinn aktiviert und die entsprechende beste Periode wird ausgewählt.

Für den Start der Optimierung sind mindestens `OptimizationDepth + OptimizationEnd + 2` abgeschlossene Kerzen erforderlich. Bis genügend Historie gesammelt ist, verzögert die Strategie den Handel.

## Parameter
| Name | Beschreibung | Standard | Optimierbar |
| ---- | ----------- | ------- | ----------- |
| `Risk` | Prozentsatz des Portfoliowerts (pro 1000 Einheiten), der das Zielauftragsvolumen definiert. | 0,5 | Ja |
| `StopLossPoints` | Stop-Loss-Distanz in Preispunkten. | 50 | Ja |
| `TakeProfitPoints` | Take-Profit-Distanz in Preispunkten. | 150 | Ja |
| `Period` | Aktiver Zeitraum von SMA, der für die Signalgenerierung verwendet wird. Wird vom Optimierer automatisch aktualisiert. | 70 | Ja |
| `OptimizationDepth` | Anzahl der historischen Balken, die für den In-Sample-Backtest verwendet wurden. | 250 | Nein |
| `ReoptimizeEvery` | Häufigkeit der Optimierungsläufe, gemessen in fertigen Stäben. | 50 | Nein |
| `OptimizationStart` | Bei der Optimierung berücksichtigter Mindestzeitraum. | 10 | Nein |
| `OptimizationStep` | Wechseln Sie zwischen benachbarten Perioden. | 5 | Nein |
| `OptimizationEnd` | Maximaler Zeitraum, der bei der Optimierung berücksichtigt wird. | 150 | Nein |
| `CandleType` | Datentyp der Kerzen (standardmäßig 15-Minuten-Zeitrahmen). | M15-Zeitrahmenkerzen | Nein |

## Nutzungshinweise
- Die Strategie wurde für EUR/USD im 15-Minuten-Zeitrahmen entwickelt. Wenn Sie auf ein anderes Instrument portieren, überprüfen Sie bitte den Punktwert, den Volumenschritt und die Spread-Annahmen.
- Stellen Sie sicher, dass der Datenfeed die besten Geld-/Briefkurse liefert. Sie werden verwendet, um den Handels-Spread während des Optimierungsprozesses abzuschätzen. Wenn die Kurse nicht verfügbar sind, greift der Algorithmus auf einen einzelnen Preisschritt-Spread zurück.
- Da die Optimierungslogik mehrere hundert historische Balken erfordert, sollten Sie der Strategie erlauben, Daten vorab zu laden, bevor Sie den Live-Handel aktivieren.

## Dateien
- `CS/KarakaticaStrategy.cs` – StockSharp Umsetzung der Strategie.
- `README.md` – Englische Beschreibung (diese Datei).
- `README_ru.md` – Russische Beschreibung.
- `README_zh.md` – Chinesische Beschreibung.
