# Gralsexperte MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Grail Expert MA ist ein StockSharp-Port des MetaTrader 4 Expert Advisors `_GrailExpertMAV1_0`. Das System sucht nach neuen Ausbrüchen jenseits des jüngsten Hoch-/Tief-Kanals und wartet auf einen Rückzug, bevor es sich der Bewegung anschließt. Ein exponentieller gleitender Durchschnitt des typischen Preises sorgt für die Richtungsabweichung: Trades sind nur zulässig, wenn der EMA in den letzten beiden abgeschlossenen Kerzen eine konfigurierbare Anzahl von Pips gewonnen oder verloren hat. Das Risikomanagement spiegelt den ursprünglichen Experten mit Pip-basierten Stop-Loss- und Take-Profit-Abständen wider und ignoriert neue Einträge, während eine Position aktiv ist.

## Strategielogik
### EMA Steigungstrendfilter
* Am Ende jedes Balkens wird ein EMA berechnet, der auf dem typischen Preis ((Hoch + Tief + Schlusskurs)/3) berechnet wird.
* Die Differenz zwischen den letzten beiden EMA-Werten muss den Schwellenwert `EMA Slope (pips)` überschreiten (umgerechnet in einen Preis mithilfe der Pip-Größe des Symbols).
* Eine positive Steigung erlaubt lange Pullbacks, eine negative Steigung erlaubt kurze Pullbacks und flache Steigungen blockieren den Handel.

### Verfolgung der Ausbruchsreichweite
* Die Strategie behält das höchste Hoch und das niedrigste Tief über die letzten `Range Period` abgeschlossenen Balken bei.
* Diese Ebenen bilden einen Kanal, dessen Höhe dazu verwendet wird, flache Bewegungen abzuwehren, die nicht genügend Abstand für die Pullback-Logik schaffen.

### Einreisevorbereitung
* Wenn der aktuelle Balken ein neues Hoch über dem gespeicherten Bereich anzeigt, wird ein potenzieller Long-Einstiegspreis bei `High - Breakout Buffer - Take Profit` Pips berechnet.
* Wenn der aktuelle Balken ein neues Tief unterhalb des gespeicherten Bereichs anzeigt, wird ein potenzieller Short-Einstiegspreis bei `Low + Breakout Buffer + Take Profit` Pips berechnet.
* Für den ursprünglichen EA musste der Abstand zwischen dem neuen Extrem und der gegenüberliegenden Seite des Bereichs mindestens `2 * Breakout Buffer + Take Profit` betragen. Der Port behält die gleiche Validierung bei und verwirft den Eintrag, wenn die Spanne zu klein ist.

### Eintrittsauslöser
* Die vorbereiteten Preise bleiben für den Rest der Bar aktiv. Ein Long wird ausgeführt, wenn das Intrabar-Tief den gespeicherten Long-Einstiegspreis erreicht oder unterschreitet, während die EMA-Steigung positiv ist.
* Ein Short wird ausgeführt, wenn das Intrabar-Hoch den gespeicherten Short-Einstiegspreis erreicht oder überschreitet, während die EMA-Steigung negativ ist.
* Es kann jeweils nur ein Trade geöffnet sein; Der Hafen löscht beide ausstehenden Einstiegspreise, sobald eine Bestellung übermittelt wird, die dem Verhalten von MQL entspricht.

### Exit-Management
* Long-Positionen verwenden einen Stop bei `Entry - Stop Loss` Pips und ein Gewinnziel bei `Entry + Take Profit` Pips (Null deaktiviert das jeweilige Level).
* Short-Positionen spiegeln die Berechnungen wider (Stopp oben, Ziel unten).
* Exits werden ausgelöst, wenn die Kerzenextreme die Schutzniveaus berühren, was der balkenbasierten Annäherung der ursprünglichen Tick-Logik entspricht.

### Zusätzliche Schutzmaßnahmen
* Ausstehende Einträge werden gelöscht, wenn sie beim Schließen einer neuen Kerze außerhalb des aktualisierten Bereichs liegen.
* Alle Pip-Abstände passen sich automatisch an die Tick-Größe des Instruments an (fünfstellige FX-Symbole ordnen einen Pip 10 Ticks zu).
* Wenn der EMA noch nicht gebildet ist oder der Bereichspuffer nicht über genügend Verlauf verfügt, bleibt die Strategie inaktiv, bis genügend Daten verfügbar sind.

## Parameter
* **Auftragsvolumen** – Handelsvolumen in Lots/Kontrakten für Marktaufträge.
* **Take Profit (Pips)** – Abstand zum festgelegten Gewinnziel; zum Deaktivieren auf `0` setzen.
* **Stop Loss (Pips)** – Abstand zum Schutzstopp; zum Deaktivieren auf `0` setzen.
* **Bereichszeitraum** – Anzahl der abgeschlossenen Kerzen, die zur Messung des Ausbruchskanals verwendet werden.
* **EMA Zeitraum** – Länge des exponentiellen gleitenden Durchschnitts, angewendet auf den typischen Preis.
* **EMA Steigung (Pips)** – minimaler Pip-Vor-/Abstieg zwischen aufeinanderfolgenden EMA-Werten, der erforderlich ist, um Eingaben zu ermöglichen.
* **Breakout-Puffer (Pips)** – zusätzlicher Abstand vom neuen Extrem, bevor Pullback-Einstiege aktiviert werden.
* **Kerzentyp** – vom Datenfeed angeforderter Zeitrahmen (Standard: 1-Stunden-Kerzen).

## Hinweise zur Implementierung
* Die Strategie verwendet Rohkerzenaktualisierungen (einschließlich Teilzuständen), um die ursprüngliche Intrabar-Hoch/Tief-Überwachung zu emulieren.
* EMA-Werte werden nur bei fertigen Kerzen verarbeitet, um die MQL `iMA`-Aufrufe mit Verschiebungen von einem und zwei Balken zu reproduzieren.
* Historische Bereiche werden mit begrenzten Warteschlangen anstelle von Indikatorsuchen verfolgt, um teure Neuscans zu vermeiden und gleichzeitig die Logik der Quelle treu zu halten.
* Es wird keine Python-Version bereitgestellt. Das Paket API enthält nur die C#-Implementierung.
