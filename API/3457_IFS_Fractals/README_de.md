# Strategie IFS Fractals
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
IFS Fractals ist ein Port des MetaTrader 5-Skripts `IFS_Fractals`. Der ursprüngliche Experte rendert eine IFS-Bitmap (Iterated Function System) des „Fraktalworts“, indem er wiederholt 28 affine Transformationen auf eine Punktwolke anwendet. Die StockSharp-Version verwandelt denselben chaotischen Prozess in einen Richtungsoszillator: Die X-Koordinate der generierten Punkte wird skaliert, mit einem exponentiellen gleitenden Durchschnitt (EMA) geglättet und als Impulsmaß interpretiert, das Long- und Short-Einstiege steuert.

## Strategielogik
### Iteriertes Funktionssystem
* **Affine Transformationen** – jede fertige Kerze löst eine Reihe von Iterationen aus (konfigurierbar). Während jeder Iteration wird eine der 28 Transformationen entsprechend den ursprünglichen Wahrscheinlichkeitsgewichten (alle gleich 35) ausgewählt. Die Transformation aktualisiert den aktuellen Punkt `(x, y)` unter Verwendung der Koeffizienten, die wörtlich aus dem MQL5-Code übernommen wurden.
* **Wahrscheinlichkeitstabelle** – die Strategie berechnet einmal beim Start ein kumulatives Wahrscheinlichkeitsarray vor und ermöglicht so eine schnelle Auswahl der nächsten Transformation mithilfe einer einzigen Zufallsziehung innerhalb der gesamten Wahrscheinlichkeitsmasse.

### Signalbau
* **Normalisierung** – die X-Koordinate wird durch denselben Skalierungsfaktor (standardmäßig `50`) geteilt, den das Skript beim Projizieren des Fraktals auf die Bitmap verwendet hat. Dadurch bleibt das Signal unabhängig vom Instrumentenpreis in einem stabilen numerischen Bereich.
* **EMA-Glättung** – die normalisierte Reihe speist einen EMA, dessen Periode konfigurierbar ist. Der EMA fungiert als Tiefpassfilter, der die dominante Drift der chaotischen Iterationen extrahiert.
* **Einstiegslogik** – wenn der EMA über die positive Einstiegsschwelle steigt, öffnet sich die Strategie oder kehrt sich in eine Long-Position um. Wenn der EMA unter den negativen Schwellenwert fällt, öffnet er sich symmetrisch oder kehrt sich in einen Short um.
* **Ausstiegslogik** – offene Long-Positionen steigen aus, sobald der EMA wieder auf oder unter die Ausstiegsschwelle fällt, während Short-Positionen aussteigen, wenn der EMA wieder über die negative Ausstiegsschwelle steigt. Dadurch entsteht ein Hystereseband, das ein schnelles Umkippen um den Nullpunkt herum verhindert.

### Risikomanagement
* **Positionsschutz** – optionale absolute Stop-Loss- und Take-Profit-Distanzen können über `StartProtection` aktiviert werden. Ein Wert von `0` deaktiviert die entsprechende Ebene und entspricht dem Verhalten des Quellskripts, das ohne Schutzbefehle ausgeführt wurde.
* **Volumenkontrolle** – Einträge verwenden einen festen Marktvolumenparameter. Jedes bestehende gegenläufige Engagement wird geschlossen, bevor ein neuer Handel eröffnet wird, um eine Position in einer einzigen Richtung aufrechtzuerhalten.

## Parameter
* **Volumen** – Marktvolumen für Neuzugänge.
* **Kerzentyp** – Zeitrahmen, der die fraktalen Iterationen steuert (Standard: 5-Minuten-Kerzen).
* **Iterationen** – Anzahl der IFS-Iterationen, die nach jeder fertigen Kerze verarbeitet werden.
* **Skalierung** – Divisor, der auf die X-Koordinate angewendet wird, bevor sie in EMA eingespeist wird.
* **Einstiegsschwelle** – absoluter EMA-Wert, der zum Öffnen einer Position erforderlich ist (positiv für Long-Positionen, negativ gespiegelt für Short-Positionen).
* **Ausgangsschwellenwert** – EMA-Wert, der Ausgänge auslöst, wenn das Signal in Richtung Null zurückkehrt.
* **EMA Periode** – Glättungsperiode des exponentiellen gleitenden Durchschnitts, der auf das normalisierte fraktale Signal angewendet wird.
* **Take Profit** – absolute Take-Profit-Distanz; zum Deaktivieren auf `0` setzen.
* **Stop-Loss** – absolute Stop-Loss-Distanz; zum Deaktivieren auf `0` setzen.

## Zusätzliche Hinweise
* Jeder Lauf erzeugt eine andere Handelssequenz, es sei denn, durch Modifizieren der Quelle wird ein deterministischer Zufallsstartwert eingefügt. Dies spiegelt die Zufälligkeit des ursprünglichen Bitmap-Rendering-Skripts wider.
* Die Strategie erfordert keine vom Markt abgeleiteten Indikatoren. Alle Daten werden intern aus den IFS-Koeffizienten generiert, sodass die abonnierten Kerzen lediglich das Timing für die Iterationen liefern.
* In diesem Paket ist keine Python-Implementierung enthalten. Unter `CS/` ist nur die C#-Strategie verfügbar.
