# Strategie CCI MACD Scalper
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Der CCI MACD Scalper portiert den MetaTrader 5 Expertenberater „CCI + MACD Scalper“ auf die StockSharp übergeordnete Strategie API. Bei der Konvertierung bleibt der ursprüngliche Indikatorstapel erhalten – ein EMA-Trendfilter, ein CCI-Nulllinientrigger und eine MACD-Divergenzprüfung –, während die Geldverwaltungslogik in StockSharp-Konventionen übersetzt wird. Die Größe der Aufträge richtet sich nach dem Eigenkapital des Portfolios, Stopps werden abgelehnt, wenn der Abstand zu gering ist, und ein optionaler Trailing Stop kann Gewinne sichern, indem Positionen nach der ersten Anpassung teilweise geschlossen werden. Eine Abklingzeit von fünf Kerzen verhindert, dass die Strategie unmittelbar nach einer Ausführung erneut aktiviert wird, und repliziert so das Verhalten des MQL-Timers.

## Strategielogik
### Indikatoren und Datenverarbeitung
* **Kerzen** – ein konfigurierbarer Zeitrahmen bestimmt jede Berechnung. Signale werden ausschließlich an abgeschlossenen Kerzen ausgewertet, um ein Nachmalen zu vermeiden.
* **EMA(34)** – Der exponentielle gleitende Durchschnitt des Schlusskurses fungiert als Richtungsfilter. Bei Long-Positionen muss der letzte Schlusskurs über dem vorherigen EMA-Wert liegen, bei Short-Positionen muss der Schlusskurs darunter liegen.
* **CCI(50)** – wird als Momentum-Trigger verwendet. Die Strategie wartet auf einen Nullliniendurchschnitt, der bei den beiden zuletzt abgeschlossenen Kerzen aufgetreten ist (die aktuelle Kerze bestätigt das Setup, nimmt aber nicht am logischen Vergleich teil).
* **MACD(12,26,9)** – die Haupt- und Signalleitung MACD müssen für die beiden vorherigen Kerzen beide auf der gleichen Seite von Null bleiben. Für den Einstieg muss die Signallinie MACD die Hauptlinie zugunsten der Position zwischen diesen beiden Balken kreuzen (bullischer Crossover für Long-Positionen, bärischer Crossover für Short-Positionen).
* **Swing-Puffer** – die letzten fünf abgeschlossenen Kerzenhochs und -tiefs bilden die Stop-Loss-Referenz. Long-Positionen verankern sich am tiefsten Tief, Short-Positionen am höchsten Hoch und entsprechen genau den Aufrufen von MetaTrader `iLowest/iHighest` mit einer Verschiebung um einen Balken.

### Einreisebestimmungen
* **Sitzungskontrolle** – Der Handel ist nur zulässig, wenn die Schlusszeit der Kerze innerhalb `[MinHour, MaxHour]` der lokalen Terminalzeit liegt.
* **Abklingzeit** – nach jedem ausgefüllten Eintrag wartet das System fünf Kerzendauern, bevor es einen neuen Handel zulässt, wobei `EventSetTimer` vom ursprünglichen Code übernommen wird.
* **Lange Einrichtung**
  * Keine aktive Long-Position (`Position <= 0`).
  * Schlusskurs über dem vorherigen EMA-Wert.
  * CCI ist bei den beiden letzten geschlossenen Kerzen von negativ auf positiv übergegangen.
  * Während derselben zwei Balken trat ein Crossover von MACD unter Null auf (Signal stieg über MACD).
  * Der Stop-Loss, der beim letzten Swing-Tief positioniert ist, erfüllt die Mindestabstandsbeschränkung.
* **Kurze Einrichtung**
  * Keine aktive Short-Position (`Position >= 0`).
  * Schlusskurs unter dem vorherigen EMA-Wert.
  * CCI ist in den letzten beiden abgeschlossenen Kerzen vom positiven zum negativen Wert übergegangen.
  * MACD Crossover trat über Null auf (Signal fiel unter MACD).
  * Der Stop-Loss am Swing-High respektiert die Mindestabstandsanforderung.

### Risiko- und Handelsmanagement
* **Dynamische Positionsgröße** – Die Handelsgröße wird aus dem konfigurierten `RiskPercent` des Portfolio-Eigenkapitals abgeleitet. Das Risiko pro Kontrakt errechnet sich aus der Stop-Loss-Distanz, dem Wertpapierpreisschritt und dem Schrittwert. Das Ergebnis wird an die Lautstärkestufe des Instruments angepasst und zwischen der minimalen und maximalen Lautstärke eingeklemmt.
* **Stop-Loss / Take-Profit** – Stop-Loss verwendet das gewählte Swing-Extrem und wird abgelehnt, wenn der Abstand unter `MinimalStopLossPoints` liegt. Der Take-Profit beträgt `entry ± RiskReward × stopDistance` und entspricht der Belohnungs-Risiko-Berechnung von EA.
* **Trailing Stop (optional)** – wenn aktiviert, verschiebt sich der Stop um `TrailingStopPoints`, sobald der Preis weit genug über dem vorherigen Stop schließt. Die erste nachgestellte Anpassung löst einen teilweisen Exit aus, der die Hälfte des ursprünglichen Volumens schließt und die MetaTrader-Implementierung getreu widerspiegelt.
* **Schutzausstiege** – bei Long-Positionen wird die Position geschlossen, wenn der Preis das Stop-Level (Kerzentief) durchbricht oder das Take-Profit-Niveau (Kerzenhoch) erreicht. Shorts spiegeln die Logik wider, indem sie Kerzenhochs bzw. -tiefs verwenden.

## Parameter
| Name | Beschreibung | Standard |
|------|-------------|---------|
| `CandleType` | Zeitrahmen, der die Indikatorberechnungen steuert. | 15-Minuten-Kerzen |
| `RiskPercent` | Prozentsatz des Portfolio-Eigenkapitals, das bei jedem Trade riskiert wird. | 2 % |
| `RiskReward` | Belohnungs-Risiko-Multiplikator für das Take-Profit-Niveau. | 1.5 |
| `EmaPeriod` | Länge des Trendfilters EMA. | 34 |
| `CciPeriod` | Länge des Commodity Channel Index. | 50 |
| `MinHour` | Früheste Stunde (einschließlich), zu der neue Geschäfte eröffnet werden können. | 0 |
| `MaxHour` | Späteste Stunde (einschließlich), zu der neue Geschäfte eröffnet werden können. | 24 |
| `MinimalStopLossPoints` | Minimal zulässiger Abstand zwischen Einstieg und Stop-Loss, ausgedrückt in Preispunkten. | 100 |
| `UseTrailingStop` | Aktiviert das Trailing-Stop-Modul und teilweise Take-Profit. | Deaktiviert |
| `TrailingStopPoints` | Trailing-Stop-Distanz, gemessen in Preispunkten. | 100 |

## Zusätzliche Hinweise
* Die Preis-Punkt-Umrechnung basiert auf dem `PriceStep` des Wertpapiers. Symbole ohne gültigen Schritt fallen auf einen Abstand von einer Preiseinheit zurück.
* Das Portfolioeigenkapital wird von `Portfolio.CurrentValue` bezogen und fällt auf `BeginValue` zurück, wenn die aktuelle Bewertung nicht verfügbar ist. Wenn beide fehlen, greift die Strategie auf die Basiseigenschaft `Volume` zurück.
* Für diese Strategie gibt es keinen Python-Port; Im Paket API ist nur die C#-Version enthalten.
