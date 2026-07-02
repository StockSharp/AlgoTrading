# Tägliche Trendumkehr
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Daily Trend Reversal ist eine Portierung des MetaTrader 4 Expert Advisors `dailyTrendReversal_D1`. Die Strategie verankert Intraday-Trades an den Eröffnungs-, Höchst- und Tiefstständen des aktuellen Tages und beteiligt sich nur, wenn sowohl die Preisbewegung als auch der Commodity Channel Index (CCI) die gleiche Richtungsneigung bestätigen. Der Handel ist auf eine konfigurierbare GMT-Sitzung beschränkt, wird optional nach Erreichen eines täglichen Gewinnziels angehalten und kann Positionen sofort verlassen, wenn die Filter auf die entgegengesetzte Seite wechseln.

## Strategielogik
### Tägliche Bias-Filter
* **Richtungsschritte** – Die Strategie bewertet bis zu drei Bedingungen, um die tägliche Tendenz zu validieren:
  1. Der Abstand vom aktuellen Preis zum Tagesextrem muss einen in Pips ausgedrückten Risikoschwellenwert überschreiten.
  2. Der Abstand vom Eröffnungskurs zum entgegengesetzten Extrem muss ebenfalls die Risikoschwelle überschreiten und der Preis muss innerhalb von 10 Pips vom täglichen Eröffnungskurs bleiben.
  3. (Optional) Die aktuelle Kerze muss in der Bewegungsrichtung schließen, während der Preis noch innerhalb von 10 Pips der täglichen Eröffnung liegt.
* **Range-Dominanz** – vergleicht den Abstand von der Eröffnung zum Hoch mit der Distanz von der Eröffnung zum Tief. Die längere Seite definiert den aktiven Trend.
* **CCI-Trend** – die letzten drei abgeschlossenen CCI-Werte müssen monoton steigend (für Long-Positionen) oder fallend (für Short-Positionen) sein.

### Einreisebestimmungen
* **Lange Einträge**
  * Nur während des konfigurierten GMT-Handelsfensters an Werktagen zulässig.
  * Der aktuelle Preis muss über der täglichen Eröffnung liegen, die Richtungsschritte müssen einen Aufwärtstrend bestätigen, die Range-Dominanz muss den Aufwärtstrend begünstigen und der CCI-Trend muss steigend sein.
  * Eröffnet eine Long-Position nur, wenn die Nettoposition flach oder short ist (Short-Engagement wird im Rahmen der Umkehrung zu Long geschlossen).
* **Kurze Einträge**
  * Gespiegelte Bedingungen: Preis unter der täglichen Eröffnung, Richtungsschritte bestätigen einen Abwärtstrend, Range-Dominanz begünstigt die Abwärtsbewegung und der CCI-Trend ist rückläufig.
  * Wird nur geöffnet, wenn die Nettoposition flach oder lang ist.

### Ausgangsregeln
* **Fester Take-Profit/Stop-Loss** – ausgedrückt in Pips relativ zum Einstieg. Ein Wert von `0` deaktiviert die entsprechende Ebene.
* **Sitzungs- und Haltekontrolle** – Sobald die GMT-Schlussstunde erreicht ist oder die Haltezeit in Stunden abgelaufen ist, werden profitable Positionen sofort geschlossen. Verlierergeschäfte wechseln in den Break-Even-Modus und werden geschlossen, sobald der Preis zum Einstiegsniveau zurückkehrt.
* **Umkehrausgang (optional)** – wenn aktiviert, werden Long-Positionen geschlossen, wenn die Abwärtsfilter übereinstimmen (Preis unter dem Eröffnungskurs und Tages-/CCI-Trends, die nach unten zeigen); Kurzschlüsse werden symmetrisch geschlossen, wenn die nach oben gerichteten Filter ausgerichtet sind.
* **Täglicher Gewinnstopp** – kombiniert den realisierten Gewinn seit dem ersten Handel des Tages mit dem variablen PnL. Wenn der konfigurierte Schwellenwert erreicht ist, werden alle Positionen geschlossen und neue Eingaben ausgesetzt, bis der Parameter manuell wieder aktiviert wird.

## Parameter
* **Auto Trading** – schaltet um, ob die Strategie neue Trades eröffnen darf.
* **Reversal Exit** – ermöglicht sofortige Ausstiege, wenn der entgegengesetzte Tagestrend bestätigt wird.
* **Trendschritte** – wählt aus, wie viele Schrittfilter (1–3) durchlaufen müssen, um die tägliche Tendenz zu validieren.
* **Volumen** – Auftragsvolumen für Markteintritte.
* **Take Profit (Pips)** – feste Gewinnzielentfernung; zum Deaktivieren auf `0` setzen.
* **Stop-Loss (Pips)** – Schutzstopp-Distanz; zum Deaktivieren auf `0` setzen.
* **Profit Stop** – Gewinnziel in Preiseinheiten, das den Handel für den Rest des Tages unterbricht; `0` deaktiviert die Funktion.
* **GMT Diff** – Kartenzeit minus GMT (in Stunden). Wird verwendet, um GMT-Sitzungsgrenzen in Diagrammzeit umzuwandeln.
* **Startstunde / Endstunde** – GMT-Stunden, die das Handelsfenster für neue Positionen begrenzen.
* **Abschlussstunde** – GMT-Stunde, nach der die Strategie Ausstiege erzwingt oder die Break-Even-Logik aktiviert.
* **Haltezeiten** – maximale Zeitspanne, die ein Trade offen bleiben darf, bevor die Sitzungslogik ausgelöst wird.
* **Risiko (Pips)** – Pip-Abstand, der von den Richtungsschritten verwendet wird.
* **CCI Zeitraum** – Anzahl der Perioden für den Commodity Channel Index.
* **Kerzentyp** – Zeitrahmen, der die Berechnungen steuert (Standard: 15-Minuten-Kerzen).

## Zusätzliche Hinweise
* Die Strategie erkennt die Pip-Größe anhand der Preisstufe des Wertpapiers. Fünf- und dreistellige FX-Symbole wandeln die konfigurierten Pip-Abstände automatisch in Preisinkremente um.
* Die tägliche Gewinnverfolgung wird mit der ersten Kerze jedes neuen Handelstages zurückgesetzt, indem der aktuell realisierte PnL als neue Basislinie erfasst wird.
* Für diese Strategie gibt es keine Python-Implementierung; Im Paket API ist nur die C#-Version enthalten.
