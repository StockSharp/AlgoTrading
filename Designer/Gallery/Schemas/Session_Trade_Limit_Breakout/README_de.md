# Diagramm der Strategie Session Trade Limit Breakout
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Ein Ausbruch ist nur dann handelbar, wenn der Markt zuvor ruhig war, nur innerhalb der Stunden, in denen das Diagramm handeln darf, und nur ein einziges Mal, bis das Recht zu handeln zurückgegeben wird. Der Block Flag setzt diesen letzten Punkt durch: Er lässt den ersten qualifizierten Ausbruch als einzelnen Impuls durch und bleibt danach geschlossen, sodass eine Folge starker Kerzen einen Einstieg ergibt statt einer Order je Bar.

![schema](schema.svg)

## Strategieübersicht

- Alles Nachgelagerte arbeitet auf abgeschlossenen 30-Minuten-Kerzen, sodass keine Entscheidung auf einer noch laufenden Bar getroffen wird.
- Ein Highest-Block über zwanzig Kerzen markiert die Obergrenze der jüngsten Handelsspanne, und ein Previous value-Block greift dieses Niveau von einer Kerze zuvor ab — das Niveau steht damit fest, bevor die Kerze kommt, die es überwinden muss.
- Ein Average Directional Index der Länge vierzehn wird von einem Konverter auf seine eigene Linie reduziert, und ein Vergleich beschränkt Einstiege auf einen noch ruhigen Markt: die ADX-Linie unterhalb ihres Grenzwerts.
- Working time beantwortet Kerze für Kerze, ob der Zeitpunkt in das Handelsfenster fällt, und ein logisches NOT derselben Antwort markiert das Ende der Session.
- Ein logisches AND fasst vier Antworten zu einem Tor zusammen: Der Schlusskurs liegt über dem Ausbruchsniveau, die ADX-Linie liegt unter dem Grenzwert, der Zeitpunkt liegt im Handelsfenster, und es besteht keine offene Position.
- Der Block Flag macht aus diesem Tor ein Ticket. Die erste wahre Antwort wird als einzelner Impuls weitergereicht und löst über Position modify mit der Bedingung zum Positionsaufbau einen Market-Kauf aus; jede spätere Antwort wird verschluckt, solange das Ticket verbraucht ist.
- Zwei Dinge geben das Ticket zurück: ein Delay-Block, der nach der Einstiegsausführung fünfzehn abgeschlossene Kerzen zählt, und die erste Kerze, die außerhalb des Handelsfensters ausgebildet wird.
- Der Ausstieg besteht aus denselben zwei Messgrößen, die den Einstieg erlaubt haben — einem gedehnten ADX-Grenzwert und einem Niveau knapp unter dem Ausbruchsniveau —, die ein logisches OR zu einem einzigen Schließauslöser zusammenführt; darunter läuft Position protection als fester Take-Profit und Stop-Loss.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Die Kerze schließt über dem höchsten Hoch der vorangegangenen zwanzig Kerzen, die ADX-Linie liegt unter dem Grenzwert für den ruhigen Markt, die Kerze gehört zum Handelsfenster, und es besteht keine offene Position. Alle vier Bedingungen treffen auf derselben Kerze ein, das logische AND macht daraus eine einzige wahre Antwort, und Flag reicht die erste solche Antwort an Position modify weiter, das das Ordervolumen zum Marktpreis kauft.
- **Short-Einstieg**: Short-Einstiege gibt es nicht. Das Diagramm ist bewusst einseitig: Der gesuchte Ausbruch ist ein Ausbruch nach oben, und ein Rückfall unter das Give-Back-Niveau wird als Grund gelesen, eine Long-Position zu verlassen, und nicht als Grund für einen Verkauf.
- **Ausstieg**: Zwei Messgrößen beenden den Trade, und durch das logische OR genügt diejenige, die zuerst eintritt. Die erste ist ein Markt, der seine Ruhe verloren hat: Eine Formel multipliziert den Grenzwert für den ruhigen Markt mit dem Trend-Multiplikator, und ein Vergleich prüft, ob die ADX-Linie dieses gedehnte Niveau erreicht hat. Die zweite ist ein Ausbruch, der sich selbst zurückgegeben hat: Eine zweite Formel senkt das Ausbruchsniveau um den Give-Back-Faktor, und ein Vergleich prüft, ob der Schlusskurs darunter gefallen ist. Beides löst ein Position modify aus, das auf Schließen der Position gestellt ist. Unter beiden beobachtet Position protection die Einstiegsausführung und schließt den Trade bei zwei Prozent Gewinn oder einem Prozent Verlust.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Candles | 00:30:00 | Zeitrahmen, auf dem jeder Block im Diagramm arbeitet; verarbeitet werden nur abgeschlossene Kerzen. |
| Breakout Length | 20 | Anzahl der Kerzen, über die der Highest-Block zurückblickt, um das Ausbruchsniveau zu bilden. |
| ADX Length | 14 | Periode des Average Directional Index, dessen Linie als Filter für den ruhigen Markt dient. |
| Calm Market Limit | 25 | Wert, unter dem die ADX-Linie bleiben muss, damit ein Ausbruch als Ausbruch aus einem ruhigen Markt gilt. |
| Session From | 12:00:00 | Beginn des Handelsfensters; eine Kerze davor kann keine Position eröffnen. |
| Session Until | 21:00:00 | Ende des Handelsfensters; die erste Kerze danach gibt das Einstiegsticket zurück. |
| Cooldown Candles | 15 | Abgeschlossene Kerzen, die nach einer Einstiegsausführung vergehen müssen, bevor das Einstiegsticket zurückgegeben wird. |
| Order Volume | 1 | Ordergröße in Lots. |
| Trend Multiplier | 1.5 | Multiplikator, der auf den Grenzwert für den ruhigen Markt angewendet wird, um das ADX-Niveau zu erhalten, das den Trade schließt. |
| Give-Back Factor | 0.98 | Anteil des Ausbruchsniveaus, unter den der Schlusskurs für den Give-Back-Ausstieg fallen muss. |
| Take Profit, % | 2 | Take-Profit-Abstand in Prozent des Einstiegskurses. |
| Stop Loss, % | 1 | Stop-Loss-Abstand in Prozent des Einstiegskurses. |

## Diagrammdetails

- Die Prüfung auf eine flache Position im Einstiegstor hat echtes Gewicht. Ohne sie würde ein Ausbruch, der bei bereits offenem Trade eintrifft, das Ticket für eine Order verbrauchen, die die Bedingung zum Positionsaufbau ablehnen würde, und das Diagramm säße anschließend die gesamte Cooldown-Zeit umsonst ab.
- Flag sendet nur in dem Moment, in dem es gesetzt wird; sein Ausgang geht deshalb direkt in den Orderauslöser und nie in das logische AND — es ist ein Impuls, kein Pegel, weshalb jede Bedingung vor ihm und nicht neben ihm gesammelt wird.
- Die Verzögerung, die die Cooldown-Zeit misst, wird von der Einstiegsausführung scharf geschaltet und von der Kerzenreihe gespeist, sodass die fünfzehn Werte, die sie zählt, fünfzehn abgeschlossene Bars sind.
- Der Highest-Block liest das Hoch jeder Kerze, sodass das gebrochene Niveau das höchste Hoch der letzten zwanzig Bars ist und nicht der höchste Schlusskurs; der Ausbruch ist entsprechend strenger.
- Take-Profit und Stop-Loss stehen neben den beiden Ausstiegsvergleichen und nicht an ihrer Stelle: Sie sind die Rückfallebene für einen Trade, der dahintreibt, ohne dass eine der beiden Messgrößen auslöst.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
