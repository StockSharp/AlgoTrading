# Blau Ergodic MDI Time-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht

Die **Blau Ergodic MDI Time-Strategie** ist eine direkte Konvertierung des MetaTrader-Experten `Exp_BlauErgodicMDI_Tm.mq5` nach StockSharp. Sie operiert auf Kerzen mit höherem Zeitrahmen und reproduziert die drei Signalmodi des ursprünglichen Algorithmus: **Breakdown**, **Twist** und **CloudTwist**. Die Strategie basiert auf einem mehrstufigen exponentiellen gleitenden Durchschnitt (EMA)-Glättungsprozess, der auf einen ausgewählten Kerzenkurs angewendet wird. Alle Berechnungen werden innerhalb der Strategie ohne zusätzliche Indikatoren durchgeführt, sodass die Logik dem MetaTrader-Experten entspricht und gleichzeitig mit der High-Level-API von StockSharp kompatibel bleibt.

Die Glättungs-Pipeline folgt der Logik des Blau Ergodic MDI-Oszillators:

1. Den gewählten Kurs mit einer EMA (Länge `BaseLength`) glätten.
2. Den geglätteten Wert vom Rohkurs subtrahieren, um eine Differenzreihe zu erhalten.
3. Drei aufeinanderfolgende EMAs auf die Differenz anwenden (Längen `FirstSmoothingLength`, `SecondSmoothingLength`, `ThirdSmoothingLength`).
4. Die Zwischen- (`histogram`) und Endausgaben (`signal`) nach dem Kursschritt des Instruments skalieren. Diese Werte steuern die Handelssignale.

## Signalmodi

### Breakdown

* Verwendet das Histogramm zwei Balken zurück (gesteuert durch `SignalBar`).
* Wenn der vorherige Histogrammwert positiv ist und der ausgewählte Balken in nicht-positives Gebiet wechselt, bereitet die Strategie einen Long-Einstieg vor und schließt optional Short-Positionen.
* Wenn der vorherige Histogrammwert negativ ist und der ausgewählte Balken in nicht-negatives Gebiet steigt, bereitet die Strategie einen Short-Einstieg vor und schließt optional Long-Positionen.

### Twist

* Vergleicht die Histogrammsteigung über zwei historische Balken.
* Wenn das Histogramm nach oben beschleunigt (Balken `SignalBar + 1` < Balken `SignalBar + 2`) und der neueste ausgewählte Balken über dem vorherigen liegt, wird ein Long-Einstiegssignal generiert. Short-Positionen können im gleichen Block geschlossen werden.
* Wenn das Histogramm nach unten beschleunigt (Balken `SignalBar + 1` > Balken `SignalBar + 2`) und der neueste ausgewählte Balken unter dem vorherigen liegt, bereitet die Strategie einen Short-Einstieg vor und kann Long-Positionen schließen.

### CloudTwist

* Verwendet sowohl das Histogramm als auch die zusätzliche geglättete Linie.
* Wenn das vorherige Histogramm über der Signallinie bleibt, aber der ausgewählte Balken darunter fällt, wird ein Long-Einstieg vorbereitet und Short-Positionen können geschlossen werden.
* Wenn das vorherige Histogramm unter der Signallinie liegt, aber der ausgewählte Balken darüber kreuzt, bereitet die Strategie einen Short-Einstieg vor und kann Long-Positionen verlassen.

## Zeitfenster-Filter

Der ursprüngliche Experte beschränkt den Handel auf eine konfigurierbare Sitzung. Die StockSharp-Version repliziert dieselben Regeln über die Parameter `UseTimeFilter`, `StartHour`, `StartMinute`, `EndHour` und `EndMinute`. Die Sitzungslogik unterstützt Fenster, die Mitternacht überschreiten, identisch zur MetaTrader-Implementierung:

* Wenn die Startzeit früher als die Endzeit ist, bleibt die Sitzung innerhalb eines Tages.
* Wenn Start- und Endzeit gleich sind, definieren die Minuten ein kürzeres Intervall in dieser Stunde.
* Wenn die Startzeit später als die Endzeit ist, erstreckt sich die Sitzung über Mitternacht.

Wenn der Handel durch den Sitzungsfilter deaktiviert ist, schließt die Strategie alle offenen Positionen und blockiert neue Einstiege, bis die Sitzung wieder öffnet.

## Risikomanagement

Die Parameter `StopLossPoints` und `TakeProfitPoints` spiegeln die Stop-Loss- und Take-Profit-Abstände des Experten wider. Abstände werden in Kursschritten ausgedrückt. Die Strategie berechnet die Schutzpreise bei jeder Eröffnung einer neuen Position neu. Jede abgeschlossene Kerze prüft, ob der Balkenbereich ein Schutzniveau berührt hat, und schließt die Position sofort, wenn ausgelöst.

## Kurseingaben

Der `PriceMode`-Parameter bietet dieselbe Liste von Kursquellen wie der ursprüngliche Indikator:

| Modus | Beschreibung |
| ----- | ------------ |
| Close | Schlusskurs. |
| Open | Eröffnungskurs. |
| High | Höchstkurs. |
| Low | Tiefstkurs. |
| Median | (High + Low) / 2. |
| Typical | (High + Low + Close) / 3. |
| Weighted | (High + Low + 2 × Close) / 4. |
| Simple | (Open + Close) / 2. |
| Quarter | (Open + High + Low + Close) / 4. |
| TrendFollow0 | High bei bullischen Kerzen, Low bei bärischen, Close bei neutralen. |
| TrendFollow1 | Durchschnitt von Close mit dem Kerzenextrem in Trendrichtung. |
| Demark | Demark-Kurs (gewichtet nach Kerzenrichtung). |

## Parameter

| Parameter | Standard | Beschreibung |
| --------- | -------- | ------------ |
| `Mode` | Twist | Wählt die Signalauswertung Breakdown, Twist oder CloudTwist. |
| `PriceMode` | Close | Kursquelle für den Oszillator. |
| `BaseLength` | 20 | EMA-Länge auf den Rohkurs angewendet. |
| `FirstSmoothingLength` | 5 | EMA-Länge der ersten Differenzglättung. |
| `SecondSmoothingLength` | 3 | EMA-Länge der zweiten Differenzglättung. |
| `ThirdSmoothingLength` | 8 | EMA-Länge der dritten Differenzglättung. |
| `SignalBar` | 1 | Anzahl abgeschlossener Balken zurück für Signalprüfungen (1 entspricht MetaTrader-Standard). |
| `AllowLongEntry` / `AllowShortEntry` | true | Long-/Short-Einstiege aktivieren oder deaktivieren. |
| `AllowLongExit` / `AllowShortExit` | true | Ausstiege für die entsprechende Seite aktivieren oder deaktivieren. |
| `UseTimeFilter` | true | Aktiviert den Handelssitzungsfilter. |
| `StartHour`, `StartMinute`, `EndHour`, `EndMinute` | 0/0/23/59 | Sitzungsgrenzen. |
| `StopLossPoints` | 1000 | Stop-Loss-Abstand in Kursschritten (0 deaktiviert). |
| `TakeProfitPoints` | 2000 | Take-Profit-Abstand in Kursschritten (0 deaktiviert). |
| `CandleType` | 4h-Zeitrahmen | Für Berechnungen verwendetes Kerzen-Abonnement. |
| `Volume` | 0.1 | Ordervolumen, entsprechend dem `MM`-Input des Experten. |

## Zusammenfassung der Handelsregeln

1. Kerzen des konfigurierten Zeitrahmens abonnieren.
2. Bei jeder abgeschlossenen Kerze die vierstufige EMA-Pipeline aktualisieren und die Histogramm- und Signalwerte in rollenden Puffern speichern.
3. Warten, bis die minimale Historientiefe erreicht ist (entsprechend der ursprünglichen `min_rates_total`-Berechnung).
4. Den ausgewählten Modus mit Balken `SignalBar` und älteren Werten auswerten, um Öffnungs-/Schließ-Flags zu setzen.
5. Positionen zuerst schließen, wenn das entsprechende Ausstiegs-Flag gesetzt ist oder wenn der Zeitfilter den Handel blockiert.
6. Neue Long- oder Short-Trades nur öffnen, wenn das jeweilige Flag gesetzt ist, der Zeitfilter den Handel erlaubt und die aktuelle Position nicht bereits in dieselbe Richtung zeigt. Bei Umkehrung dimensioniert die Strategie die Order automatisch, um die bestehende Exposition plus das konfigurierte Volumen abzudecken.
7. Schutzstops und -ziele mithilfe von Kerzenextremen zur Erkennung von Auslösungen aufrechterhalten.

## Verwendungshinweise

* Die Strategie verwendet Tabulatoren für die Einrückung, konsistent mit den Projektrichtlinien.
* Sie ruft `StartProtection()` einmal beim Start auf, um die Sicherheitsfunktionen von StockSharp mit Positionsänderungen abzustimmen.
* Indikatorwerte werden nur für die minimale Anzahl von Balken gespeichert, die von den Signalen benötigt werden. Es werden keine großen Sammlungen erstellt, gemäß den Repository-Anweisungen.
* Um mit anderen Glättungsmethoden der MetaTrader-Version zu experimentieren, die EMA-Längen entsprechend anpassen. Die EMA-basierte Pipeline bietet die engste Annäherung, die von der High-Level-API von StockSharp unterstützt wird.

## Ausführung der Strategie

1. Die Strategieklasse zur StockSharp-Lösung hinzufügen und das Projekt kompilieren.
2. Parameter konfigurieren (Instrument, Kerzen-Zeitrahmen, Modus, Sitzung und Risikoeinstellungen).
3. Die Strategie an einen Connector anhängen, der die erforderlichen Marktdaten bereitstellt.
4. Die Strategie starten; sie abonniert automatisch die konfigurierten Kerzen und verwaltet Orders gemäß den obigen Regeln.
