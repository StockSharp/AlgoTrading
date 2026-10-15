# Nextbar-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Nextbar-Strategie** ist eine direkte Übersetzung des MetaTrader 4 Expertenberaters `nextbar.mq4`. Der ursprüngliche EA wertet den Abstand zwischen der letzten abgeschlossenen Kerze und einer Kerze aus, die mehrere Balken älter ist. Wenn sich der Preis weit genug in eine Richtung bewegt, folgt er je nach konfiguriertem Richtungsflag entweder dem Impuls oder handelt dagegen. Die Positionen werden dann durch symmetrische Take-Profit-/Stop-Loss-Levels geschützt und nach einer festgelegten Anzahl von Balken zwangsweise geschlossen.

Diese StockSharp-Version behält das gleiche Verhalten bei, wenn die High-Level-Strategie API verwendet wird. Es verarbeitet nur abgeschlossene Kerzen und stellt so sicher, dass alle Berechnungen der Bar-on-Close-Logik des MT4-Skripts entsprechen.

## Ursprüngliche MQL-Logik
* **Impulsentfernung** – vergleiche `Close[1]` mit `Close[bars2check+1]`. Wenn die Differenz mindestens `minbar * Point` beträgt, behandeln Sie es als gültiges Signal.
* **Richtungsflagge** – die MQL-Eingabe `direction` entspricht `1` für Trendfolge (Kauf nach einer Rallye, Verkauf nach einem Rückgang) und `2` für konträren Handel (Kauf nach einem Rückgang, Verkauf nach einer Rallye).
* **Eingabebeschränkung** – es kann jeweils nur eine Bestellung geöffnet sein. Nach dem Signal wird am Anfang des Balkens ein neuer Trade gesendet.
* **Ausstiegsregeln** – Schließen Sie eine Long-Position, wenn der letzte Schlusskurs die Gewinndistanz über dem Einstiegspunkt oder die Verlustdistanz darunter erreicht; Für Shorts gilt das Umgekehrte. Wenn keines der beiden Niveaus erreicht wird, schließen Sie den Handel nach `bars2hold` abgeschlossenen Kerzen.

## StockSharp Implementierungshighlights
* Verwendet `SubscribeCandles()` und `Bind`, um abgeschlossene Kerzen im konfigurierten Zeitrahmen zu erhalten.
* Speichert eine kurze rollierende Historie der Schlusskurse, um auf die Kerze zu verweisen, die dem Offset MQL `bars2check + 1` entspricht.
* Konvertiert alle punktbasierten Parameter mit `Security.PriceStep` und ahmt die Konstante MetaTrader `Point` nach.
* Platziert Marktaufträge mit der Strategie `Volume` und unterstützt über den Parameter `Direction` entweder Momentum-folgende oder konträre Einstiege.
* Implementiert Gewinn-, Verlust- und Haltezeitraum-Exits genau einmal pro fertiger Kerze, um mit dem ursprünglichen Arbeitsablauf in Einklang zu bleiben.

## Parameter
| Parameter | Beschreibung | Standard | Notizen |
|-----------|-------------|---------|-------|
| `CandleType` | Zeitrahmen, der für die Signalauswertung verwendet wird. | 1-stündiger Zeitrahmen | Hängen Sie die Strategie an ein Wertpapier an, das diesen Kerzentyp bereitstellen kann. |
| `BarsToCheck` | Anzahl der abgeschlossenen Kerzen zwischen dem Referenzschluss und dem letzten Schluss. | 8 | Entspricht `bars2check` aus EA. |
| `BarsToHold` | Maximale Anzahl abgeschlossener Kerzen, um eine Position offen zu halten. | 10 | Entspricht `bars2hold`. Die Position wird auf dem Balken geschlossen, bei dem der Zähler diese Zahl erreicht. |
| `MinMovePoints` | Mindestabstand (in MetaTrader Punkten) zwischen den beiden verglichenen Schlusskursen. | 77 | Entspricht `minbar`. Konvertiert mit `Security.PriceStep`. |
| `TakeProfitPoints` | Gewinnzielentfernung in MetaTrader Punkten. | 115 | Entspricht der Eingabe `profit`. Zum Deaktivieren auf Null setzen, falls gewünscht. |
| `StopLossPoints` | Stop-Loss-Distanz in MetaTrader Punkten. | 115 | Entspricht der Eingabe `loss`. Zum Deaktivieren auf Null setzen, falls gewünscht. |
| `Direction` | Handelsmodus: `Follow` (Trend) oder `Reverse` (Contrarian). | `Follow` | Spiegelt die `direction`-Eingabe (`1` = folgen, `2` = umkehren). |
| `Volume` | Handelsvolumen, das für Marktaufträge verwendet wird. | Strategievolumen | Konfigurieren Sie über die Standardeigenschaft `Strategy.Volume`. |

## Handelsablauf
1. Warten Sie auf eine fertige Kerze und speichern Sie ihren Schlusskurs.
2. Rufen Sie den Schlusskurs von `BarsToCheck` Kerzen ab und berechnen Sie die Differenz.
3. Wenn die absolute Bewegung unter `MinMovePoints * PriceStep` liegt, unternehmen Sie nichts.
4. Ansonsten:
   * Im **Folgen**-Modus kaufen Sie, wenn der Preis steigt, und verkaufen, wenn der Preis fällt.
   * Im **Reverse**-Modus kaufen Sie, wenn der Preis fällt, und verkaufen, wenn der Preis steigt.
5. Bei jeder weiteren fertigen Kerze, während die Position offen ist:
   * Schließen Sie Long-Positionen, wenn der Schlusskurs `TakeProfitPoints` über oder `StopLossPoints` unter dem gespeicherten Einstiegspreis liegt.
   * Schließen Sie Short-Positionen, wenn der Schlusskurs `TakeProfitPoints` unter oder `StopLossPoints` über dem Einstiegspunkt liegt.
   * Erzwingen Sie das Schließen, sobald seit dem Eintritt `BarsToHold` Kerzen verstrichen sind.

## Nutzungshinweise
* Die Umrechnung von Punkten in den absoluten Preis erfordert `Security.PriceStep`. Stellen Sie die richtigen Instrument-Metadaten (Preisschritt, Schrittpreis, Volumenregeln) bereit, bevor Sie die Strategie ausführen.
* Die Strategie verwaltet nicht mehrere gleichzeitige Positionen; Stellen Sie sicher, dass `Volume` der Größe entspricht, die Sie für eine einzelne MT4-Bestellung erwarten.
* Da Entscheidungen nur anhand abgeschlossener Kerzen bewertet werden, sollte die Strategie mit historischen und Echtzeitdaten ausgeführt werden, die fertige Balken liefern.
