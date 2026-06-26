# Daily Range-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie ist eine StockSharp-Konvertierung des MetaTrader-5-Expert-Advisors `MQL/23334/Daily range.mq5`. Der ursprüngliche EA verfolgt die höchsten und niedrigsten Preise der letzten Tage, verschiebt diese Niveaus um einen konfigurierbaren Prozentsatz der Tagesspanne und handelt Ausbrüche. Der C#-Port bewahrt das Verhalten und übernimmt dabei die hochrangige Strategie-API von StockSharp.

## Strategielogik
### Bereichsberechnung
* Die Strategie speichert aggregierte Statistiken für jeden Handelstag (Hoch, Tief, letzter Schluss).
* Ein gleitendes Fenster der `SlidingWindowDays` letzten Tage (einschließlich des aktuellen) wird geführt.
* `RangeMode` wählt, wie der Referenzbereich berechnet wird:
  * **HighestLowest** – die Distanz zwischen dem höchsten Hoch und dem niedrigsten Tief im Fenster.
  * **CloseToClose** – die durchschnittliche absolute Änderung zwischen aufeinanderfolgenden täglichen Schlusspreisen im Fenster.
* Sobald die konfigurierte `StartTime` an einem neuen Tag erreicht wird, baut die Strategie die oberen und unteren Ausbruchsniveaus neu auf:
  * `Upper = Highest + Range × OffsetCoefficient`
  * `Lower = Lowest − Range × OffsetCoefficient`
* Bis `StartTime` erreicht ist, bleiben die Ausbruchsniveaus des Vortages aktiv (entsprechend der MQL-Implementierung).

### Einstiegsregeln
* Ein Long-Einstieg wird ausgelöst, wenn der Schlusskurs der verarbeiteten Kerze größer oder gleich dem aktuellen oberen Niveau ist und weniger als `MaxPositionsPerDay` Long-Einstiege am selben Tag eröffnet wurden.
* Ein Short-Einstieg wird ausgelöst, wenn der Schlusskurs auf oder unter das untere Niveau fällt und das tägliche Short-Einstiegslimit nicht erreicht wurde.
* Beim Wechsel von einer bestehenden Position zur Gegenseite gleicht die Strategie zunächst das ausstehende Volumen aus und fügt dann das neue `Volume` hinzu, entsprechend dem Netting-Verhalten des ursprünglichen EA.
* Signale werden nur auf abgeschlossenen Kerzen aus dem konfigurierten `CandleType`-Abonnement ausgewertet und nur wenn `IsFormedAndOnlineAndAllowTrading()` den Handel erlaubt.

### Ausstiegsregeln
* Stop-Loss- und Take-Profit-Abstände werden aus dem aktuellen Bereich abgeleitet: `Range × StopLossCoefficient` bzw. `Range × TakeProfitCoefficient`.
* Bei Long-Positionen wird eine Schließorder gesendet, wenn das Kerzentief das Stop-Niveau berührt oder das Hoch das Take-Profit-Niveau überschreitet.
* Bei Short-Positionen wird eine Schließorder gesendet, wenn das Kerzenhoch das Stop-Niveau trifft oder das Tief das Take-Profit-Niveau kreuzt.
* Wenn einer der Koeffizienten auf null gesetzt wird, ist der entsprechende Schutz deaktiviert.

### Risikokontrollen und Limits
* Separate Tageszähler werden für Long- und Short-Einstiege geführt. Sie werden bei Beginn eines neuen Handelstages zurückgesetzt.
* Die `Volume`-Eigenschaft der Basis-`Strategy` steuert die Größe zusätzlicher Einstiege.
* Es werden keine ausstehenden Orders registriert; Ausstiege werden mit Marktorders bei der nächsten Strategie-Iteration nach Erkennung der Bedingung ausgeführt.

## Parameter
| Name | Beschreibung | Standardwert |
| --- | --- | --- |
| `RangeMode` | Bestimmt wie der Tagesbereich berechnet wird (`HighestLowest` oder `CloseToClose`). | `HighestLowest` |
| `SlidingWindowDays` | Anzahl der Kalendertage im gleitenden Fenster für die Bereichsberechnung. | `3` |
| `StopLossCoefficient` | Multiplikator für den aktuellen Bereich zur Stop-Loss-Distanz. | `0.03` |
| `TakeProfitCoefficient` | Multiplikator für den aktuellen Bereich zur Take-Profit-Distanz. | `0.05` |
| `OffsetCoefficient` | Zusätzlicher Offset für die Ausbruchsniveaus über dem Hoch und unter dem Tief. | `0.01` |
| `MaxPositionsPerDay` | Maximale Anzahl erlaubter Einstiege pro Richtung an einem Handelstag. | `3` |
| `StartTime` | Tageszeit, zu der ein neuer Bereich für die aktuelle Sitzung berechnet wird. | `10:05` |
| `CandleType` | Kerzenabonnement für Bereichsberechnung und Signalauswertung. | `15-Minuten-Zeitrahmen` |

## Implementierungshinweise
* Die Strategie verlässt sich ausschließlich auf StockSharp's hochrangige `Strategy`-Infrastruktur (`SubscribeCandles`, `WhenNew` und Marktorders) und manipuliert keine rohen Orderbücher.
* Bereichsstatistiken werden ohne Indikatorwert-Lookups gespeichert; alle Berechnungen erfolgen innerhalb der Strategie, entsprechend den Repository-Richtlinien.
* Schutzorders werden durch Überwachung von Kerzenextremen simuliert statt separate Stop-/Limit-Orders zu registrieren, was die Implementierung über verschiedene Adapter portierbar hält.
* Python-Unterstützung ist wie gewünscht absichtlich weggelassen. Nur die C#-Version ist in diesem Ordner vorhanden.
* Für den Live-Handel stellen Sie sicher, dass ausreichend historische Kerzen verfügbar sind, damit die erste Bereichsberechnung genug Daten hat.
