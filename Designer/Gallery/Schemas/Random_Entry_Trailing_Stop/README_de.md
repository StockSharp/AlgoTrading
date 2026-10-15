# Strategiediagramm: Zufallseinstieg mit Trailing-Stop auf dem Ticker
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die meisten Diagramme verbrauchen ihre Blöcke darauf, den Einstiegszeitpunkt zu bestimmen. Dieses hier verwendet dafür fast keinen und alles auf den Ausstieg. Der Einstieg ist ein Münzwurf: Ein Random-Block zieht bei jeder geschlossenen Kerze eine Zahl, und auf welcher Seite eines Schwellenwerts sie landet, entscheidet über Long oder Short. Worum es in diesem Beispiel geht, ist das, was danach passiert — ein Trailing-Stop, der bei jedem ausgeführten Print des Tickers neu gesetzt wird statt einmal pro Bar.

![schema](schema.svg)

## Strategieübersicht

- Eine Fünf-Minuten-Kerzenreihe ist der Taktgeber des Diagramms. Jede geschlossene Kerze ist eine Ziehung und ein Einstiegsversuch; nichts anderes bringt die Einstiegslogik voran.
- Der Random-Block wird von dieser Kerze ausgelöst und zieht eine Zahl zwischen null und eins. Ein Vergleich mit dem Schwellenwert ergibt die Long-Seite, eine logische Negation derselben Antwort die Short-Seite, sodass ein einziger Vergleich beide Richtungen bedient.
- Die aktuelle Position wird durch eine Variable auf den Kerzentakt gebracht: Sie hält den Wert an ihrem Eingang und gibt ihn beim Kerzen-Trigger frei; der Vergleich dieses gehaltenen Werts mit null ist die Flat-Prüfung.
- Zwei logische UND-Gatter verknüpfen den Münzwurf mit der Flat-Prüfung. Beide Eingänge treffen im Kerzentakt ein, sodass jedes Gatter genau einmal pro geschlossener Kerze entschieden wird.
- Beide Einstiegsblöcke tragen die Bedingung für das Eröffnen einer Position, sodass ein Gatter, das weiterhin Ja meldet, eine bereits bestehende Position nicht aufstocken kann — das Diagramm hält jeweils nur eine Position und dreht sie nie um.
- Der Tick-Strom wird zusammen mit den Kerzen abonniert, und ein Konverter liest den Preis aus jedem ausgeführten Print.
- Position protection erhält die Einstiegsausführungen über eine Combination und diesen Print-Preis an seinem Price-Socket. Trailing ist eingeschaltet, sodass jeder Print, der den Trade weiter in den Gewinn bewegt, den Stop hinter sich herzieht — der Ausstieg wird zwischen den Kerzen entschieden, nicht auf ihnen.
- Das Chart-Panel zeichnet die Kerzen, beide Einstiegsorders, die schützende Stop-Order und jede Ausführung, sodass das gesamte Leben einer Position auf einem Panel ablesbar ist.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Bei einer geschlossenen Kerze liegt die gezogene Zahl unter dem Schwellenwert und die gehaltene Position ist null. Das Long-Gatter lässt durch, und Position modify kauft das Ordervolumen zum Marktpreis.
- **Short-Einstieg**: Bei derselben geschlossenen Kerze liegt die gezogene Zahl auf oder über dem Schwellenwert — der durch die logische Negation invertierte Long-Vergleich — und die gehaltene Position ist null. Position modify verkauft das Ordervolumen zum Marktpreis.
- **Ausstieg**: Es gibt kein Ausstiegssignal und keinen Take-Profit. Position protection übernimmt den Trade ab seiner ersten Ausführung: Es setzt den Stop 0.5% vom Einstiegspreis entfernt und trägt ihn bei eingeschaltetem Trailing hinter einem Long nach oben und hinter einem Short nach unten mit, sobald bessere Preise gedruckt werden. Die Position wird in dem Moment durch eine Marktorder geschlossen, in dem ein Print dieses Niveau berührt, was die nächste geschlossene Kerze frei lässt, um die Münze erneut zu werfen.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Candles | 00:05:00 | Zeitrahmen der Kerzenreihe. Eine geschlossene Kerze ist ein Münzwurf und ein Einstiegsversuch. |
| Coin Threshold | 0.5 | Der Wert, mit dem die gezogene Zahl verglichen wird. Bei 0.5 sind beide Richtungen gleich wahrscheinlich; ein niedrigerer Wert macht Longs seltener, ein höherer häufiger. |
| Order Volume | 1 | Ordergröße in Lots. |
| Trailing Stop, % | 0.5 | Abstand des Trailing-Stops in Prozent des Einstiegspreises. |

## Diagrammdetails

- Der Random-Block zieht aus der eigenen Zufallsquelle der Strategie und nicht aus einer globalen, sodass ein zweimaliges Abspielen derselben Historie dieselbe Folge von Würfen und dieselben Trades ergibt.
- Der Stop wird als Prozentsatz des Einstiegspreises angegeben und nicht als feste Anzahl von Preisschritten. Derselbe Abstand in Preiseinheiten bedeutet bei einem Instrument, das nahe 65 000 notiert, etwas völlig anderes als bei einem, das nahe 5 notiert; nur die prozentuale Form übersteht beide Fälle.
- Dass der Price-Socket vom Ticker gespeist wird, macht den Ausstieg feinkörnig. Stattdessen vom Kerzenschluss aus bepreist, würde derselbe Stop nur zwölfmal pro Stunde geprüft.
- Der Stop zieht kontinuierlich nach: Jede Preisverbesserung bewegt ihn, ohne zusätzlichen Abstand, den der Preis erst zurücklegen müsste, bevor der Stop wieder nachrücken darf.
- Solange die Position flat ist, wird bei jeder geschlossenen Kerze ein Einstieg versucht, sodass das Diagramm normalerweise etwas hält und der schützende Stop immer eine Position zu verwalten hat.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
