# Ausbruch aus der Frühbucherreichweite
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Early Bird Range Breakout ist eine C#-Portierung des MetaTrader 4 Expert Advisors `earlyBird1`. Das System verfolgt die Höchst- und Tiefststände einer konfigurierbaren Pre-Market-Range, wendet einen 14-Perioden-RSI-Filter an, um die Handelsausrichtung zu bestimmen, und steigt beim ersten Ausbruch ein, sobald die reguläre Sitzung eröffnet wird. Die Beschränkung auf einen einzelnen Trade pro Richtung, die volatilitätskontrollierte Trailing-Logik und die tägliche Abschlussdisziplin des ursprünglichen Expert Advisors bleiben erhalten.

## Strategielogik
### Sortimentsaufbau
* **Zeitfenster** – der Bereich wird zwischen `Range Start Hour` und `Range End Hour` berechnet (nach Anwendung der DST-Offset-Logik). Jede Kerze, die dieses Fenster schneidet, erweitert die Hoch-/Tiefgrenze.
* **Einstiegspuffer** – ein konfigurierbarer Offset in Pips wird oberhalb des Bereichshochs hinzugefügt und unterhalb des Bereichstiefs subtrahiert, um den `±2/Fakt`-Breakout-Puffer des MetaTrader-Skripts nachzuahmen.
* **Tägliches Zurücksetzen** – die Spanne, die Einstiegsauslöser und die Handelszähler werden mit der ersten abgeschlossenen Kerze jedes neuen Handelstages zurückgesetzt.

### Richtungsfilter
* **RSI bei Eröffnungen** – die Strategie füttert den RSI mit Kerzeneröffnungspreisen, passend zur MT4-Implementierung, die `iRSI(..., PRICE_OPEN)` verwendet.
* **Bias-Auswahl** – wenn RSI über 50 liegt, ist nur der lange Abzug aktiviert; Wenn RSI 50 oder niedriger ist, ist nur der kurze Trigger aktiv. Dies gewährleistet eine einzelne Richtungseinstellung pro Kerze, genau wie beim Original EA.

### Einreisebestimmungen
* **Handelssitzung** – neue Positionen sind nur an Werktagen zwischen `Session Start` und `Session End` zulässig, nachdem sich die Breakout-Range gebildet hat.
* **Einzelversuch pro Seite** – sobald eine Long- (oder Short-)Position eröffnet wird, wird die entsprechende Seite für den Rest des Tages deaktiviert, was den täglichen Handelszählern im MT4-Code entspricht.
* **Absicherungsschalter** – Wenn `Allow Hedging` aktiviert ist, kann die Strategie von einer Short- zu einer Long-Position (oder umgekehrt) umkehren, indem genügend Volumen bereitgestellt wird, um das bestehende Risiko abzuflachen und sofort die Richtung umzukehren. Wenn die Absicherung deaktiviert ist, werden Eingaben übersprungen, es sei denn, die Position ist flach.

### Ausgangsregeln
* **Festes Risiko und Ziel** – Stop-Loss- und Take-Profit-Werte werden in Pips ausgedrückt. Das Gewinnziel wird automatisch durch die Stop-Distanz und die Range-Breite eingeschränkt, wodurch die `MathMin`-Logik des ursprünglichen Expert Advisors reproduziert wird.
* **Volatilitätsgesteuertes Trailing** – sobald die Spanne der aktuellen Kerze die 16-Perioden-Durchschnittsspanne multipliziert mit `Trailing Risk` überschreitet und der Handel mindestens `Trailing Trigger` im Gewinn ist, wird der Stop um die volle Stop-Distanz nachgezogen, während der Take-Profit näher herangezogen wird (die Hälfte des Trailing-Triggers), was dem Verhalten von `OrderModify` im MQL-Code entspricht.
* **Sitzungsabschluss** – zur konfigurierten Schlusszeit werden gewinnbringende Geschäfte sofort geschlossen. Verlierende Positionen verschieben ihren Take-Profit auf den Einstiegspreis, genau wie bei der MT4-Break-Even-Durchsetzung.

## Parameter
* **Auto Trading** – Master-Aktivierungsschalter für automatisierte Eingaben.
* **Absicherung zulassen** – ermöglicht die Umkehrung in die entgegengesetzte Richtung, auch wenn eine Position bereits offen ist.
* **Handelsrichtung** – beschränkt die Strategie auf nur Long (`1`), nur Short (`2`) oder beide Richtungen (`0`).
* **Volumen** – Auftragsvolumen für Markteintritte.
* **Take Profit (Pips)** – maximale Entfernung zum Gewinnziel; Die effektive Distanz wird durch den Stop-Loss und die Range-Breite begrenzt.
* **Stop-Loss (Pips)** – feste Schutzstoppdistanz in Pips.
* **Trailing Trigger (Pips)** – erforderlicher minimaler günstiger Ausschlag, bevor die Trailing-Logik den Stop und Take-Profit anpassen kann.
* **Trailing-Risiko** – Multiplikator, der auf den durchschnittlichen Kerzenbereich über 16 Perioden angewendet wird, wenn beurteilt wird, ob die Volatilität hoch genug ist, um nachlaufen zu können.
* **Einstiegspuffer (Pips)** – Pip-Versatz, der bei der Berechnung der Ausbruchsniveaus auf die Bereichsgrenzen angewendet wird.
* **Sitzungsstartstunde/-minute** – Beginn des aktiven Handelsfensters (Chartzeit vor DST-Anpassung).
* **Session End Hour** – Ende des Handelsfensters für neue Positionen.
* **Closing Hour** – Stunde, nach der Positionen gezwungen werden, die Gewinnschwelle zu erreichen oder geschlossen werden.
* **Bereichsanfangsstunde / Bereichsendstunde** – Stunden, die den Bereich vor der Sitzung definieren, der für Ausbrüche verwendet wird.
* **Start der Sommerzeit / Start der Winterzeit** – Tagesmarkierungen, die zum Umschalten zwischen ein- und zweistündigen Abweichungen verwendet werden und die `Sommerzeit/Winterzeit`-Logik imitieren.
* **RSI Länge** – Anzahl der Perioden für den RSI-Filter (Standard 14).
* **Kerzentyp** – primärer Zeitrahmen, der die Berechnungen steuert (standardmäßig 15-Minuten-Kerzen).

## Zusätzliche Hinweise
* Die Pip-Größe wird aus dem aktuellen Preisniveau (≥ 10 Einheiten → `0.01`, sonst `0.0001`) abgeleitet, genau wie die `Fakt`-Berechnung im MT4-Skript.
* In der Trailing-Statistik werden die letzten 16 abgeschlossenen Kerzen verwendet, mit Ausnahme des aktuellen Balkens, was der ursprünglichen Durchschnittslogik entspricht.
* Die StockSharp-Strategie verwendet Nettopositionen, sodass gleichzeitige Long- und Short-Positionen durch Überkauf oder Überverkauf des bestehenden Engagements emuliert werden, wenn die Absicherung aktiviert ist.
* Es wird nur die C#-Implementierung bereitgestellt. Für diese Strategie gibt es keine Python-Version.
