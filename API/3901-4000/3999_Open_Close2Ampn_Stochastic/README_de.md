# Open Close2 Ampn Stochastic Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
- Port des MetaTrader 4 Expert *open_close2ampnstochastic_strategy*, neu aufgebaut auf dem StockSharp High-Level API.
- Verwendet einen klassischen Stochastic-Oszillator (Länge 9, Glättung 3/3) zusammen mit einem Preisaktionsfilter mit zwei Balken: Die aktuelle Kerze muss die Richtung der vorherigen fortsetzen, bevor eine Order gesendet wird.
- Konzipiert für den Einzelpositionshandel. Die Standardkerzenquelle ist eine Stunde, aber über den Parameter `CandleType` kann ein beliebiger Zeitrahmen eingegeben werden.

## Signallogik
1. **Eingangsschutz** – es kann jeweils nur eine Position geöffnet sein. Wenn die Strategie flach ist, wertet sie die letzte vollständig geformte Kerze aus:
   - **Long-Einstieg**, wenn die Stochastic-Hauptlinie über der Signallinie liegt *und* sowohl der Eröffnungs- als auch der Schlusskurs der letzten Kerze unter ihren vorherigen Werten liegen (Anhalten des Abwärtsdrucks, gefolgt von der Oszillatorstärke).
   - **Kurzer Einstieg**, wenn die Hauptlinie Stochastic unter der Signallinie liegt *und* die Kerze einen höheren Eröffnungs- und Schlusskurs als die vorherige zeigt (Aufwärtsschub mit Bestätigung des rückläufigen Oszillators).
2. **Ausstiegsregeln** – solange eine Position besteht, gelten die gleichen Bedingungen in umgekehrter Reihenfolge:
   - **Long schließen**, wenn die Hauptlinie unter die Signallinie fällt und die neue Kerze höhere Eröffnungs-/Schlusskurse druckt.
   - **Short schließen**, wenn die Hauptlinie über die Signallinie steigt und die neue Kerze niedrigere Eröffnungs-/Schlusskurse druckt.
3. **Drawdown Guard** – repliziert den MT4-Notausstieg: Wenn die schwebende Verlustgröße (realisierter PnL + aktuelle kerzenbasierte Schätzung) `MaximumRisk × account_margin` erreicht, liquidiert die Strategie die Position sofort. StockSharp stellt die *AccountMargin* von MetaTrader nicht zur Verfügung, daher nähert sich der Port dieser über `Portfolio.BlockedValue` an und greift auf `Portfolio.CurrentValue` zurück, wenn die blockierte Marge nicht verfügbar ist.

## Money-Management
- **BaseVolume** spiegelt die ursprüngliche `Lots`-Eingabe wider und wird immer dann verwendet, wenn keine Kontoinformationen verfügbar sind.
- Wenn eine Portfoliobewertung vorliegt, beträgt die Rohauftragsgröße `Portfolio.CurrentValue × MaximumRisk / 1000` und entspricht der ursprünglichen `AccountFreeMargin`-basierten Größenbestimmung.
- Nach jedem Verlusthandel wird die nächste Position um `losses / DecreaseFactor` reduziert; Der Streak-Zähler wird nach einem profitablen Trade zurückgesetzt. Die resultierende Größe darf niemals unter `MinimumVolume` fallen, der wie beim Skript MQL standardmäßig 0,1 Lose beträgt.
- Alle berechneten Volumina werden vor dem Senden von Marktaufträgen an die Instrumentenlimits (`VolumeStep`, `MinVolume`, `MaxVolume`) angepasst.

## Parameter
| Name | Typ | Standard | Beschreibung |
| --- | --- | --- | --- |
| `BaseVolume` | dezimal | `0.1` | Ersatz-Ordergröße, wenn die risikobasierte Größenbestimmung nicht berechnet werden kann. |
| `MaximumRisk` | dezimal | `0.3` | Anteil des Eigenkapitals, der sowohl für die dynamische Größenbestimmung als auch für den Drawdown-Schutz verwendet wird. Auf `0` setzen, um Risikoberechnungen zu deaktivieren. |
| `DecreaseFactor` | dezimal | `100` | Divisor nach aufeinanderfolgenden Verlusten angewendet. Höhere Werte verlangsamen die Reduzierung. |
| `MinimumVolume` | dezimal | `0.1` | Absoluter Mindestwert für das berechnete Volumen. |
| `StochasticLength` | int | `9` | Rückblickperiode des Stochastic-Oszillators. |
| `StochasticKLength` | int | `3` | Glättungszeitraum der %K-Linie. |
| `StochasticDLength` | int | `3` | Glättungsperiode der %D-Signalleitung. |
| `CandleType` | `DataType` | `TimeFrame(1h)` | Kerzenquelle, die zum Ansteuern der Indikator- und Preisfilter verwendet wird. |

## Implementierungshinweise
- Der für den Notausgang erforderliche variable PnL wird mit dem letzten Kerzenschluss und `Strategy.PositionPrice` geschätzt. Dies spiegelt die Absicht von `AccountProfit` in MetaTrader wider, die tatsächlichen Berechnungen auf Brokerseite können jedoch abweichen.
- Wenn der Connector weder die blockierte Marge noch den Portfoliowert offenlegt, bleibt der Drawdown-Schutz inaktiv, während die Strategie weiterhin mit `BaseVolume` handelt.
- `StartProtection()` ist beim Start aktiviert, sodass die Schutzmechanismen von StockSharp (Stop/Take-Routing, Wiederverbindungen) das in der MQL-Version vorhandene Sicherheitsnetz widerspiegeln.

## Unterschiede zum Original-Experten
- Die Lotrundung von MetaTrader wird mithilfe der Instrumentenmetadaten emuliert, die über StockSharp verfügbar sind. Überprüfen Sie die `VolumeStep`/`MinVolume`-Werte für das gehandelte Wertpapier, damit die Positionsgröße mit den Einschränkungen des Brokers übereinstimmt.
- Der MT4-Code wertete Tick für Tick aus, während er mit `Volume[0]` schützte. Der Port verarbeitet nur abgeschlossene Kerzen, was doppelte Signale verhindert und das empfohlene Muster für StockSharp-Strategien ist.
- Bei den Kontometriken handelt es sich um Näherungswerte. Wenn Sie sich auf strenge Margin-Limits verlassen, passen Sie `MaximumRisk` an oder überschreiben Sie den Schutz, um ihn an die genauen Formeln des Brokers anzupassen.
