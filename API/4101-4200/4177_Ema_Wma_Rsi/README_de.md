# Strategie EMA WMA RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
EMA WMA RSI ist eine Konvertierung des von cmillion erstellten MetaTrader 4-Expertenberaters „EMA WMA RSI“. Der ursprüngliche Roboter vergleicht einen exponentiellen gleitenden Durchschnitt (EMA) und einen linear gewichteten gleitenden Durchschnitt (WMA), der aus Kerzeneröffnungen berechnet wird, und filtert jeden Schnittpunkt mit einem Schwellenwert für den Relative Strength Index (RSI). Der StockSharp-Port behält die gleiche Indikatorlogik bei, arbeitet mit fertigen Kerzen und reproduziert die Geldverwaltungsoptionen: optionale Abflachung der Gegenposition, punktbasierte Stop-Loss-/Take-Profit-Levels und ein Trailing Stop, der festen Abständen, dem neuesten Fraktal oder aktuellen Kerzenextremen folgen kann.

Die Strategie ist für ein einzelnes Symbol und einen einzelnen Zeitrahmen konzipiert, der über den Parameter `Candle Type` ausgewählt wird. Bei der Umrechnung von Risikoabständen in absolute Preise werden MetaTrader „Punkte“ (der minimale Tick) angenommen. Daher sollten Instrumentenmetadaten wie `Security.Step` und `Security.StepPrice` ausgefüllt werden, um optimale Ergebnisse zu erzielen.

## Strategielogik
### Indikatoren
* **EMA** – durch `EMA Period` definierter Zeitraum, angewendet auf Kerzenöffnungspreise.
* **WMA** – Zeitraum definiert durch `WMA Period`, auch gespeist mit Kerzeneröffnungen.
* **RSI** – `RSI Period`, berechnet auf demselben Open-Price-Stream.

Alle Indikatoren werden einmal pro fertiger Kerze aktualisiert. Der Port spiegelt die ursprüngliche Ausführung „Bar öffnen“ wider, indem er die EMA/WMA-Werte des vorherigen Balkens speichert und sie direkt nach dem Schließen mit dem aktuellen Balken vergleicht.

### Einreisebestimmungen
* **Lange Einrichtung**
  1. Der aktuelle EMA-Wert liegt unter dem WMA, während der vorherige Balken EMA über dem WMA hatte (ein Abwärtskreuz).
  2. Der Wert von RSI liegt über 50.
  3. Wenn eine Short-Position vorhanden ist, wird diese optional geschlossen, wenn `Close Counter Trades` aktiviert ist; andernfalls wird das Signal ignoriert, bis die Strategie flach ist.
  4. Wenn die Bedingungen erfüllt sind, wird eine Marktkauforder entweder mit dem festen Volumen oder mit der risikobasierten Größenbestimmung gesendet.
* **Short-Setup** – symmetrische Logik: EMA kreuzt über WMA, der vorherige Balken zeigte EMA unter WMA, RSI liegt unter 50 und die Strategie flacht entweder einen Long ab oder überspringt den Trade.

### Ausgangsregeln
* **Anfänglicher Schutz** – `Stop Loss (points)` und `Take Profit (points)` werden anhand der Tick-Größe des Instruments in absolute Entfernungen übersetzt. Jeder Wert kann auf Null gesetzt werden, um ihn zu deaktivieren.
* **Trailing Stop**
  * Wenn `Trailing Stop (points)` größer als Null ist, folgt der Stop dem Preis in einem festen Abstand, gemessen vom letzten Schlusskurs (nur Anziehen, niemals Nachgeben).
  * Wenn die Nachlaufdistanz Null ist, sucht der Algorithmus nach adaptiven Ebenen:
    * `Trailing Source = CandleExtremes` blickt auf frühere Kerzenhochs/-tiefs zurück. Ein Long-Stop bewegt sich zum ersten Tief, das mindestens fünf Punkte unter dem aktuellen Preis liegt; Ein kurzer Stopp verwendet Höchstwerte von fünf Punkten darüber.
    * `Trailing Source = Fractals` scannt zuvor bestätigte Bill Williams-Fraktale (zwei Kerzen auf jeder Seite). Derselbe Fünf-Punkte-Puffer gilt, um zu vermeiden, dass der Stop zu nahe am aktuellen Preis platziert wird.
  * Nachlaufende Anpassungen werden erst aktiviert, wenn der Preis über den ursprünglichen Einstiegspreis hinausgeht und das Verhalten von MetaTrader EA reproduziert.
* **Positionsausstieg** – Wenn der Trailing Stop oder Take-Profit innerhalb der Spanne einer Kerze berührt wird, wird die Position mit einer Marktorder geschlossen und der interne Status zurückgesetzt.

### Positionsgrößenbestimmung
* `Fixed Volume` liefert die genaue Marktauftragsgröße (Lots/Kontrakte). Dies ist die Standardeinstellung und entspricht dem EA-Parameter `Lot`.
* Wenn Sie `Fixed Volume` auf Null setzen, wird die risikobasierte Größenanpassung aktiviert. Die Strategie schätzt das monetäre Risiko pro Einheit anhand der verfügbaren Stop-Distanz (entweder dem konfigurierten Stop-Loss oder der effektiven Trailing-Distanz) und `Security.StepPrice`. `Risk %` bestimmt, wie viel Portfolio-Eigenkapital pro Trade ausgesetzt ist. Wenn sowohl das feste Volumen als auch der Risikoprozentsatz Null sind, wird das Signal ignoriert.

## Parameter
| Parameter | Beschreibung | Standard |
|-----------|-------------|---------|
| `EMA Period` | Zeitraum des exponentiellen gleitenden Durchschnitts, der auf Kerzeneröffnungen angewendet wird. | `28` |
| `WMA Period` | Zeitraum des linear gewichteten gleitenden Durchschnitts bei Eröffnungen. | `8` |
| `RSI Period` | RSI Länge wird als Richtungsfilter verwendet. | `14` |
| `Stop Loss (points)` | Stop-Loss-Offset in MetaTrader Punkten. `0` deaktiviert den Schutzstopp. | `0` |
| `Take Profit (points)` | Take-Profit-Offset in Punkten. `0` deaktiviert das Ziel. | `500` |
| `Trailing Stop (points)` | Nachlaufdistanz in Punkten korrigiert. `0` wechselt zum adaptiven Trailing (Fraktale oder Kerzentiefs/-hochs). | `70` |
| `Trailing Source` | Adaptive Trailing-Methode: `CandleExtremes` für rohe Hochs/Tiefs, `Fractals` für Williams Fraktale. | `CandleExtremes` |
| `Close Counter Trades` | Schließen Sie eine entgegengesetzte Position, bevor Sie einen neuen Handel eröffnen. | `false` |
| `Fixed Volume` | Marktauftragsvolumen. Auf `0` setzen, um die risikobasierte Größenanpassung zu aktivieren. | `0.1` |
| `Risk %` | Prozentsatz des zugesagten Portfolio-Eigenkapitals, wenn `Fixed Volume` Null ist. Erfordert einen gültigen Stoppabstand. | `10` |
| `Candle Type` | Primärer Zeitrahmen für Indikatoren und Signalauswertung. | `30-minute candles` |

## Hinweise zur Implementierung
* Preisstufenkonvertierungen basieren auf `Security.Step` (oder `Security.PriceStep`) und `Security.StepPrice`. Stellen Sie realistische Instrumentenmetadaten bereit, um Punkt-zu-Preis-Berechnungen korrekt zu halten.
* Die Strategie verarbeitet nur fertige Kerzen und verwendet ihre offenen Preise für Indikatoraktualisierungen, entsprechend der „Neuer Balken“-Logik im MQL4-Code.
* Nachfolgende Niveaus halten mindestens einen Puffer von fünf Punkten vom aktuellen Preis entfernt, genau wie die ursprüngliche Hilfsfunktion `SlLastBar`.
* Wenn das Schließen von Gegenpositionen deaktiviert ist, sichert die Strategie nie ab – es wird immer nur eine einzige Nettoposition verwaltet.
* In diesem Paket ist keine Python-Implementierung enthalten.
