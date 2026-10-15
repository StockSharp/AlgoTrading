# Diagramm der Strategie Armed Session Box
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Eine ruhige Nacht endet meist irgendwo. Dieses Diagramm misst die Spanne, die der Markt zwischen zwei Nachtstunden eingehalten hat, wartet auf die Eröffnung der Handelssitzung und schärft sich genau einmal — dabei friert es das Hoch und das Tief dieser Box als die beiden Preise ein, die es handeln wird. Von da an beobachtet es die besten Quotes des Orderbuchs und kauft oder verkauft in dem Moment, in dem eines der eingefrorenen Niveaus erreicht wird.

![schema](schema.svg)

## Strategieübersicht

- Fünf-Minuten-Kerzen werden mit Zwischenaktualisierungen abonniert, und ein Final value-Block lässt nur abgeschlossene Kerzen durch; jede Entscheidung im Diagramm fällt auf einem fertigen Bar.
- Ein Working time-Block markiert das Messfenster, und eine Variable vom Typ Kerze wird durch dieses Signal ausgelöst, sodass nur Kerzen aus dem Nachtfenster die Indikatoren erreichen — die Box entsteht aus diesem Fenster und aus nichts sonst.
- Highest und Lowest über dem gefilterten Strom sind das Hoch und das Tief der Box, und zwei Variablen halten die letzten Werte fest, damit der Rest des Diagramms die Box auf jeder Kerze ablesen kann, Stunden nachdem sie gemessen wurde.
- Market depth wird für das beste Ask und das beste Bid gelesen, und zwei weitere Variablen halten diese Quotes im Takt der Kerzen: Das Orderbuch aktualisiert sich Hunderte Male pro Bar und würde sich niemals mit einer auf Kerzen aufgebauten Bedingung decken.
- Formeln verwandeln die Box in ihre Breite als Prozentsatz des Preises und in einen Randabstand, der von dieser Breite aus gemessen wird, sodass dieselben zwei Einstellungen bei einem Instrument im Zehntausenderbereich dasselbe bedeuten wie bei einem, das in Einheiten notiert.
- Ein zweiter Working time-Block öffnet die Handelssitzung, und eine logische Bedingung sammelt vier Antworten: Die Box ist schmal, das Ask hält Abstand zum oberen Rand, das Bid hält Abstand zum unteren Rand, und die Position ist flat.
- Der Flag macht aus dieser Bedingung ein einziges Scharfschalten pro Sitzung und friert beide Niveaus ein; die Box dahinter mag in der nächsten Nacht neu gezeichnet werden, die scharfgeschalteten Niveaus bewegen sich nicht.
- Die Einstiege sind Market-Orders aus einer flachen Position, sobald ein gehaltener Quote sein eingefrorenes Niveau erreicht, und Position protection führt den Trade von da an weiter.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Innerhalb der Sitzung, bei scharfgeschalteter Box und flacher Position, erreicht das auf der Kerze gehaltene beste Ask das eingefrorene obere Niveau. Position modify kauft das Ordervolumen zum Markt.
- **Short-Einstieg**: Innerhalb derselben Sitzung und unter denselben Bedingungen — scharfgeschaltet und flat — fällt das auf der Kerze gehaltene beste Bid auf das eingefrorene untere Niveau. Position modify verkauft das Ordervolumen zum Markt.
- **Ausstieg**: Im Diagramm gibt es kein Ausstiegssignal. Position protection übernimmt den Trade, sobald er offen ist, und schließt ihn bei einem Take-Profit oder einem Stop-Loss von einem Prozent des Einstiegspreises, wobei der aktuelle Preis aus dem Orderbuch und nicht aus einer Kerze gelesen wird. Ein angenommener Einstieg setzt den Scharfschaltzustand außerdem wieder auf null, sodass eine Sitzung einen Trade ergibt und das nächste Scharfschalten auf den nächsten Tag wartet.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Candles | 00:05:00 | Zeitrahmen der Kerzenserie, auf der das gesamte Diagramm läuft. |
| Box From | 02:00:00 | Beginn des Fensters, in dem die Box gemessen wird. |
| Box Until | 07:59:59 | Ende des Messfensters; die letzte Kerze, die davor eröffnet, zählt noch mit. |
| Box Top Length | 72 | Über wie viele Kerzen des Messfensters das Hoch der Box genommen wird. |
| Box Bottom Length | 72 | Über wie viele Kerzen des Messfensters das Tief der Box genommen wird. |
| Max Box Width, % | 3 | Breiteste Box, als Prozentsatz des Preises, die noch als ruhig genug zum Handeln gilt. |
| Edge Margin, % | 20 | Wie weit der Preis im Moment des Scharfschaltens von einem Rand entfernt liegen muss, als Prozentsatz der Boxhöhe. |
| Session From | 08:00:00 | Beginn der Sitzung, in der Scharfschalten und Einstiege erlaubt sind. |
| Session Until | 20:00:00 | Ende der Sitzung; danach wird der Flag freigegeben und der Scharfschaltzustand gelöscht. |
| Order Volume | 0.01 | Ordergröße, die von beiden Einstiegen gesendet wird. |
| Take Profit, % | 1 | Take-Profit-Abstand, in Prozent des Einstiegspreises. |
| Stop Loss, % | 1 | Stop-Loss-Abstand, in Prozent des Einstiegspreises. |

## Diagrammdetails

- Die Box ist ein rollierendes Highest und Lowest über die eingestellte Länge auf den Kerzen, die in das Messfenster gefallen sind, und keine Spanne, die jede Nacht von Grund auf neu aufgebaut wird. Früh im Fenster steckt noch der Rest der Vornacht im Puffer; zum Ende des Fensters entsprechen die Werte genau der eben gemessenen Nacht, und genau dann werden sie verwendet.
- Der Scharfschaltzustand wird als Zahl geführt, nicht als Ausgang des Flag. Der Flag gibt sein einziges True aus und schweigt danach; deshalb ist es eine Variable — beim Scharfschalten auf eins gesetzt, beim Schließen der Sitzung auf null und nach einem Einstieg auf null —, die die Einstiegsbedingungen auf jeder Kerze lesen können.
- Jeder Wert, der in einen Vergleich oder eine logische Bedingung eingeht, wird auf jeder abgeschlossenen Kerze über eine haltende Variable erneut ausgegeben. Ein Vergleich löst nur aus, wenn beide Seiten seit seinem letzten Auslösen eingetroffen sind, deshalb muss ein einmal am Tag erfasstes Niveau Bar für Bar weitergereicht werden.
- Das Scharfschalten wird auf jeder Kerze der Sitzung geprüft, nicht nur in ihrer ersten Minute: Der erste Moment, in dem der Preis bequem innerhalb einer schmalen Box liegt, ist der Moment, in dem die Niveaus eingefroren werden. Zwischen dem Scharfschalten und dem ersten möglichen Einstieg liegt deshalb immer mindestens eine Kerze.
- Die Einstiege sind Market-Orders bei der Berührung eines Niveaus, und der schützende Ausstieg ist ein Prozentsatz des Einstiegspreises statt des gegenüberliegenden Rands der Box, sodass beide Seiten des Trades in denselben Einheiten ausgedrückt sind und einen Wechsel des Instruments überstehen.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
